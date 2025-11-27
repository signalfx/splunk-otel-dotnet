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

using System.Diagnostics;
using System.Reflection;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration.Utils;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal class PluginSettings
{
    // Maximum/default values, are defined in GDI spec.
    private const double MaxSnapshotSelectionRate = 0.1;
    private const double DefaultSnapshotSelectionRate = 0.01;

    // Runtime suspensions done to collect thread samples often take ~0.25ms. Use `60ms` as default sampling interval
    // to limit induced overhead.
    private const int DefaultSnapshotSamplingIntervalMs = 60;

    private static readonly bool IsYamlConfigEnabled = Environment.GetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.Enabled) == "true";

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
        CpuProfilerEnabled = source.GetBool(ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled) ?? false;
        SnapshotsEnabled = source.GetBool(ConfigurationKeys.Splunk.Snapshots.Enabled) ?? false;
        HighResolutionTimerEnabled = source.GetBool(ConfigurationKeys.Splunk.Snapshots.HighResolutionTimerEnabled) ?? false;

        var snapshotInterval = source.GetInt32(ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs) ?? DefaultSnapshotSamplingIntervalMs;
        SnapshotsSamplingInterval = snapshotInterval <= 0 ? DefaultSnapshotSamplingIntervalMs : snapshotInterval;
        var configuredSelectionRate = source.GetDouble(ConfigurationKeys.Splunk.Snapshots.SelectionRate) ?? DefaultSnapshotSelectionRate;
        SnapshotsSelectionRate = GetFinalSnapshotSelectionProbability(configuredSelectionRate);
        MemoryProfilerEnabled = source.GetBool(ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled) ?? false;
        var callStackInterval = source.GetInt32(ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval) ?? 10000;
        CpuProfilerCallStackInterval = callStackInterval < 0 ? 10000u : (uint)callStackInterval;
        var maxMemorySamplesPerMinute = source.GetInt32(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples) ?? 200;
        MemoryProfilerMaxMemorySamplesPerMinute = maxMemorySamplesPerMinute > 200 ? 200u : (uint)maxMemorySamplesPerMinute;
        var httpClientTimeout = source.GetInt32(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerExportTimeout) ?? 3000;
        ProfilerHttpClientTimeout = (uint)httpClientTimeout;
        var exportInterval = source.GetInt32(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerExportInterval) ?? 500;
        ProfilerExportInterval = exportInterval < 500 ? 500u : (uint)exportInterval;

        ProfilerLogsEndpoint = GetProfilerLogsEndpoints(source, otlpEndpoint == null ? null : new Uri(otlpEndpoint));
#endif
    }

    internal PluginSettings(SplunkConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        Realm = Constants.None;
        AccessToken = null;
        IsOtlpEndpointSet = false;
        TraceResponseHeaderEnabled = configuration.ResponseHeaderEnabled;

#if NET
        var profiler = configuration.Profiling;
        if (profiler != null)
        {
            CpuProfilerEnabled = true;
            MemoryProfilerEnabled = profiler.MemoryEnabled;
            ProfilerLogsEndpoint = new Uri(profiler.LogsEndpoint);
            var exportInterval = profiler.ExportInterval;
            ProfilerExportInterval = exportInterval < 500u ? 500u : exportInterval;

            ProfilerHttpClientTimeout = profiler.ExportTimeout;

            var maxMemorySamplesPerMinute = profiler.MaxMemorySamples;
            MemoryProfilerMaxMemorySamplesPerMinute = maxMemorySamplesPerMinute > 200u ? 200u : maxMemorySamplesPerMinute;

            var callStackInterval = profiler.CallStackInterval;
            CpuProfilerCallStackInterval = callStackInterval < 0u ? 10000u : callStackInterval;
        }

        var callGraphs = configuration.Callgraphs;
        if (callGraphs != null)
        {
            SnapshotsEnabled = true;

            SnapshotsSamplingInterval = callGraphs.SamplingInterval;
            var configuredSelectionRate = callGraphs.SelectionProbability;
            SnapshotsSelectionRate = configuredSelectionRate > MaxSnapshotSelectionRate ? MaxSnapshotSelectionRate : configuredSelectionRate;
        }
#endif
    }

    public int SnapshotsSamplingInterval { get; set; }

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

    public Uri ProfilerLogsEndpoint { get; } = new Uri("http://localhost:4318/v1/logs");

    public uint ProfilerHttpClientTimeout { get; }

    public uint ProfilerExportInterval { get; }
#endif

    public static PluginSettings FromDefaultSources()
    {
        Console.WriteLine("FromDefaultSources");
        Debugger.Launch();
        if (IsYamlConfigEnabled)
        {
            var fileName = Environment.GetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.FileName) ?? "config.yaml";

            var splunkConfiguration = LoadSplunkConfig(fileName);
            if (splunkConfiguration != null)
            {
                return new PluginSettings(splunkConfiguration);
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

    private static double GetFinalSnapshotSelectionProbability(double configuredSelectionRate)
    {
        return configuredSelectionRate switch
        {
            <= 0 => DefaultSnapshotSelectionRate,
            > MaxSnapshotSelectionRate => MaxSnapshotSelectionRate,
            _ => configuredSelectionRate
        };
    }

#if NET
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

    private static SplunkConfiguration? LoadSplunkConfig(string fileName)
    {
        Debugger.Launch();
        var parserTypeFullName = "OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser";

        var assembly = AppDomain.CurrentDomain
            .GetAssemblies();

        foreach (var asm in assembly)
        {
            Console.WriteLine($"Assembly: {asm.FullName}");
        }

        var parserType = assembly
            .Select(a => a.GetType(parserTypeFullName, throwOnError: false, ignoreCase: false))
            .FirstOrDefault(t => t != null);

        if (parserType == null)
        {
            throw new TypeLoadException($"Could not find Parser type for YAML configuration parsing.");
        }

        var parseYaml = parserType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(m =>
                m.Name == "ParseYaml" &&
                m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(string));

        if (parseYaml == null)
        {
            throw new MissingMethodException(parserType.FullName, "ParseYaml<T>(string)");
        }

        var closed = parseYaml.MakeGenericMethod(typeof(SplunkWrapper));

        var wrapperObj = closed.Invoke(null, [fileName]);
        if (wrapperObj == null)
        {
            return null;
        }

        var splunkProp = wrapperObj.GetType().GetProperty("Splunk", BindingFlags.Public | BindingFlags.Instance);

        return (SplunkConfiguration?)splunkProp?.GetValue(wrapperObj);
    }
}
