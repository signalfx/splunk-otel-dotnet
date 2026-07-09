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

using System.Diagnostics;
using System.Reflection;
using OpenTelemetry;
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
        var processor = GetRequiredNullablePropertyValue(provider, "Processor");
        return ResolveEndpointsFromPipeline(
            processor,
            typeof(OtlpTraceExporter),
            ResolveTracePipelineType,
            static pipelineItem => pipelineItem is BaseExportProcessor<Activity>,
            ResolveEndpoint);
    }

    public static IReadOnlyList<EffectiveOtlpEndpoint> ResolveMetricEndpoints(MeterProvider provider)
    {
        var reader = GetRequiredNullablePropertyValue(provider, "Reader");
        return ResolveEndpointsFromPipeline(
            reader,
            typeof(OtlpMetricExporter),
            ResolveMetricPipelineType,
            static pipelineItem => pipelineItem is BaseExportingMetricReader,
            ResolveEndpoint);
    }

    public static IReadOnlyList<EffectiveOtlpEndpoint> ResolveLogEndpoints(LoggerProvider provider)
    {
        var processor = GetRequiredNullablePropertyValue(provider, "Processor");
        return ResolveEndpointsFromPipeline(
            processor,
            typeof(OtlpLogExporter),
            ResolveLogPipelineType,
            static pipelineItem => pipelineItem is BaseExportProcessor<LogRecord>,
            ResolveLogEndpoint);
    }

    private static IReadOnlyList<EffectiveOtlpEndpoint> ResolveEndpointsFromPipeline(
        object? pipeline,
        Type exporterType,
        Func<object, EffectiveOtlpPipelineType?> pipelineTypeResolver,
        Func<object, bool> isExportingPipelineItem,
        Func<object, EffectiveOtlpPipelineType, EffectiveOtlpEndpoint> endpointResolver)
    {
        if (pipeline == null)
        {
            return [];
        }

        var endpoints = new List<EffectiveOtlpEndpoint>();
        foreach (var pipelineItem in FlattenPipeline(pipeline))
        {
            var pipelineType = pipelineTypeResolver(pipelineItem);
            if (pipelineType == null)
            {
                var unknownPipelineExporter = GetFieldValue(pipelineItem, "exporter");
                if (unknownPipelineExporter != null)
                {
                    if (exporterType.IsInstanceOfType(unknownPipelineExporter))
                    {
                        throw new InvalidOperationException(
                            $"The active OTLP exporter uses unsupported pipeline type {pipelineItem.GetType().FullName}.");
                    }

                    continue;
                }

                if (isExportingPipelineItem(pipelineItem))
                {
                    throw new InvalidOperationException(
                        $"Failed to read the exporter from pipeline type {pipelineItem.GetType().FullName}.");
                }

                if (IsOpenTelemetrySdkType(pipelineItem.GetType()))
                {
                    throw new InvalidOperationException($"The SDK uses unsupported pipeline type {pipelineItem.GetType().FullName}.");
                }

                continue;
            }

            var exporter = GetExporter(pipelineItem);
            // SDK type filtering prevents reading endpoints from non-OTLP exporters.
            if (!exporterType.IsInstanceOfType(exporter))
            {
                continue;
            }

            if (exporter.GetType() != exporterType)
            {
                throw new InvalidOperationException(
                    $"The active OTLP exporter uses unsupported exporter type {exporter.GetType().FullName}.");
            }

            endpoints.Add(endpointResolver(exporter, pipelineType.Value));
        }

        return endpoints;
    }

    private static IEnumerable<object> FlattenPipeline(object pipeline)
    {
        // Single exporters are stored directly; multiple exporters use a linked-list pipeline.
        var pipelineType = pipeline.GetType();
        if (!IsSupportedCompositePipelineType(pipelineType))
        {
            yield return pipeline;
            yield break;
        }

        var headField = FindField(pipelineType, "Head")
            ?? throw new MissingMemberException(pipelineType.FullName, "Head");
        var head = headField.GetValue(pipeline)
            ?? throw new InvalidOperationException($"Field {pipelineType.FullName}.Head returned null.");
        for (var node = head; node != null; node = GetRequiredNullablePropertyValue(node, "Next"))
        {
            yield return GetRequiredFieldValue(node, "Value");
        }
    }

    private static object GetExporter(object pipelineItem)
    {
        return GetRequiredFieldValue(pipelineItem, "exporter");
    }

    private static EffectiveOtlpEndpoint ResolveEndpoint(
        object exporter,
        EffectiveOtlpPipelineType pipelineType)
    {
        return CreateEndpoint(GetExportClient(exporter), pipelineType);
    }

    private static EffectiveOtlpEndpoint ResolveLogEndpoint(
        object exporter,
        EffectiveOtlpPipelineType pipelineType)
    {
        return CreateEndpoint(UnwrapLazyExportClient(GetExportClient(exporter)), pipelineType);
    }

    private static object GetExportClient(object exporter)
    {
        var transmissionHandler = GetRequiredFieldValue(exporter, "transmissionHandler");
        return GetRequiredPropertyValue(transmissionHandler, "ExportClient");
    }

    private static EffectiveOtlpEndpoint CreateEndpoint(
        object exportClient,
        EffectiveOtlpPipelineType pipelineType)
    {
        // The concrete export client's Endpoint is the final endpoint used by the SDK exporter.
        var endpoint = GetRequiredPropertyValue(exportClient, "Endpoint") as Uri
            ?? throw new InvalidOperationException($"Failed to read OTLP exporter endpoint from {exportClient.GetType().FullName}.");

        var exporterType = ResolveExporterType(exportClient);
        return new EffectiveOtlpEndpoint(endpoint.AbsoluteUri, exporterType, pipelineType);
    }

    private static object UnwrapLazyExportClient(object exportClient)
    {
        const string lazyExportClientTypeName = "OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation.ExportClient.LazyExportClient";
        if (exportClient.GetType().FullName != lazyExportClientTypeName)
        {
            return exportClient;
        }

        var lazy = GetRequiredFieldValue(exportClient, "exportClient");
        if (!lazy.GetType().IsGenericType || lazy.GetType().GetGenericTypeDefinition() != typeof(Lazy<>))
        {
            throw new InvalidOperationException(
                $"The SDK's {lazyExportClientTypeName} uses unsupported storage type {lazy.GetType().FullName}.");
        }

        return GetRequiredPropertyValue(lazy, "Value");
    }

    private static EffectiveOtlpPipelineType? ResolveTracePipelineType(object pipelineItem)
    {
        if (pipelineItem is BatchActivityExportProcessor)
        {
            return EffectiveOtlpPipelineType.Batch;
        }

        return pipelineItem is SimpleActivityExportProcessor
            ? EffectiveOtlpPipelineType.Simple
            : null;
    }

    private static EffectiveOtlpPipelineType? ResolveLogPipelineType(object pipelineItem)
    {
        if (pipelineItem is BatchLogRecordExportProcessor)
        {
            return EffectiveOtlpPipelineType.Batch;
        }

        return pipelineItem is SimpleLogRecordExportProcessor
            ? EffectiveOtlpPipelineType.Simple
            : null;
    }

    private static EffectiveOtlpPipelineType? ResolveMetricPipelineType(object pipelineItem)
    {
        return pipelineItem is PeriodicExportingMetricReader
            ? EffectiveOtlpPipelineType.Periodic
            : null;
    }

    private static bool IsSupportedCompositePipelineType(Type type)
    {
        if (!IsOpenTelemetrySdkType(type))
        {
            return false;
        }

        return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(CompositeProcessor<>)) ||
            type.FullName == "OpenTelemetry.Metrics.CompositeMetricReader";
    }

    private static bool IsOpenTelemetrySdkType(Type type)
    {
        return type.Assembly == typeof(BaseProcessor<>).Assembly;
    }

    private static EffectiveOtlpExporterType ResolveExporterType(object exportClient)
    {
        // The SDK's export-client implementation preserves the selected transport after options have been applied.
        return exportClient.GetType().FullName switch
        {
            "OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation.ExportClient.OtlpHttpExportClient" => EffectiveOtlpExporterType.HttpProtobuf,
            "OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation.ExportClient.OtlpGrpcExportClient" => EffectiveOtlpExporterType.Grpc,
            _ => throw new InvalidOperationException(
                $"The active OTLP exporter uses unsupported export client type {exportClient.GetType().FullName}.")
        };
    }

    private static object? GetRequiredNullablePropertyValue(object source, string name)
    {
        var property = FindProperty(source.GetType(), name)
            ?? throw new MissingMemberException(source.GetType().FullName, name);

        return property.GetValue(source);
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
