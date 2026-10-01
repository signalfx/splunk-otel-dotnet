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
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration.Parser;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal class PluginSettings
{
    private static readonly ILogger Log = new Logger();

    private static readonly bool IsYamlConfigEnabled = Environment.GetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.Enabled) == "true";

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
        TraceResponseHeaderEnabled = source.GetBool(ConfigurationKeys.Splunk.TraceResponseHeaderEnabled) ?? Constants.DefaultTraceResponseHeaderEnabled;
        var otlpEndpoint = source.GetString(ConfigurationKeys.OpenTelemetry.OtlpEndpoint);
        IsOtlpEndpointSet = !string.IsNullOrEmpty(otlpEndpoint);
        OpAmpRemoteConfigEnabled = source.GetBool(ConfigurationKeys.Splunk.OpAmp.RemoteConfig) ?? false;

        SnapshotsEnabled = source.GetBool(ConfigurationKeys.Splunk.Snapshots.Enabled) ?? false;
        var snapshotInterval = source.GetInt32(ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs) ?? Constants.DefaultSnapshotSamplingIntervalMs;
        SnapshotsSamplingInterval = PluginSettingsHelper.GetFinalSnapshotSamplingInterval(snapshotInterval);
        var configuredSelectionRate = source.GetDouble(ConfigurationKeys.Splunk.Snapshots.SelectionRate) ?? Constants.DefaultSnapshotSelectionRate;
        SnapshotsSelectionRate = PluginSettingsHelper.GetFinalSnapshotSelectionProbability(configuredSelectionRate);
        HighResolutionTimerEnabled = source.GetBool(ConfigurationKeys.Splunk.Snapshots.HighResolutionTimerEnabled) ?? false;

        CpuProfilerEnabled = source.GetBool(ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled) ?? false;
        var callStackInterval = source.GetInt32(ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval) ?? Constants.DefaultSamplingInterval;
        CpuProfilerCallStackInterval = CpuProfilerEnabled || OpAmpRemoteConfigEnabled
            ? PluginSettingsHelper.GetFinalContinuousSamplingInterval(callStackInterval, SnapshotsEnabled, SnapshotsSamplingInterval)
            : Constants.DefaultSamplingInterval;

#if NET
        MemoryProfilerEnabled = source.GetBool(ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled) ?? Constants.DefaultHighResolutionTimer;
        var maxMemorySamplesPerMinute = source.GetInt32(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples) ?? Constants.DefaultMaxMemorySamples;
        MemoryProfilerMaxMemorySamplesPerMinute = PluginSettingsHelper.GetFinalMaxMemorySamples(maxMemorySamplesPerMinute);
#endif
        var httpClientTimeout = source.GetInt32(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerExportTimeout) ?? Constants.DefaultProfilerExportTimeout;
        ProfilerHttpClientTimeout = PluginSettingsHelper.GetFinalExportTimeout(httpClientTimeout);
        var exportInterval = source.GetInt32(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerExportInterval) ?? Constants.DefaultProfilerExportInterval;
        ProfilerExportInterval = PluginSettingsHelper.GetFinalExportInterval(exportInterval);

        ProfilerLogsEndpoint = PluginSettingsHelper.GetProfilerLogsEndpoint(source, otlpEndpoint == null ? null : new Uri(otlpEndpoint));
    }

    internal PluginSettings(YamlRoot configuration, string? fileName = null)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        FileBasedConfigFileName = fileName ?? Constants.DefaultFileBasedConfigFileName;
        Realm = Constants.None;
        AccessToken = null;
        IsOtlpEndpointSet = false;

        var traceConfig = configuration.InstrumentationDevelopment?.Dotnet?.Traces;
#if NETFRAMEWORK
        TraceResponseHeaderEnabled = traceConfig?.Aspnet?.ResponseHeaderEnabled ?? Constants.DefaultTraceResponseHeaderEnabled;
#else
        TraceResponseHeaderEnabled = traceConfig?.Aspnetcore?.ResponseHeaderEnabled ?? Constants.DefaultTraceResponseHeaderEnabled;
