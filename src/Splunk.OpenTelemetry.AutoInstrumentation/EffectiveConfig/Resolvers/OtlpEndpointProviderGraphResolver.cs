// <copyright file="OtlpEndpointProviderGraphResolver.cs" company="Splunk Inc.">
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
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Resolvers;

// Built SDK providers hold final OTLP endpoints after defaults and signal paths are applied.
internal static class OtlpEndpointProviderGraphResolver
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static IReadOnlyList<EffectiveOtlpEndpoint> ResolveTraceEndpoints(TracerProvider provider)
    {
        var processor = GetPropertyValue(provider, "Processor");
        return ResolveEndpointsFromPipeline(processor, typeof(OtlpTraceExporter));
    }

    public static IReadOnlyList<EffectiveOtlpEndpoint> ResolveMetricEndpoints(MeterProvider provider)
    {
        var reader = GetPropertyValue(provider, "Reader");
        return ResolveEndpointsFromPipeline(reader, typeof(OtlpMetricExporter));
    }

    public static IReadOnlyList<EffectiveOtlpEndpoint> ResolveLogEndpoints(LoggerProvider provider)
    {
        var processor = GetPropertyValue(provider, "Processor");
        return ResolveEndpointsFromPipeline(processor, typeof(OtlpLogExporter));
    }

    private static IReadOnlyList<EffectiveOtlpEndpoint> ResolveEndpointsFromPipeline(object? pipeline, Type exporterType)
    {
        if (pipeline == null)
        {
            return [];
        }

        var endpoints = new List<EffectiveOtlpEndpoint>();
        foreach (var pipelineItem in FlattenPipeline(pipeline))
        {
            var exporter = GetExporter(pipelineItem);
            // SDK type filtering prevents reading endpoints from non-OTLP exporters.
            if (exporter == null || exporter.GetType() != exporterType)
            {
                continue;
            }

            var endpoint = ResolveEndpoint(exporter);
            if (endpoint != null)
            {
                endpoints.Add(endpoint.Value);
            }
        }

        return endpoints;
    }

    private static IEnumerable<object> FlattenPipeline(object pipeline)
    {
        // Single exporters are stored directly; multiple exporters use a linked-list pipeline.
        var head = GetFieldValue(pipeline, "Head");
        if (head == null)
        {
            yield return pipeline;
            yield break;
        }

        for (var node = head; node != null; node = GetPropertyValue(node, "Next"))
        {
            var value = GetFieldValue(node, "Value");
            if (value != null)
            {
                yield return value;
            }
        }
    }

    private static object? GetExporter(object pipelineItem)
    {
        return GetFieldValue(pipelineItem, "exporter");
    }

    private static EffectiveOtlpEndpoint? ResolveEndpoint(object exporter)
    {
        // ExportClient.Endpoint is the final endpoint used by the SDK exporter.
        var transmissionHandler = GetRequiredFieldValue(exporter, "transmissionHandler");
        var exportClient = GetRequiredPropertyValue(transmissionHandler, "ExportClient");
        var endpoint = GetRequiredPropertyValue(exportClient, "Endpoint") as Uri
            ?? throw new InvalidOperationException($"Failed to read OTLP exporter endpoint from {exportClient.GetType().FullName}.");

        var exporterType = ResolveExporterType(exportClient);
        return exporterType == null
            ? null
            : new EffectiveOtlpEndpoint(endpoint.AbsoluteUri, exporterType.Value);
    }

    private static EffectiveOtlpExporterType? ResolveExporterType(object exportClient)
    {
        // The SDK's export-client implementation preserves the selected transport after options have been applied.
        return exportClient.GetType().FullName switch
        {
            "OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation.ExportClient.OtlpHttpExportClient" => EffectiveOtlpExporterType.HttpProtobuf,
            "OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation.ExportClient.OtlpGrpcExportClient" => EffectiveOtlpExporterType.Grpc,
            _ => null
        };
    }

    private static object? GetPropertyValue(object source, string name)
    {
        var property = FindProperty(source.GetType(), name);
        return property?.GetValue(source);
    }

    private static object GetRequiredPropertyValue(object source, string name)
    {
        var property = FindProperty(source.GetType(), name)
            ?? throw new MissingMemberException(source.GetType().FullName, name);

        return property.GetValue(source)
            ?? throw new InvalidOperationException($"Property {source.GetType().FullName}.{name} returned null.");
    }

    private static object? GetFieldValue(object source, string name)
    {
        var field = FindField(source.GetType(), name);
        return field?.GetValue(source);
    }

    private static object GetRequiredFieldValue(object source, string name)
    {
        var field = FindField(source.GetType(), name)
            ?? throw new MissingMemberException(source.GetType().FullName, name);

        return field.GetValue(source)
            ?? throw new InvalidOperationException($"Field {source.GetType().FullName}.{name} returned null.");
    }

    private static PropertyInfo? FindProperty(Type type, string name)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            var property = current.GetProperty(name, InstanceFlags);
            if (property != null)
            {
                return property;
            }
        }

        return null;
    }

    private static FieldInfo? FindField(Type type, string name)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            var field = current.GetField(name, InstanceFlags);
            if (field != null)
            {
                return field;
            }
        }

        return null;
    }
}
