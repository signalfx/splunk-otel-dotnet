// <copyright file="OtlpEndpointResolver.cs" company="Splunk Inc.">
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

using System.Reflection;
using OpenTelemetry.Exporter;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal static class OtlpEndpointResolver
{
    private const string TraceHttpSignalPath = "v1/traces";
    private const string MetricsHttpSignalPath = "v1/metrics";
    private const string LogsHttpSignalPath = "v1/logs";
    private const string TraceGrpcSignalPath = "opentelemetry.proto.collector.trace.v1.TraceService/Export";
    private const string MetricsGrpcSignalPath = "opentelemetry.proto.collector.metrics.v1.MetricsService/Export";
    private const string LogsGrpcSignalPath = "opentelemetry.proto.collector.logs.v1.LogsService/Export";

    private static readonly PropertyInfo? AppendSignalPathToEndpointProperty =
        typeof(OtlpExporterOptions).GetProperty("AppendSignalPathToEndpoint", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly ILogger Log = new Logger();

    internal static string? ResolveFromOptions(OtlpExporterOptions options, string configurationKey)
    {
        var signalPaths = GetSignalPaths(configurationKey);
        if (signalPaths == null)
        {
            return null;
        }

        // Endpoint getter returns the SDK default when no endpoint was configured.
        // File-based config resolves omitted endpoints before values are copied here.
#pragma warning disable CS0618 // OtlpExportProtocol.Grpc is obsolete but still supported by upstream auto-instrumentation.
        if (options.Protocol == OtlpExportProtocol.Grpc)
#pragma warning restore CS0618
        {
            return AppendPathIfNotPresent(options.Endpoint, signalPaths.Value.Grpc);
        }

        if (options.Protocol != OtlpExportProtocol.HttpProtobuf)
        {
            Log.Warning($"Unsupported OTLP protocol '{options.Protocol}'. {configurationKey} will be omitted from effective configuration.");
            return null;
        }

        var appendSignalPath = TryReadAppendSignalPathToEndpoint(options);
        if (appendSignalPath == null)
        {
            Log.Warning($"Failed to read OtlpExporterOptions.AppendSignalPathToEndpoint. {configurationKey} will be omitted from effective configuration.");
            return null;
        }

        return appendSignalPath.Value
            ? AppendPathIfNotPresent(options.Endpoint, signalPaths.Value.Http)
            : NormalizeEndpoint(options.Endpoint);
    }

    private static (string Http, string Grpc)? GetSignalPaths(string configurationKey)
    {
        return configurationKey switch
        {
            EffectiveConfigKeys.TracesEndpoint => (TraceHttpSignalPath, TraceGrpcSignalPath),
            EffectiveConfigKeys.MetricsEndpoint => (MetricsHttpSignalPath, MetricsGrpcSignalPath),
            EffectiveConfigKeys.LogsEndpoint => (LogsHttpSignalPath, LogsGrpcSignalPath),
            _ => null
        };
    }

    private static bool? TryReadAppendSignalPathToEndpoint(OtlpExporterOptions options)
    {
        return AppendSignalPathToEndpointProperty?.GetValue(options) is bool value
            ? value
            : null;
    }

    private static string AppendPathIfNotPresent(Uri endpoint, string path)
    {
        var absoluteUri = endpoint.AbsoluteUri;
        var separator = string.Empty;

        // Match the upstream SDK helper. It treats already-appended signal paths
        // case-insensitively, so effective config reporting must do the same.
        if (absoluteUri.EndsWith("/", StringComparison.Ordinal))
        {
            if (absoluteUri.EndsWith(string.Concat(path, "/"), StringComparison.OrdinalIgnoreCase))
            {
                return NormalizeEndpoint(endpoint);
            }
        }
        else
        {
            if (absoluteUri.EndsWith(path, StringComparison.OrdinalIgnoreCase))
            {
                return NormalizeEndpoint(endpoint);
            }

            separator = "/";
        }

        return NormalizeEndpoint(new Uri(string.Concat(absoluteUri, separator, path)));
    }

    private static string NormalizeEndpoint(Uri endpoint)
    {
        // The SDK constructs the final exporter endpoint through UriBuilder.
        // Trim the trailing slash only to keep debug log output stable.
        return new UriBuilder(endpoint).Uri.ToString().TrimEnd('/');
    }
}
