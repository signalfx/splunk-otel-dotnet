// <copyright file="ProfilerRuntimeConfiguration.cs" company="Splunk Inc.">
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

using Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Resolvers;

namespace Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

internal static class ProfilerRuntimeConfiguration
{
    private static readonly object Sync = new();

    private static PluginSettings? _startupSettings;
    private static ProfilerRemoteConfigState? _state;

    public static bool RuntimeConfigurationEnabled
    {
        get
        {
            lock (Sync)
            {
                return GetStartupSettings().OpAmpRemoteConfigEnabled;
            }
        }
    }

    public static ProfilerRemoteConfigState Current => GetState();

    internal static uint StartupCpuProfilerCallStackInterval
    {
        get
        {
            lock (Sync)
            {
                var startupSettings = GetStartupSettings();
                return startupSettings.CpuProfilerEnabled || startupSettings.OpAmpRemoteConfigEnabled
                    ? PluginSettingsHelper.GetFinalContinuousSamplingInterval(startupSettings)
                    : 0u;
            }
        }
    }

    internal static uint CurrentCpuProfilerCallStackInterval
    {
        get
        {
            var state = Current;
            return state.CpuProfilerEnabled
                ? state.CpuProfilerCallStackInterval
                : 0u;
        }
    }

    public static void Initialize(PluginSettings settings)
    {
        settings = settings ?? throw new ArgumentNullException(nameof(settings));

        lock (Sync)
        {
            _startupSettings = settings;
            Volatile.Write(ref _state, new ProfilerRemoteConfigState(settings));
        }
    }

    public static void Apply(YamlRoot? configuration)
    {
        Apply(configuration, NativeContinuousProfilerConfigurator.Configure);
    }

    public static EffectiveProfilerState GetEffectiveProfilerState()
    {
        return GetEffectiveProfilerState(UpstreamProfilerStateResolver.Resolve);
    }

    internal static EffectiveProfilerState GetEffectiveProfilerState(Func<EffectiveProfilerState> queryNativeState)
    {
        // Keep combined native and managed reads consistent with configuration transitions.
        lock (Sync)
        {
            var runtimeState = GetState();
            var nativeState = queryNativeState();
            return new EffectiveProfilerState(
                nativeState.Features,
                nativeState.CpuSamplingInterval,
                nativeState.SnapshotSamplingInterval,
                runtimeState.SnapshotsSelectionRate);
        }
    }

    internal static void Apply(YamlRoot? configuration, Action<PluginSettings, ProfilerRemoteConfigState> configureNative)
    {
        lock (Sync)
        {
            var startupSettings = GetStartupSettings();
            var next = ProfilerRemoteConfigState.FromRemoteConfiguration(configuration, startupSettings);
            configureNative(startupSettings, next);
            Volatile.Write(ref _state, next);
        }
    }

    private static PluginSettings GetStartupSettings()
    {
        return _startupSettings
            ?? throw new InvalidOperationException("Profiler runtime configuration has not been initialized.");
    }

    private static ProfilerRemoteConfigState GetState()
    {
        return Volatile.Read(ref _state)
            ?? throw new InvalidOperationException("Profiler runtime configuration has not been initialized.");
    }
}
