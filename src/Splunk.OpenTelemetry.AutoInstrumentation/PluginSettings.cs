// <copyright file="PluginSettings.cs" company="Splunk Inc.">
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

internal class PluginSettings
{
    // Maximum/default values, are defined in GDI spec.
    private const double MaxSnapshotSelectionRate = 0.1;
    private const double DefaultSnapshotSelectionRate = 0.01;

    // Runtime suspensions done to collect thread samples often take ~0.25ms.
    private const int DefaultSnapshotSamplingIntervalMs = 40;

    private static readonly ILogger Log = new Logger();

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginSettings"/> class
    /// using the specified <see cref="IConfigurationSource"/> to initialize values.
    /// </summary>
    /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
    internal PluginSettings(IConfigurationSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        Realm = source.GetString(ConfigurationKeys.Splunk.Realm) ?? Constants.None;
        AccessToken = source.GetString(ConfigurationKeys.Splunk.AccessToken);
        TraceResponseHeaderEnabled = source.GetBool(ConfigurationKeys.Splunk.TraceResponseHeaderEnabled) ?? true;
        var otlpEndpoint = source.GetString(ConfigurationKeys.OpenTelemetry.OtlpEndpoint);
        IsOtlpEndpointSet = !string.IsNullOrEmpty(otlpEndpoint);

#if NET
        SnapshotsEnabled = source.GetBool(ConfigurationKeys.Splunk.Snapshots.Enabled) ?? false;
        var snapshotInterval = source.GetInt32(ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs) ?? DefaultSnapshotSamplingIntervalMs;
        SnapshotsSamplingInterval = (uint)(snapshotInterval <= 0 ? DefaultSnapshotSamplingIntervalMs : snapshotInterval);
        var configuredSelectionRate = source.GetDouble(ConfigurationKeys.Splunk.Snapshots.SelectionRate) ?? DefaultSnapshotSelectionRate;
        SnapshotsSelectionRate = GetFinalSnapshotSelectionProbability(configuredSelectionRate);
        HighResolutionTimerEnabled = source.GetBool(ConfigurationKeys.Splunk.Snapshots.HighResolutionTimerEnabled) ?? false;

        CpuProfilerEnabled = source.GetBool(ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled) ?? false;
        var callStackInterval = source.GetInt32(ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval) ?? 10000;
        CpuProfilerCallStackInterval = GetFinalContinuousSamplingInterval(callStackInterval, SnapshotsEnabled, SnapshotsSamplingInterval);

        MemoryProfilerEnabled = source.GetBool(ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled) ?? false;
        var maxMemorySamplesPerMinute = source.GetInt32(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples) ?? 200;
        MemoryProfilerMaxMemorySamplesPerMinute = maxMemorySamplesPerMinute > 200 ? 200u : (uint)maxMemorySamplesPerMinute;
        var httpClientTimeout = source.GetInt32(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerExportTimeout) ?? 3000;
        ProfilerHttpClientTimeout = (uint)httpClientTimeout;
        var exportInterval = source.GetInt32(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerExportInterval) ?? 500;
        ProfilerExportInterval = exportInterval < 500 ? 500u : (uint)exportInterval;

        ProfilerLogsEndpoint = GetProfilerLogsEndpoints(source, otlpEndpoint == null ? null : new Uri(otlpEndpoint));
#endif
    }

    public uint SnapshotsSamplingInterval { get; set; }

    public bool SnapshotsEnabled { get; set; }

    public bool HighResolutionTimerEnabled { get; set; }

    public double SnapshotsSelectionRate { get; set; }

    public string Realm { get; }

    public string? AccessToken { get; }

    public bool TraceResponseHeaderEnabled { get; }

    public bool IsOtlpEndpointSet { get; }

#if NET
    public bool CpuProfilerEnabled { get; }

    public uint CpuProfilerCallStackInterval { get; }

    public uint MemoryProfilerMaxMemorySamplesPerMinute { get; }

    public bool MemoryProfilerEnabled { get; }

    public Uri ProfilerLogsEndpoint { get; }

    public uint ProfilerHttpClientTimeout { get; }

    public uint ProfilerExportInterval { get; }
#endif

    public static PluginSettings FromDefaultSources()
    {
        var configurationSource = new CompositeConfigurationSource
        {
            new EnvironmentConfigurationSource(),

#if NETFRAMEWORK
            // on .NET Framework only, also read from app.config/web.config
            new NameValueConfigurationSource(System.Configuration.ConfigurationManager.AppSettings)
#endif

        };

        return new PluginSettings(configurationSource);
    }

#if NET
    private static uint GetFinalContinuousSamplingInterval(int callStackInterval, bool snapshotsEnabled, uint snapshotsSamplingInterval)
    {
        var interval = callStackInterval < 0 ? 10000u : (uint)callStackInterval;
        if (snapshotsEnabled)
        {
            var finalContinuousSamplingInterval = (interval / snapshotsSamplingInterval) * snapshotsSamplingInterval;
            if (finalContinuousSamplingInterval != interval)
            {
                Log.Warning($"Adjusting continuous profiler call stack interval from {interval}ms to {finalContinuousSamplingInterval}ms to be aligned with snapshot sampling interval of {snapshotsSamplingInterval}ms.");
            }

            return finalContinuousSamplingInterval;
        }

        return interval;
    }

    private static double GetFinalSnapshotSelectionProbability(double configuredSelectionRate)
    {
        return configuredSelectionRate switch
        {
            <= 0 or double.NaN => DefaultSnapshotSelectionRate,
            > MaxSnapshotSelectionRate => MaxSnapshotSelectionRate,
            _ => configuredSelectionRate
        };
    }

    private static Uri GetProfilerLogsEndpoints(IConfigurationSource source, Uri? otlpFallback)
    {
        var profilerLogsEndpoint = source.GetString(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerLogsEndpoint);

        if (string.IsNullOrEmpty(profilerLogsEndpoint))
        {
            if (otlpFallback == null)
            {
                return new Uri("http://localhost:4318/v1/logs");
            }

            return otlpFallback.ToString().EndsWith("v1/logs") ? otlpFallback : new Uri(otlpFallback, "v1/logs");
        }

        return new Uri(profilerLogsEndpoint);
    }
#endif
}
