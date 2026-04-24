// <copyright file="EffectiveConfigReader.cs" company="Splunk Inc.">
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

using System.Collections;
using System.Reflection;
using System.Text;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Logging.EffectiveConfig;

internal static class EffectiveConfigReader
{
    internal const string EffectiveConfigStart = "Effective configuration start:";
    internal const string EffectiveConfigEnd = "Effective configuration end.";

    private const string InstrumentationTypeName = "OpenTelemetry.AutoInstrumentation.Instrumentation, OpenTelemetry.AutoInstrumentation";
    private const string DefaultHttpBaseEndpoint = "http://localhost:4318";
    private const string DefaultGrpcBaseEndpoint = "http://localhost:4317";
    private static readonly ILogger Log = new Logger();

    public static IReadOnlyDictionary<string, string> Read(PluginSettings settings)
    {
        var config = new Dictionary<string, string>();
        PopulateSplunkSettings(config, settings);
        PopulateUpstreamSettings(config);
        return config;
    }

    public static string Format(IReadOnlyDictionary<string, string> config)
    {
        var sb = new StringBuilder();
        foreach (var kvp in config)
        {
            sb.AppendLine($"{kvp.Key}={kvp.Value}");
        }

        return sb.ToString();
    }

    // Unlike OTLP endpoints, service name is not populated into ResourceSettings on the env-var path —
    // upstream reads OTEL_SERVICE_NAME/OTEL_RESOURCE_ATTRIBUTES later via AddEnvironmentVariableDetector().
    // So we read env vars directly first, then fall back to ResourceSettings (yaml path only).
    internal static string? ResolveServiceName(Type? instrumentationType)
    {
        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME");
        if (!string.IsNullOrEmpty(serviceName))
        {
            return serviceName;
        }

        var resourceAttributes = Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES");
        if (!string.IsNullOrEmpty(resourceAttributes))
        {
            var fromEnv = ResourceAttributesHelper.ParseServiceName(resourceAttributes);
            if (fromEnv != null)
            {
                return fromEnv;
            }
        }

        if (instrumentationType != null)
        {
            try
            {
                var fromResource = ReadServiceNameFromResourceSettings(instrumentationType);
                if (fromResource != null)
                {
                    return fromResource;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to read ResourceSettings.Resources (OTEL_SERVICE_NAME): {ex.Message}");
            }
        }

        return GetFallbackServiceName(instrumentationType);
    }

    // Fallback for when reflection finds no resolved endpoint (e.g. exporter is none).
    // Resolution chain: signal-specific env var → base env var + signal suffix → spec default.
    // Protocol (signal-specific → base → default http/protobuf) determines port and path for grpc.
    // Uri.TryCreate mirrors upstream's Configuration.GetUri — invalid URIs are discarded.
    internal static string ResolveOtlpEndpointFallback(string signalEndpointEnvVar, string protocolEnvVar, string httpSignalPathSuffix)
    {
        var signalEndpoint = Environment.GetEnvironmentVariable(signalEndpointEnvVar);
        if (!string.IsNullOrEmpty(signalEndpoint) && Uri.TryCreate(signalEndpoint, UriKind.Absolute, out var signalUri))
        {
            return signalUri.ToString().TrimEnd('/');
        }

        var isGrpc = ResolveIsGrpc(protocolEnvVar);

        var baseEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (!string.IsNullOrEmpty(baseEndpoint) && Uri.TryCreate(baseEndpoint, UriKind.Absolute, out var baseUri))
        {
            var normalized = baseUri.ToString().TrimEnd('/');
            return isGrpc
                ? normalized
                : normalized + httpSignalPathSuffix;
        }

        return isGrpc
            ? DefaultGrpcBaseEndpoint
            : DefaultHttpBaseEndpoint + httpSignalPathSuffix;
    }

    // Mirrors upstream's GetExporterOtlpProtocol: signal-specific protocol env var → base → default http/protobuf.
    private static bool ResolveIsGrpc(string protocolEnvVar)
    {
        var protocol = Environment.GetEnvironmentVariable(protocolEnvVar)
                       ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL");
        return string.Equals(protocol, "grpc", StringComparison.OrdinalIgnoreCase);
    }

    // Tries OtlpSettings.Endpoint first (populated on env-var path), then falls back to
    // walking Processors/Readers (yaml path, where OtlpSettings is not populated).
    private static string? ReadOtlpEndpoint(Type instrumentationType, string settingsPropertyName)
    {
        var settings = GetLazySettingsValue(instrumentationType, settingsPropertyName);
        if (settings == null)
        {
            return null;
        }

        var otlpProp = settings.GetType().GetProperty("OtlpSettings", BindingFlags.Public | BindingFlags.Instance);
        var otlpSettings = otlpProp?.GetValue(settings);
        if (otlpSettings != null)
        {
            var endpointProp = otlpSettings.GetType().GetProperty("Endpoint", BindingFlags.Public | BindingFlags.Instance);
            var endpoint = endpointProp?.GetValue(otlpSettings);
            if (endpoint != null)
            {
                return endpoint.ToString();
            }
        }

        return ReadOtlpEndpointFromFileConfig(settings);
    }

    private static object? GetLazySettingsValue(Type instrumentationType, string settingsPropertyName)
    {
        var lazyProp = instrumentationType.GetProperty(settingsPropertyName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        var lazyObj = lazyProp?.GetValue(null);
        var valueProp = lazyObj?.GetType().GetProperty("Value");
        return valueProp?.GetValue(lazyObj);
    }

    // Yaml object graph (all types internal to upstream):
    //   TracerSettings.Processors / LogSettings.Processors → IReadOnlyList<ProcessorConfig>
    //     .Batch/.Simple → .Exporter.OtlpHttp.Endpoint or .OtlpGrpc.Endpoint
    //   MetricSettings.Readers → IReadOnlyList<MetricReaderConfig>
    //     .Periodic → .Exporter.OtlpHttp.Endpoint or .OtlpGrpc.Endpoint
    // Returns the first OTLP endpoint found.
    private static string? ReadOtlpEndpointFromFileConfig(object settings)
    {
        var processorsProp = settings.GetType().GetProperty("Processors", BindingFlags.Public | BindingFlags.Instance)
                          ?? settings.GetType().GetProperty("Readers", BindingFlags.Public | BindingFlags.Instance);

        if (processorsProp?.GetValue(settings) is not IEnumerable items)
        {
            return null;
        }

        foreach (var item in items)
        {
            var endpoint = ReadOtlpEndpointFromProcessorOrReader(item);
            if (endpoint != null)
            {
                return endpoint;
            }
        }

        return null;
    }

    private static string? ReadOtlpEndpointFromProcessorOrReader(object processorOrReader)
    {
        foreach (var subPropName in new[] { "Batch", "Simple", "Periodic" })
        {
            var subProp = processorOrReader.GetType().GetProperty(subPropName, BindingFlags.Public | BindingFlags.Instance);
            var subConfig = subProp?.GetValue(processorOrReader);
            if (subConfig == null)
            {
                continue;
            }

            var exporterProp = subConfig.GetType().GetProperty("Exporter", BindingFlags.Public | BindingFlags.Instance);
            var exporter = exporterProp?.GetValue(subConfig);
            if (exporter == null)
            {
                continue;
            }

            var endpoint = ReadEndpointFromExporterConfig(exporter);
            if (endpoint != null)
            {
                return endpoint;
            }
        }

        return null;
    }

    private static string? ReadEndpointFromExporterConfig(object exporterConfig)
    {
        foreach (var exporterPropName in new[] { "OtlpHttp", "OtlpGrpc" })
        {
            var otlpProp = exporterConfig.GetType().GetProperty(exporterPropName, BindingFlags.Public | BindingFlags.Instance);
            var otlpConfig = otlpProp?.GetValue(exporterConfig);
            if (otlpConfig == null)
            {
                continue;
            }

            var endpointProp = otlpConfig.GetType().GetProperty("Endpoint", BindingFlags.Public | BindingFlags.Instance);
            var endpoint = endpointProp?.GetValue(otlpConfig);
            if (endpoint != null)
            {
                return endpoint.ToString();
            }
        }

        return null;
    }

    private static string? ReadServiceNameFromResourceSettings(Type instrumentationType)
    {
        var resourceSettings = GetLazySettingsValue(instrumentationType, "ResourceSettings");
        if (resourceSettings == null)
        {
            return null;
        }

        var resourcesProp = resourceSettings.GetType().GetProperty("Resources", BindingFlags.Public | BindingFlags.Instance);
        if (resourcesProp?.GetValue(resourceSettings) is not IEnumerable<KeyValuePair<string, object>> resources)
        {
            return null;
        }

        foreach (var kvp in resources)
        {
            if (string.Equals(kvp.Key, "service.name", StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value?.ToString();
            }
        }

        return null;
    }

    private static string? GetFallbackServiceName(Type? instrumentationType)
    {
        if (instrumentationType == null)
        {
            return null;
        }

        try
        {
            var configuratorType = instrumentationType.Assembly.GetType("OpenTelemetry.AutoInstrumentation.Configurations.ServiceNameConfigurator");
            var method = configuratorType?.GetMethod("GetFallbackServiceName", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            return method?.Invoke(null, null) as string;
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to read ServiceNameConfigurator.GetFallbackServiceName (OTEL_SERVICE_NAME): {ex.Message}");
            return null;
        }
    }

    private static void PopulateSplunkSettings(Dictionary<string, string> config, PluginSettings settings)
    {
        config[ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled] = settings.CpuProfilerEnabled.ToString();
#if NET
        config[ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled] = settings.MemoryProfilerEnabled.ToString();
#endif
        config[ConfigurationKeys.Splunk.Snapshots.Enabled] = settings.SnapshotsEnabled.ToString();
        config[ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs] = settings.SnapshotsSamplingInterval.ToString();
        config[ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval] = settings.CpuProfilerCallStackInterval.ToString();
    }

    private static void PopulateUpstreamSettings(Dictionary<string, string> config)
    {
        var instrumentationType = Type.GetType(InstrumentationTypeName);
        if (instrumentationType == null)
        {
            Log.Warning("Upstream instrumentation type not found. Upstream settings will be omitted from effective configuration.");
            return;
        }

        PopulateOtlpEndpoint(config, "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", instrumentationType, "TracerSettings", "OTEL_EXPORTER_OTLP_TRACES_PROTOCOL", "/v1/traces");
        PopulateOtlpEndpoint(config, "OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", instrumentationType, "MetricSettings", "OTEL_EXPORTER_OTLP_METRICS_PROTOCOL", "/v1/metrics");
        PopulateOtlpEndpoint(config, "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", instrumentationType, "LogSettings", "OTEL_EXPORTER_OTLP_LOGS_PROTOCOL", "/v1/logs");

        var serviceName = ResolveServiceName(instrumentationType);
        if (serviceName != null)
        {
            config["OTEL_SERVICE_NAME"] = serviceName;
        }
        else
        {
            Log.Warning("Failed to resolve OTEL_SERVICE_NAME: value not found.");
        }
    }

    private static void PopulateOtlpEndpoint(Dictionary<string, string> config, string envVarName, Type instrumentationType, string settingsPropertyName, string protocolEnvVar, string httpSignalPathSuffix)
    {
        try
        {
            var value = ReadOtlpEndpoint(instrumentationType, settingsPropertyName)
                        ?? ResolveOtlpEndpointFallback(envVarName, protocolEnvVar, httpSignalPathSuffix);
            config[envVarName] = value;
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to read {settingsPropertyName}.Endpoint ({envVarName}): {ex.Message}");
        }
    }
}
