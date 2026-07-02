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

    private static ProfilerRuntimeSettings? _settings;
    private static bool _snapshotsEnabled;
    private static uint _snapshotsSamplingInterval;
    private static bool _opAmpRemoteConfigurationEnabled;

    public static bool RuntimeConfigurationEnabled
    {
        get
        {
            lock (Sync)
            {
                return _opAmpRemoteConfigurationEnabled;
            }
        }
    }

    public static ProfilerRuntimeSettings Current
    {
        get
        {
            lock (Sync)
            {
                if (_settings == null)
                {
                    throw new InvalidOperationException("Profiler runtime configuration has not been initialized.");
                }

                return _settings;
            }
        }
    }

    public static void Initialize(PluginSettings settings)
    {
        lock (Sync)
        {
            _settings = ProfilerRuntimeSettings.FromPluginSettings(settings);
            _snapshotsEnabled = settings.SnapshotsEnabled;
            _snapshotsSamplingInterval = settings.SnapshotsSamplingInterval == 0
                ? Constants.DefaultSnapshotSamplingIntervalMs
                : settings.SnapshotsSamplingInterval;
            _opAmpRemoteConfigurationEnabled = settings.OpAmpRemoteConfigEnabled;
        }
    }

    // public static void EnableOpAmpRemoteConfiguration()
    // {
    //     lock (Sync)
    //     {
    //         _opAmpRemoteConfigurationEnabled = true;
    //     }
    // }

    public static void Apply(YamlRoot? configuration)
    {
        var profilingConfig = configuration?.Distribution?.Splunk?.Profiling;
        if (profilingConfig == null)
        {
            return;
        }

        ProfilerRuntimeSettings next;

        lock (Sync)
        {
            if (_settings == null)
            {
                throw new InvalidOperationException("Profiler runtime configuration has not been initialized.");
            }

            var cpuProfiler = profilingConfig.AlwaysOn?.CpuProfiler;
            var cpuProfilerEnabled = cpuProfiler != null;
            var cpuProfilerCallStackInterval = cpuProfilerEnabled
                ? PluginSettings.GetFinalContinuousSamplingInterval((int)cpuProfiler!.SamplingInterval, _snapshotsEnabled, _snapshotsSamplingInterval)
                : Constants.DefaultSamplingInterval;

            next = new ProfilerRuntimeSettings(
                cpuProfilerEnabled,
                cpuProfilerEnabled ? cpuProfilerCallStackInterval : Constants.DefaultSamplingInterval);

            _settings = next;
        }

        ApplyToExporter(next);
        NativeContinuousProfilerConfigurator.Configure(next);
    }

    public static void ApplyCurrentToNative()
    {
        NativeContinuousProfilerConfigurator.Configure(Current);
    }

    public static void ApplyToExporter(PprofInOtlpLogsExporter exporter)
    {
        ApplyToExporter(Current, exporter);
    }

    private static void ApplyToExporter(ProfilerRuntimeSettings settings)
    {
        var exporter = Plugin.TryGetPprofInOtlpLogsExporter();
        if (exporter != null)
        {
            ApplyToExporter(settings, exporter);
        }
    }

    private static void ApplyToExporter(ProfilerRuntimeSettings settings, PprofInOtlpLogsExporter exporter)
    {
        exporter.SampleProcessor.ContinuousSamplingPeriod = settings.CpuProfilerCallStackInterval;
    }
}
