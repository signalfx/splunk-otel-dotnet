// <copyright file="NativeContinuousProfilerConfigurator.cs" company="Splunk Inc.">
// Copyright Splunk Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Reflection;

namespace Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

internal static class NativeContinuousProfilerConfigurator
{
    private static readonly Lazy<RuntimeConfigurationBinding> Binding = new(() => new RuntimeConfigurationBinding(
        Type.GetType("OpenTelemetry.AutoInstrumentation.NativeMethods, OpenTelemetry.AutoInstrumentation", throwOnError: true)!));

    public static void Configure(
        PluginSettings startupSettings,
        ProfilerRemoteConfigState state)
    {
        Binding.Value.Configure(startupSettings, state);
    }

    public static void ConfigureStartup(PluginSettings settings, ProfilerRemoteConfigState state)
    {
        Binding.Value.Configure(settings, state, seed: true);
    }

    // Bind the upstream managed wrapper so it owns the native ABI and structure sizes.
    internal sealed class RuntimeConfigurationBinding
    {
        private readonly MethodInfo _apply;
        private readonly ConstructorInfo _configuration;
        private readonly object _controlPlaneAuthority;
        private readonly object _seedAuthority;

        public RuntimeConfigurationBinding(Type nativeMethodsType)
        {
            _apply = nativeMethodsType.GetMethod(
                "ApplyContinuousProfilerConfiguration",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingMethodException(nativeMethodsType.FullName, "ApplyContinuousProfilerConfiguration");
            var parameters = _apply.GetParameters();
            if (!_apply.ReturnType.IsEnum || parameters.Length != 3 || !parameters[1].ParameterType.IsEnum ||
                !parameters[2].IsOut || !parameters[2].ParameterType.IsByRef)
            {
                throw new MissingMethodException("The upstream continuous profiler configuration method has an unsupported signature.");
            }

            _configuration = parameters[0].ParameterType.GetConstructor([typeof(uint), typeof(uint), typeof(uint)])
                ?? throw new MissingMethodException(parameters[0].ParameterType.FullName, ".ctor(uint, uint, uint)");
            _controlPlaneAuthority = Enum.Parse(parameters[1].ParameterType, "ControlPlane");
            _seedAuthority = Enum.Parse(parameters[1].ParameterType, "Seed");
        }

        public void Configure(PluginSettings startupSettings, ProfilerRemoteConfigState state, bool seed = false)
        {
            var cpuSamplingInterval = state.CpuProfilerEnabled
                ? state.CpuProfilerCallStackInterval
                : 0u;
            var snapshotSamplingInterval = state.SnapshotsEnabled
                ? state.SnapshotsSamplingInterval
                : 0u;
#if NET
            var maxMemorySamplesPerMinute = startupSettings.MemoryProfilerEnabled
                ? PluginSettingsHelper.GetFinalMaxMemorySamples(startupSettings)
                : 0u;
#else
            const uint maxMemorySamplesPerMinute = 0;
#endif
            var configuration = _configuration.Invoke([cpuSamplingInterval, snapshotSamplingInterval, maxMemorySamplesPerMinute]);
            object?[] arguments = [configuration, seed ? _seedAuthority : _controlPlaneAuthority, null];
            var result = _apply.Invoke(null, arguments);
            var resultName = result?.ToString();
            if (resultName is not ("Applied" or "NoChange") &&
                !(seed && resultName is "IgnoredSeedAlreadyCommitted" or "IgnoredLowerAuthority"))
            {
                throw new InvalidOperationException($"The upstream continuous profiler configuration failed: {result}.");
            }
        }
    }
}
