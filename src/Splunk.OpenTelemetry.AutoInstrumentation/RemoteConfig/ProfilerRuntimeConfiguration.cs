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
using Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

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
                return PluginSettingsHelper.GetFinalContinuousSamplingInterval(GetStartupSettings());
            }
        }
    }

    internal static uint CurrentCpuProfilerCallStackInterval
    {
        get
        {
            lock (Sync)
            {
                return GetState().CpuProfilerEnabled
                    ? PluginSettingsHelper.GetFinalContinuousSamplingInterval(GetStartupSettings())
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
            _state = new ProfilerRemoteConfigState(settings.CpuProfilerEnabled);
        }
    }

    public static void Apply(YamlRoot? configuration)
    {
        var profilingConfig = configuration?.Distribution?.Splunk?.Profiling;
        if (profilingConfig == null)
        {
            return;
        }

        PluginSettings startupSettings;
        ProfilerRemoteConfigState next;

        lock (Sync)
        {
            var cpuProfilerEnabled = profilingConfig.AlwaysOn?.CpuProfiler != null;
            startupSettings = GetStartupSettings();
            next = new ProfilerRemoteConfigState(cpuProfilerEnabled);
            _state = next;
        }

        ApplyToExporter(startupSettings, next);
        NativeContinuousProfilerConfigurator.Configure(startupSettings, next);
    }

    public static void ApplyCurrentToNative()
    {
        var (startupSettings, state) = GetConfigurationSnapshot();
        NativeContinuousProfilerConfigurator.Configure(startupSettings, state);
    }

    public static void ApplyToExporter(PprofInOtlpLogsExporter exporter)
    {
        var (startupSettings, state) = GetConfigurationSnapshot();
        ApplyToExporter(startupSettings, state, exporter);
    }

    private static void ApplyToExporter(
        PluginSettings startupSettings,
        ProfilerRemoteConfigState state)
    {
        var exporter = Plugin.TryGetPprofInOtlpLogsExporter();
        if (exporter != null)
        {
            ApplyToExporter(startupSettings, state, exporter);
        }
    }

    private static void ApplyToExporter(
        PluginSettings startupSettings,
        ProfilerRemoteConfigState state,
        PprofInOtlpLogsExporter exporter)
    {
        exporter.SampleProcessor.ContinuousSamplingPeriod = state.CpuProfilerEnabled
            ? PluginSettingsHelper.GetFinalContinuousSamplingInterval(startupSettings)
            : 0u;
    }

    private static (PluginSettings StartupSettings, ProfilerRemoteConfigState State) GetConfigurationSnapshot()
    {
        lock (Sync)
        {
            return (GetStartupSettings(), GetState());
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
