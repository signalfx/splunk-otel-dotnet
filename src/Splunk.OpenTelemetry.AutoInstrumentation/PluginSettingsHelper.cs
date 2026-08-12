// <copyright file="PluginSettingsHelper.cs" company="Splunk Inc.">
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

using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal static class PluginSettingsHelper
{
    private static readonly ILogger Log = new Logger();

    public static string ResolveFileBasedConfigFileName()
    {
        var fileName = Environment.GetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.FileName);
        return ResolveFileBasedConfigFileName(fileName);
    }

    public static string ResolveFileBasedConfigFileName(string? fileName)
    {
        return string.IsNullOrEmpty(fileName) ? Constants.DefaultFileBasedConfigFileName : fileName!;
    }

    public static uint GetFinalContinuousSamplingInterval(long callStackInterval)
    {
        return callStackInterval <= 0 || callStackInterval > uint.MaxValue
            ? Constants.DefaultSamplingInterval
            : (uint)callStackInterval;
    }

    public static uint GetFinalContinuousSamplingInterval(
        long callStackInterval,
        bool snapshotsEnabled,
        uint snapshotsSamplingInterval)
    {
        var interval = GetFinalContinuousSamplingInterval(callStackInterval);
        if (!snapshotsEnabled)
        {
            return interval;
        }

        var snapshotInterval = GetFinalSnapshotSamplingInterval(snapshotsSamplingInterval);
        var finalContinuousSamplingInterval = (interval / snapshotInterval) * snapshotInterval;
        if (finalContinuousSamplingInterval <= snapshotInterval)
        {
            finalContinuousSamplingInterval = snapshotInterval * 2;
        }

        if (finalContinuousSamplingInterval != interval)
        {
            Log.Warning($"Adjusting continuous profiler call stack interval from {interval}ms to {finalContinuousSamplingInterval}ms to be aligned with snapshot sampling interval of {snapshotInterval}ms.");
        }

        return finalContinuousSamplingInterval;
    }

    public static uint GetFinalContinuousSamplingInterval(PluginSettings settings)
    {
        var snapshotsSamplingInterval = GetFinalSnapshotSamplingInterval(settings.SnapshotsSamplingInterval);
        return GetFinalContinuousSamplingInterval(
            settings.CpuProfilerCallStackInterval,
            settings.SnapshotsEnabled,
            snapshotsSamplingInterval);
    }

#if NET
    public static uint GetFinalMaxMemorySamples(long maxMemorySamplesPerMinute)
    {
        if (maxMemorySamplesPerMinute < 0 || maxMemorySamplesPerMinute > 200)
        {
            return Constants.DefaultMaxMemorySamples;
        }

        return (uint)maxMemorySamplesPerMinute;
    }

    public static uint GetFinalMaxMemorySamples(PluginSettings settings)
    {
        return GetFinalMaxMemorySamples(settings.MemoryProfilerMaxMemorySamplesPerMinute);
    }
#endif

    public static uint GetFinalExportInterval(long exportInterval)
    {
        if (exportInterval < 500 || exportInterval > uint.MaxValue)
        {
            return Constants.DefaultProfilerExportInterval;
        }

        return (uint)exportInterval;
    }

    public static uint GetFinalExportTimeout(long exportTimeout)
    {
        if (exportTimeout <= 0 || exportTimeout > uint.MaxValue)
        {
            return Constants.DefaultProfilerExportTimeout;
        }

        return (uint)exportTimeout;
    }

    public static uint GetFinalSnapshotSamplingInterval(long snapshotsSamplingInterval)
    {
        if (snapshotsSamplingInterval <= 0 || snapshotsSamplingInterval > int.MaxValue)
        {
            return Constants.DefaultSnapshotSamplingIntervalMs;
        }

        return (uint)snapshotsSamplingInterval;
    }

    public static double GetFinalSnapshotSelectionProbability(double configuredSelectionRate)
    {
        return configuredSelectionRate switch
        {
            <= 0 or double.NaN => Constants.DefaultSnapshotSelectionRate,
            > Constants.MaxSnapshotSelectionRate => Constants.MaxSnapshotSelectionRate,
            _ => configuredSelectionRate
        };
    }

    public static Uri GetProfilerLogsEndpoint(IConfigurationSource source, Uri? otlpFallback)
    {
        var profilerLogsEndpoint = source.GetString(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerLogsEndpoint);

        if (string.IsNullOrEmpty(profilerLogsEndpoint))
        {
            if (otlpFallback == null)
            {
                return new Uri(Constants.DefaultProfilerLogsEndpoint);
            }

            return otlpFallback.ToString().EndsWith("v1/logs") ? otlpFallback : new Uri(otlpFallback, "v1/logs");
        }

        return new Uri(profilerLogsEndpoint);
    }
}
