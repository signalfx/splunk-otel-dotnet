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
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal static class OtlpEndpointProviderGraphResolver
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static IReadOnlyList<string> ResolveTraceEndpoints(TracerProvider provider)
    {
        var processor = GetPropertyValue(provider, "Processor");
        return ResolveEndpointsFromPipeline(processor, "OpenTelemetry.Exporter.OtlpTraceExporter");
    }

    public static IReadOnlyList<string> ResolveMetricEndpoints(MeterProvider provider)
    {
        var reader = GetPropertyValue(provider, "Reader");
        return ResolveEndpointsFromPipeline(reader, "OpenTelemetry.Exporter.OtlpMetricExporter");
    }

    private static IReadOnlyList<string> ResolveEndpointsFromPipeline(object? pipeline, string exporterTypeName)
    {
        if (pipeline == null)
        {
            return [];
        }

        var endpoints = new List<string>();
        foreach (var pipelineItem in FlattenPipeline(pipeline))
        {
            var exporter = GetExporter(pipelineItem);
            if (exporter?.GetType().FullName != exporterTypeName)
            {
                continue;
            }

            endpoints.Add(ResolveEndpoint(exporter));
        }

        return endpoints;
    }

    private static IEnumerable<object> FlattenPipeline(object pipeline)
    {
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

    private static string ResolveEndpoint(object exporter)
    {
        var transmissionHandler = GetRequiredFieldValue(exporter, "transmissionHandler");
        var exportClient = GetRequiredPropertyValue(transmissionHandler, "ExportClient");
        var endpoint = GetRequiredPropertyValue(exportClient, "Endpoint") as Uri
            ?? throw new InvalidOperationException($"Failed to read OTLP exporter endpoint from {exportClient.GetType().FullName}.");

        return FormatForLogging(endpoint);
    }

    private static string FormatForLogging(Uri endpoint)
    {
        return new UriBuilder(endpoint).Uri.ToString().TrimEnd('/');
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
