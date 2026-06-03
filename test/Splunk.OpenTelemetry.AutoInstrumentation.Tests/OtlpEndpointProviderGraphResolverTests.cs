// <copyright file="OtlpEndpointProviderGraphResolverTests.cs" company="Splunk Inc.">
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
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Model;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Resolvers;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class OtlpEndpointProviderGraphResolverTests
{
    [Fact]
    public void ResolveTraceEndpoints_ReturnsOtlpExporterEndpoint()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddOtlpExporter(options => ConfigureHttpEndpoint(options, "http://collector:4318/custom-traces"))
            .Build();

        Assert.Equal(
            [EffectiveOtlpEndpoint.Http("http://collector:4318/custom-traces")],
            OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints(provider));
    }

    [Fact]
    public void ResolveTraceEndpoints_PreservesSdkComputedEndpoint()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddOtlpExporter(options => ConfigureHttpEndpoint(options, "http://collector:4318/custom-traces/"))
            .Build();

        Assert.Equal(
            [EffectiveOtlpEndpoint.Http("http://collector:4318/custom-traces/")],
            OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints(provider));
    }

    [Fact]
    public void ResolveTraceEndpoints_ReturnsMultipleOtlpExporterEndpoints()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddOtlpExporter(options => ConfigureHttpEndpoint(options, "http://collector:4318/traces-a"))
            .AddOtlpExporter(options => ConfigureHttpEndpoint(options, "http://collector:4319/traces-b"))
            .Build();

        Assert.Equal(
            [
                EffectiveOtlpEndpoint.Http("http://collector:4318/traces-a"),
                EffectiveOtlpEndpoint.Http("http://collector:4319/traces-b")
            ],
            OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints(provider));
    }

    [Fact]
    public void ResolveTraceEndpoints_ReturnsEmpty_WhenNoOtlpExporterIsConfigured()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateTracerProviderBuilder().Build();

        Assert.Empty(OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints(provider));
    }

    [Fact]
    public void ResolveMetricEndpoints_ReturnsOtlpExporterEndpoint()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateMeterProviderBuilder()
            .AddMeter("test-meter")
            .AddOtlpExporter(options => ConfigureHttpEndpoint(options, "http://collector:4318/custom-metrics"))
            .Build();

        Assert.Equal(
            [EffectiveOtlpEndpoint.Http("http://collector:4318/custom-metrics")],
            OtlpEndpointProviderGraphResolver.ResolveMetricEndpoints(provider));
    }

    [Fact]
    public void ResolveMetricEndpoints_ReturnsMultipleOtlpExporterEndpoints()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateMeterProviderBuilder()
            .AddMeter("test-meter")
            .AddOtlpExporter(options => ConfigureHttpEndpoint(options, "http://collector:4318/metrics-a"))
            .AddOtlpExporter(options => ConfigureHttpEndpoint(options, "http://collector:4319/metrics-b"))
            .Build();

        Assert.Equal(
            [
                EffectiveOtlpEndpoint.Http("http://collector:4318/metrics-a"),
                EffectiveOtlpEndpoint.Http("http://collector:4319/metrics-b")
            ],
            OtlpEndpointProviderGraphResolver.ResolveMetricEndpoints(provider));
    }

    [Fact]
    public void ResolveMetricEndpoints_ReturnsEmpty_WhenNoOtlpExporterIsConfigured()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateMeterProviderBuilder().Build();

        Assert.Empty(OtlpEndpointProviderGraphResolver.ResolveMetricEndpoints(provider));
    }

    [Fact]
    public void ResolveLogEndpoints_ReturnsMultipleOtlpExporterEndpoints()
    {
        using var provider = CreateLoggerProviderBuilder()
            .AddOtlpExporter(options => ConfigureHttpEndpoint(options, "http://collector:4318/logs-a"))
            .AddOtlpExporter(options => ConfigureHttpEndpoint(options, "http://collector:4319/logs-b"))
            .Build();

        Assert.Equal(
            [
                EffectiveOtlpEndpoint.Http("http://collector:4318/logs-a"),
                EffectiveOtlpEndpoint.Http("http://collector:4319/logs-b")
            ],
            OtlpEndpointProviderGraphResolver.ResolveLogEndpoints(provider));
    }

#if NET
    [Fact]
    public void ResolveTraceEndpoints_ReturnsGrpcOtlpExporterEndpoint()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddOtlpExporter(options => ConfigureGrpcEndpoint(options, "http://collector:4317"))
            .Build();

        Assert.Equal(
            [EffectiveOtlpEndpoint.Grpc("http://collector:4317/opentelemetry.proto.collector.trace.v1.TraceService/Export")],
            OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints(provider));
    }

    [Fact]
    public void ResolveMetricEndpoints_ReturnsGrpcOtlpExporterEndpoint()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateMeterProviderBuilder()
            .AddMeter("test-meter")
            .AddOtlpExporter(options => ConfigureGrpcEndpoint(options, "http://collector:4317"))
            .Build();

        Assert.Equal(
            [EffectiveOtlpEndpoint.Grpc("http://collector:4317/opentelemetry.proto.collector.metrics.v1.MetricsService/Export")],
            OtlpEndpointProviderGraphResolver.ResolveMetricEndpoints(provider));
    }

    [Fact]
    public void ResolveLogEndpoints_ReturnsGrpcOtlpExporterEndpoint()
    {
        using var provider = CreateLoggerProviderBuilder()
            .AddOtlpExporter(options => ConfigureGrpcEndpoint(options, "http://collector:4317"))
            .Build();

        Assert.Equal(
            [EffectiveOtlpEndpoint.Grpc("http://collector:4317/opentelemetry.proto.collector.logs.v1.LogsService/Export")],
            OtlpEndpointProviderGraphResolver.ResolveLogEndpoints(provider));
    }
#endif

    private static void ConfigureHttpEndpoint(OtlpExporterOptions options, string endpoint)
    {
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
        options.Endpoint = new Uri(endpoint);
    }

#if NET
    private static void ConfigureGrpcEndpoint(OtlpExporterOptions options, string endpoint)
    {
#pragma warning disable CS0618 // OtlpExportProtocol.Grpc is obsolete but still supported by the SDK.
        options.Protocol = OtlpExportProtocol.Grpc;
#pragma warning restore CS0618
        options.Endpoint = new Uri(endpoint);
    }
#endif

    private static LoggerProviderBuilder CreateLoggerProviderBuilder()
    {
        return (LoggerProviderBuilder)typeof(global::OpenTelemetry.Sdk)
            .GetMethod("CreateLoggerProviderBuilder", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, null)!;
    }
}
