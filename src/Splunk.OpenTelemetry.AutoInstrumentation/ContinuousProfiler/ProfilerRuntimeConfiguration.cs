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

using System.Globalization;

namespace Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal static class ProfilerRuntimeConfiguration
{
    private static readonly object Sync = new();

    private static ProfilerRuntimeSettings? _settings;
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
            _opAmpRemoteConfigurationEnabled = false;
        }
    }

    public static void EnableOpAmpRemoteConfiguration()
    {
        lock (Sync)
        {
            _opAmpRemoteConfigurationEnabled = true;
        }
    }

    public static ProfilerRuntimeUpdateResult Apply(IReadOnlyDictionary<string, string?> values)
    {
        ProfilerRuntimeSettings next;
        var result = new ProfilerRuntimeUpdateResult();

        lock (Sync)
        {
            if (_settings == null)
            {
                throw new InvalidOperationException("Profiler runtime configuration has not been initialized.");
            }

            var cpuProfilerEnabled = _settings.CpuProfilerEnabled;
            var cpuProfilerCallStackInterval = _settings.CpuProfilerCallStackInterval;
            var memoryProfilerEnabled = _settings.MemoryProfilerEnabled;
            var memoryProfilerMaxMemorySamplesPerMinute = _settings.MemoryProfilerMaxMemorySamplesPerMinute;
            var snapshotsEnabled = _settings.SnapshotsEnabled;
            var snapshotsSamplingInterval = _settings.SnapshotsSamplingInterval;
            var snapshotsSelectionRate = _settings.SnapshotsSelectionRate;
            var profilerExportInterval = _settings.ProfilerExportInterval;

            foreach (var value in values)
            {
                switch (value.Key)
                {
                    case ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled:
                        if (TryParseBool(value.Value, out var parsedBool))
                        {
                            cpuProfilerEnabled = parsedBool;
                            result.Applied.Add(value.Key);
                        }
                        else
                        {
                            result.Invalid.Add(value.Key);
                        }

                        break;

                    case ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval:
                        if (TryParseInt32(value.Value, out var parsedInt))
                        {
                            cpuProfilerCallStackInterval = PluginSettings.GetFinalContinuousSamplingInterval(parsedInt, snapshotsEnabled, snapshotsSamplingInterval);
                            result.Applied.Add(value.Key);
                        }
                        else
                        {
                            result.Invalid.Add(value.Key);
                        }

                        break;

                    case ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled:
                        if (TryParseBool(value.Value, out parsedBool))
                        {
#if NET
                            memoryProfilerEnabled = parsedBool;
                            result.Applied.Add(value.Key);
#else
                            result.Unsupported.Add(value.Key);
#endif
                        }
                        else
                        {
                            result.Invalid.Add(value.Key);
                        }

                        break;

                    case ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples:
                        if (TryParseInt32(value.Value, out parsedInt))
                        {
#if NET
                            memoryProfilerMaxMemorySamplesPerMinute = PluginSettings.GetFinalMaxMemorySamples(parsedInt);
                            result.Applied.Add(value.Key);
#else
                            result.Unsupported.Add(value.Key);
#endif
                        }
                        else
                        {
                            result.Invalid.Add(value.Key);
                        }

                        break;

                    case ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerExportInterval:
                        result.Unsupported.Add(value.Key);
                        break;

                    case ConfigurationKeys.Splunk.Snapshots.Enabled:
                        if (TryParseBool(value.Value, out parsedBool))
                        {
                            snapshotsEnabled = parsedBool;
                            result.Applied.Add(value.Key);
                        }
                        else
                        {
                            result.Invalid.Add(value.Key);
                        }

                        break;

                    case ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs:
                        if (TryParseInt32(value.Value, out parsedInt))
                        {
                            snapshotsSamplingInterval = PluginSettings.GetFinalSnapshotSamplingInterval(parsedInt);
                            result.Applied.Add(value.Key);
                        }
                        else
                        {
                            result.Invalid.Add(value.Key);
                        }

                        break;

                    case ConfigurationKeys.Splunk.Snapshots.SelectionRate:
                        if (TryParseDouble(value.Value, out var parsedDouble))
                        {
                            snapshotsSelectionRate = PluginSettings.GetFinalSnapshotSelectionProbability(parsedDouble);
                            result.Applied.Add(value.Key);
                        }
                        else
                        {
                            result.Invalid.Add(value.Key);
                        }

                        break;

                    default:
                        result.Unknown.Add(value.Key);
                        break;
                }
            }

            if (cpuProfilerEnabled)
            {
                cpuProfilerCallStackInterval = PluginSettings.GetFinalContinuousSamplingInterval((int)cpuProfilerCallStackInterval, snapshotsEnabled, snapshotsSamplingInterval);
            }

            next = new ProfilerRuntimeSettings(
                cpuProfilerEnabled,
                cpuProfilerEnabled ? cpuProfilerCallStackInterval : Constants.DefaultSamplingInterval,
                memoryProfilerEnabled,
                memoryProfilerMaxMemorySamplesPerMinute,
                snapshotsEnabled,
                snapshotsSamplingInterval,
                snapshotsSelectionRate,
                profilerExportInterval);

            _settings = next;
        }

        ApplyToExporter(next);
        NativeContinuousProfilerConfigurator.Configure(next);
        return result;
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
        exporter.SampleProcessor.SelectedSamplingPeriod = settings.SnapshotsSamplingInterval;
    }

    private static bool TryParseBool(string? value, out bool result)
    {
        return bool.TryParse(value, out result);
    }

    private static bool TryParseInt32(string? value, out int result)
    {
        return int.TryParse(value, out result);
    }

    private static bool TryParseDouble(string? value, out double result)
    {
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }
}
