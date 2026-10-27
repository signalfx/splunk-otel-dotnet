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

    public static ProfilerRemoteConfigState Current
    {
        get
        {
            lock (Sync)
            {
                return GetState();
            }
        }
    }

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
            lock (Sync)
            {
                var state = GetState();
                return state.CpuProfilerEnabled
                    ? state.CpuProfilerCallStackInterval
                    : 0u;
            }
        }
    }

    public static void Initialize(PluginSettings settings)
    {
        settings = settings ?? throw new ArgumentNullException(nameof(settings));

        lock (Sync)
        {
            _startupSettings = settings;
            _state = new ProfilerRemoteConfigState(
                settings.CpuProfilerEnabled,
                PluginSettingsHelper.GetFinalContinuousSamplingInterval(settings));
        }
    }

    public static void Apply(YamlRoot? configuration)
    {
        Apply(configuration, NativeContinuousProfilerConfigurator.Configure);
    }

    internal static void Apply(YamlRoot? configuration, Action<PluginSettings, ProfilerRemoteConfigState> configureNative)
    {
        var profilingConfig = configuration?.Distribution?.Splunk?.Profiling;
        if (profilingConfig == null)
        {
            return;
        }

        lock (Sync)
        {
            var cpuProfiler = profilingConfig.AlwaysOn?.CpuProfiler;
            var startupSettings = GetStartupSettings();
            var cpuSamplingInterval = cpuProfiler != null
                ? PluginSettingsHelper.GetFinalContinuousSamplingInterval(
                    cpuProfiler.SamplingInterval,
                    startupSettings.SnapshotsEnabled,
                    startupSettings.SnapshotsSamplingInterval)
                : 0u;
            var next = new ProfilerRemoteConfigState(cpuProfiler != null, cpuSamplingInterval);
            configureNative(startupSettings, next);
            _state = next;
        }
    }

    private static PluginSettings GetStartupSettings()
    {
        return _startupSettings
            ?? throw new InvalidOperationException("Profiler runtime configuration has not been initialized.");
    }

    private static ProfilerRemoteConfigState GetState()
    {
        return _state
            ?? throw new InvalidOperationException("Profiler runtime configuration has not been initialized.");
    }
}
