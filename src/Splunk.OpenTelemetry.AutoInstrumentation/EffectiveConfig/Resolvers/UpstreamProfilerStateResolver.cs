// <copyright file="UpstreamProfilerStateResolver.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Resolvers;

internal static class UpstreamProfilerStateResolver
{
    private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Lazy<bool> ProfilerEnabled = new(ResolveProfilerEnabled);
    private static readonly Lazy<RuntimeStateBinding> Binding = new(() => new RuntimeStateBinding(
        UpstreamInstrumentationResolver.GetInstrumentationType().Assembly.GetType(
            "OpenTelemetry.AutoInstrumentation.NativeMethods", throwOnError: true)!));

    public static EffectiveProfilerState Resolve()
    {
        return ProfilerEnabled.Value ? Binding.Value.Query() : EffectiveProfilerState.None;
    }

    private static bool ResolveProfilerEnabled()
    {
        var instrumentationType = UpstreamInstrumentationResolver.GetInstrumentationType();
        var settingsLazy = instrumentationType.GetProperty("GeneralSettings", StaticFlags)?.GetValue(null)
            ?? throw new MissingMemberException(instrumentationType.FullName, "GeneralSettings");
        var settings = settingsLazy.GetType().GetProperty("Value")?.GetValue(settingsLazy)
            ?? throw new MissingMemberException(settingsLazy.GetType().FullName, "Value");
        return settings.GetType().GetProperty("ProfilerEnabled")?.GetValue(settings) is bool enabled
            ? enabled
            : throw new MissingMemberException(settings.GetType().FullName, "ProfilerEnabled");
    }

    // Bind the upstream managed wrapper so it owns the native ABI, platform selection and structure sizes.
    internal sealed class RuntimeStateBinding
    {
        private readonly MethodInfo _query;
        private readonly FieldInfo _configuration;
        private readonly FieldInfo _cpuInterval;
        private readonly FieldInfo _snapshotInterval;
        private readonly FieldInfo _allocationRate;

        public RuntimeStateBinding(Type nativeMethodsType)
        {
            _query = nativeMethodsType.GetMethod("GetContinuousProfilerState", StaticFlags)
                ?? throw new MissingMethodException(nativeMethodsType.FullName, "GetContinuousProfilerState");
            var parameters = _query.GetParameters();
            if (!_query.ReturnType.IsEnum || parameters.Length != 1 || !parameters[0].IsOut || !parameters[0].ParameterType.IsByRef)
            {
                throw new MissingMethodException("The upstream continuous profiler state query has an unsupported signature.");
            }

            var stateType = parameters[0].ParameterType.GetElementType()!;
            _configuration = stateType.GetField("CommittedConfiguration", BindingFlags.Instance | BindingFlags.Public)
                ?? throw new MissingFieldException(stateType.FullName, "CommittedConfiguration");
            _cpuInterval = GetConfigurationField("CpuSamplingIntervalMilliseconds");
            _snapshotInterval = GetConfigurationField("SelectiveThreadSamplingIntervalMilliseconds");
            _allocationRate = GetConfigurationField("MaxAllocationSamplesPerMinute");
        }

        public EffectiveProfilerState Query()
        {
            object?[] arguments = [null];
            var result = _query.Invoke(null, arguments);
            if (result?.ToString() != "Succeeded")
            {
                throw new InvalidOperationException($"The upstream continuous profiler state query failed: {result}.");
            }

            var configuration = _configuration.GetValue(arguments[0])
                ?? throw new InvalidOperationException("The upstream continuous profiler state was unavailable.");
            var cpuInterval = (uint)_cpuInterval.GetValue(configuration)!;
            var snapshotInterval = (uint)_snapshotInterval.GetValue(configuration)!;
            var features = EffectiveProfilerFeatures.None;
            if (cpuInterval != 0)
            {
                features |= EffectiveProfilerFeatures.Cpu;
            }

            if (snapshotInterval != 0)
            {
                features |= EffectiveProfilerFeatures.Snapshot;
            }

#if NET
            if ((uint)_allocationRate.GetValue(configuration)! != 0)
            {
                features |= EffectiveProfilerFeatures.Memory;
            }
#endif
            return new EffectiveProfilerState(features, cpuInterval, snapshotInterval);
        }

        private FieldInfo GetConfigurationField(string name)
        {
            var field = _configuration.FieldType.GetField(name, BindingFlags.Instance | BindingFlags.Public);
            return field?.FieldType == typeof(uint)
                ? field
                : throw new MissingFieldException(_configuration.FieldType.FullName, name);
        }
    }
}