#endif

        OpAmpRemoteConfigEnabled = configuration.OpampDevelopment?.Features?.RemoteConfig != null;

        var profilingConfig = configuration.Distribution?.Splunk?.Profiling;
        if (profilingConfig != null)
        {
            if (profilingConfig.Callgraphs != null)
            {
                SnapshotsEnabled = true;
                HighResolutionTimerEnabled = profilingConfig.Callgraphs.HighResolutionTimerEnabled;
                SnapshotsSamplingInterval = PluginSettingsHelper.GetFinalSnapshotSamplingInterval(profilingConfig.Callgraphs.SamplingInterval);
                var configuredSelectionRate = profilingConfig.Callgraphs.SelectionProbability;
                SnapshotsSelectionRate = PluginSettingsHelper.GetFinalSnapshotSelectionProbability(configuredSelectionRate);
            }

            if (profilingConfig.AlwaysOn != null)
            {
                if (profilingConfig.AlwaysOn.CpuProfiler != null)
                {
                    CpuProfilerEnabled = true;
                    var callStackInterval = profilingConfig.AlwaysOn.CpuProfiler.SamplingInterval;
                    CpuProfilerCallStackInterval = PluginSettingsHelper.GetFinalContinuousSamplingInterval(callStackInterval, SnapshotsEnabled, SnapshotsSamplingInterval);
                }

#if NET
                if (profilingConfig.AlwaysOn.MemoryProfiler != null)
                {
                    MemoryProfilerEnabled = true;
                    MemoryProfilerMaxMemorySamplesPerMinute = PluginSettingsHelper.GetFinalMaxMemorySamples(profilingConfig.AlwaysOn.MemoryProfiler.MaxMemorySamples);
                }
#endif
            }

            ProfilerHttpClientTimeout = PluginSettingsHelper.GetFinalExportTimeout(profilingConfig.Exporter.OtlpLogHttp.ExportTimeout);
            ProfilerExportInterval = PluginSettingsHelper.GetFinalExportInterval(profilingConfig.Exporter.OtlpLogHttp.ScheduleDelay);
            ProfilerLogsEndpoint = new Uri(profilingConfig.Exporter.OtlpLogHttp.Endpoint);
        }

        if (OpAmpRemoteConfigEnabled && !CpuProfilerEnabled)
        {
            CpuProfilerCallStackInterval = PluginSettingsHelper.GetFinalContinuousSamplingInterval(
                Constants.DefaultSamplingInterval,
                SnapshotsEnabled,
                SnapshotsSamplingInterval);
        }
    }

    public uint SnapshotsSamplingInterval { get; set; }

    public bool SnapshotsEnabled { get; set; }

    public bool HighResolutionTimerEnabled { get; set; }

    public double SnapshotsSelectionRate { get; set; }

    public string Realm { get; }

    public string? AccessToken { get; }

    public bool TraceResponseHeaderEnabled { get; }

    public bool IsOtlpEndpointSet { get; }

    public string? FileBasedConfigFileName { get; }

    public bool CpuProfilerEnabled { get; }

    public uint CpuProfilerCallStackInterval { get; }

#if NET
    public uint MemoryProfilerMaxMemorySamplesPerMinute { get; }

    public bool MemoryProfilerEnabled { get; }
#endif

    public Uri ProfilerLogsEndpoint { get; } = new Uri(Constants.DefaultProfilerLogsEndpoint);

    public uint ProfilerHttpClientTimeout { get; }

    public uint ProfilerExportInterval { get; }

    public bool OpAmpRemoteConfigEnabled { get; }

    public static PluginSettings FromDefaultSources()
    {
        if (IsYamlConfigEnabled)
        {
            var fileName = PluginSettingsHelper.ResolveFileBasedConfigFileName();

            var splunkConfiguration = YamlConfigurationParser.ParseFile(fileName);
            if (splunkConfiguration != null)
            {
                return new PluginSettings(splunkConfiguration, fileName);
            }
            else
            {
                Log.Error($"Failed to load Splunk configuration from file '{fileName}'. Falling back to environment variables.");
            }
        }

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
}
