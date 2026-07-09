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

using System.Diagnostics;
using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
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
            .AddOtlpExporter(options => ConfigureHttpEndpoint(
                options,
                "http://collector:4319/traces-b",
                ExportProcessorType.Simple))
            .Build();

        Assert.Equal(
            [
                EffectiveOtlpEndpoint.Http("http://collector:4318/traces-a"),
                EffectiveOtlpEndpoint.Http("http://collector:4319/traces-b", EffectiveOtlpPipelineType.Simple)
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
    public void ResolveTraceEndpoints_IgnoresUnknownProcessorWithoutExporter()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddProcessor(new NoopActivityProcessor())
            .Build();

        Assert.Empty(OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints(provider));
    }

    [Fact]
    public void ResolveTraceEndpoints_IgnoresUnknownProcessorWithNonOtlpExporter()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddProcessor(new UnknownActivityProcessorWithExporter(new UnknownActivityExporter()))
            .Build();

        Assert.Empty(OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints(provider));
    }

    [Fact]
    public void ResolveTraceEndpoints_IgnoresUnknownProcessorWithUnrelatedHeadField()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddProcessor(new NoopActivityProcessorWithHead())
            .Build();

        Assert.Empty(OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints(provider));
    }

    [Fact]
    public void ResolveTraceEndpoints_Throws_WhenUnknownProcessorUsesOtlpExporter()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddProcessor(new UnknownActivityProcessorWithExporter(
                new OtlpTraceExporter(CreateHttpOptions("http://collector:4318/custom-traces"))))
            .Build();

        Assert.Throws<InvalidOperationException>(
            () => OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints(provider));
    }

    [Fact]
    public void ResolveTraceEndpoints_ReturnsEndpoint_WhenOtlpProcessorTypeIsDerived()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddProcessor(new DerivedSimpleActivityExportProcessor(
                new OtlpTraceExporter(CreateHttpOptions("http://collector:4318/custom-traces"))))
            .Build();

        Assert.Equal(
            [EffectiveOtlpEndpoint.Http("http://collector:4318/custom-traces", EffectiveOtlpPipelineType.Simple)],
            OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints(provider));
    }

    [Fact]
    public void ResolveTraceEndpoints_Throws_WhenOtlpExporterTypeIsDerived()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddProcessor(new SimpleActivityExportProcessor(
                new DerivedOtlpTraceExporter(CreateHttpOptions("http://collector:4318/custom-traces"))))
            .Build();

        Assert.Throws<InvalidOperationException>(
            () => OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints(provider));
    }

    [Fact]
    public void ResolveTraceEndpoints_Throws_WhenCompositeHeadIsNull()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddProcessor(new NoopActivityProcessor())
            .AddProcessor(new NoopActivityProcessor())
            .Build();
        var processor = provider.GetType()
            .GetProperty("Processor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetValue(provider)!;
        processor.GetType()
            .GetField("Head", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(processor, null);

        Assert.Throws<InvalidOperationException>(
            () => OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints(provider));
    }

    [Fact]
    public void ResolveMetricEndpoints_ReturnsOtlpExporterEndpoint()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateMeterProviderBuilder()
            .AddMeter("test-meter")
            .AddOtlpExporter(options => ConfigureHttpEndpoint(options, "http://collector:4318/custom-metrics"))
            .Build();

        Assert.Equal(
            [EffectiveOtlpEndpoint.Http("http://collector:4318/custom-metrics", EffectiveOtlpPipelineType.Periodic)],
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
                EffectiveOtlpEndpoint.Http("http://collector:4318/metrics-a", EffectiveOtlpPipelineType.Periodic),
                EffectiveOtlpEndpoint.Http("http://collector:4319/metrics-b", EffectiveOtlpPipelineType.Periodic)
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
    public void ResolveMetricEndpoints_Throws_WhenOtlpExporterTypeIsDerived()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateMeterProviderBuilder()
            .AddMeter("test-meter")
            .AddReader(new PeriodicExportingMetricReader(
                new DerivedOtlpMetricExporter(CreateHttpOptions("http://collector:4318/custom-metrics"))))
            .Build();

        Assert.Throws<InvalidOperationException>(
            () => OtlpEndpointProviderGraphResolver.ResolveMetricEndpoints(provider));
    }

    [Fact]
    public void ResolveMetricEndpoints_ReturnsEndpoint_WhenOtlpReaderTypeIsDerived()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateMeterProviderBuilder()
            .AddMeter("test-meter")
            .AddReader(new DerivedPeriodicExportingMetricReader(
                new OtlpMetricExporter(CreateHttpOptions("http://collector:4318/custom-metrics"))))
            .Build();

        Assert.Equal(
            [EffectiveOtlpEndpoint.Http("http://collector:4318/custom-metrics", EffectiveOtlpPipelineType.Periodic)],
            OtlpEndpointProviderGraphResolver.ResolveMetricEndpoints(provider));
    }

    [Fact]
    public void ResolveMetricEndpoints_Throws_WhenOtlpExporterUsesUnknownReaderType()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateMeterProviderBuilder()
            .AddMeter("test-meter")
            .AddReader(new UnknownExportingMetricReader(
                new OtlpMetricExporter(CreateHttpOptions("http://collector:4318/custom-metrics"))))
            .Build();

        Assert.Throws<InvalidOperationException>(
            () => OtlpEndpointProviderGraphResolver.ResolveMetricEndpoints(provider));
    }

    [Fact]
    public void ResolveMetricEndpoints_IgnoresUnknownReaderTypeWithNonOtlpExporter()
    {
        using var provider = global::OpenTelemetry.Sdk.CreateMeterProviderBuilder()
            .AddMeter("test-meter")
            .AddReader(new UnknownExportingMetricReader(new UnknownMetricExporter()))
            .Build();

        Assert.Empty(OtlpEndpointProviderGraphResolver.ResolveMetricEndpoints(provider));
    }

    [Fact]
    public void ResolveLogEndpoints_ReturnsMultipleOtlpExporterEndpoints()
    {
        using var provider = CreateLoggerProviderBuilder()
            .AddOtlpExporter(options => ConfigureHttpEndpoint(options, "http://collector:4318/logs-a"))
            .AddProcessor(new SimpleLogRecordExportProcessor(
                new OtlpLogExporter(CreateHttpOptions(
                    "http://collector:4319/logs-b",
                    ExportProcessorType.Simple))))
            .Build();

        Assert.Equal(
            [
                EffectiveOtlpEndpoint.Http("http://collector:4318/logs-a"),
                EffectiveOtlpEndpoint.Http("http://collector:4319/logs-b", EffectiveOtlpPipelineType.Simple)
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
            [EffectiveOtlpEndpoint.Grpc(
                "http://collector:4317/opentelemetry.proto.collector.metrics.v1.MetricsService/Export",
                EffectiveOtlpPipelineType.Periodic)],
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

    private static void ConfigureHttpEndpoint(
        OtlpExporterOptions options,
        string endpoint,
        ExportProcessorType processorType = ExportProcessorType.Batch)
    {
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
        options.Endpoint = new Uri(endpoint);
        options.ExportProcessorType = processorType;
    }

    private static OtlpExporterOptions CreateHttpOptions(
        string endpoint,
        ExportProcessorType processorType = ExportProcessorType.Batch)
    {
        var options = new OtlpExporterOptions();
        ConfigureHttpEndpoint(options, endpoint, processorType);
        return options;
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

    private sealed class UnknownExportingMetricReader : BaseExportingMetricReader
    {
        public UnknownExportingMetricReader(BaseExporter<Metric> exporter)
            : base(exporter)
        {
        }
    }

    private sealed class UnknownMetricExporter : BaseExporter<Metric>
    {
        public override ExportResult Export(in Batch<Metric> batch)
        {
            return ExportResult.Success;
        }
    }

    private sealed class DerivedOtlpTraceExporter : OtlpTraceExporter
    {
        public DerivedOtlpTraceExporter(OtlpExporterOptions options)
            : base(options)
        {
        }
    }

    private sealed class DerivedOtlpMetricExporter : OtlpMetricExporter
    {
        public DerivedOtlpMetricExporter(OtlpExporterOptions options)
            : base(options)
        {
        }
    }

    private sealed class DerivedSimpleActivityExportProcessor : SimpleActivityExportProcessor
    {
        public DerivedSimpleActivityExportProcessor(BaseExporter<Activity> exporter)
            : base(exporter)
        {
        }
    }

    private sealed class DerivedPeriodicExportingMetricReader : PeriodicExportingMetricReader
    {
        public DerivedPeriodicExportingMetricReader(BaseExporter<Metric> exporter)
            : base(exporter)
        {
        }
    }

    private sealed class UnknownActivityProcessorWithExporter : BaseProcessor<Activity>
    {
        private readonly BaseExporter<Activity> exporter;

        public UnknownActivityProcessorWithExporter(BaseExporter<Activity> exporter)
        {
            this.exporter = exporter;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                exporter.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class UnknownActivityExporter : BaseExporter<Activity>
    {
        public override ExportResult Export(in Batch<Activity> batch)
        {
            return ExportResult.Success;
        }
    }

    private sealed class NoopActivityProcessor : BaseProcessor<Activity>
    {
    }

    private sealed class NoopActivityProcessorWithHead : BaseProcessor<Activity>
    {
#pragma warning disable CA1051, SA1401 // The field name intentionally exercises composite detection.
        public readonly object? Head = null;
#pragma warning restore CA1051, SA1401
    }
}
