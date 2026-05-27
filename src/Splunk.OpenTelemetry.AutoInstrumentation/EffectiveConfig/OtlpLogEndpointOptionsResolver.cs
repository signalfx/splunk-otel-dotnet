// <copyright file="OtlpLogEndpointOptionsResolver.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal static class OtlpLogEndpointOptionsResolver
{
    private const string LogsHttpSignalPath = "v1/logs";
    private const string LogsGrpcSignalPath = "opentelemetry.proto.collector.logs.v1.LogsService/Export";

    private static readonly PropertyInfo? AppendSignalPathToEndpointProperty =
        typeof(OtlpExporterOptions).GetProperty("AppendSignalPathToEndpoint", BindingFlags.Instance | BindingFlags.NonPublic);

    public static string? ResolveEndpoint(OtlpExporterOptions options)
    {
        // ILogger options are captured before SDK export clients add signal-specific paths.
#pragma warning disable CS0618 // OtlpExportProtocol.Grpc is obsolete but still used by the SDK and specification.
        if (options.Protocol == OtlpExportProtocol.Grpc)
#pragma warning restore CS0618
        {
            // Mirror the SDK's final gRPC logs endpoint path.
            return Format(AppendPathIfNotPresent(options.Endpoint, LogsGrpcSignalPath));
        }

        if (options.Protocol != OtlpExportProtocol.HttpProtobuf)
        {
            return null;
        }

        var appendSignalPathToEndpoint = TryGetAppendSignalPathToEndpoint(options);
        if (appendSignalPathToEndpoint == null)
        {
            // HTTP path behavior depends on a private SDK flag; omit rather than guess.
            return null;
        }

        var endpoint = appendSignalPathToEndpoint.Value
            ? AppendPathIfNotPresent(options.Endpoint, LogsHttpSignalPath)
            : options.Endpoint;

        return Format(endpoint);
    }

    private static bool? TryGetAppendSignalPathToEndpoint(OtlpExporterOptions options)
    {
        return AppendSignalPathToEndpointProperty?.GetValue(options) is bool appendSignalPathToEndpoint
            ? appendSignalPathToEndpoint
            : null;
    }

    private static Uri AppendPathIfNotPresent(Uri endpoint, string path)
    {
        // Explicit signal endpoints should not get the same path appended twice.
        var absoluteUri = endpoint.AbsoluteUri;
        var separator = string.Empty;

        if (absoluteUri.EndsWith("/", StringComparison.Ordinal))
        {
            if (absoluteUri.EndsWith(string.Concat(path, "/"), StringComparison.OrdinalIgnoreCase))
            {
                return endpoint;
            }
        }
        else
        {
            if (absoluteUri.EndsWith(path, StringComparison.OrdinalIgnoreCase))
            {
                return endpoint;
            }

            separator = "/";
        }

        return new Uri(string.Concat(endpoint.AbsoluteUri, separator, path));
    }

    private static string Format(Uri endpoint)
    {
        return new UriBuilder(endpoint).Uri.AbsoluteUri;
    }
}
