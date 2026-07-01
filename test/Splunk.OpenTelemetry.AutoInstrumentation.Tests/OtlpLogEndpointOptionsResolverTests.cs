// <copyright file="OtlpLogEndpointOptionsResolverTests.cs" company="Splunk Inc.">
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

using OpenTelemetry;
using OpenTelemetry.Exporter;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Resolvers;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class OtlpLogEndpointOptionsResolverTests
{
    [Fact]
    public void ResolveEndpoint_AppendsHttpLogsPathForDefaultEndpoint()
    {
        using var defaultEndpointScope = new EnvironmentVariableScope("OTEL_EXPORTER_OTLP_ENDPOINT", null);
        using var logsEndpointScope = new EnvironmentVariableScope("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", null);

        var options = new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf
        };

        AssertEndpoint(
            OtlpLogEndpointOptionsResolver.ResolveEndpoint(options),
            "http://localhost:4318/v1/logs",
            EffectiveOtlpExporterType.HttpProtobuf);
    }

    [Fact]
    public void ResolveEndpoint_KeepsExplicitHttpEndpoint()
    {
        var options = new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf,
            Endpoint = new Uri("http://collector:4318/v1/logs")
        };

        AssertEndpoint(
            OtlpLogEndpointOptionsResolver.ResolveEndpoint(options),
            "http://collector:4318/v1/logs",
            EffectiveOtlpExporterType.HttpProtobuf);
    }

    [Fact]
    public void ResolveEndpoint_PreservesExplicitHttpEndpointTrailingSlash()
    {
        var options = new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf,
            Endpoint = new Uri("http://collector:4318/v1/logs/")
        };

        AssertEndpoint(
            OtlpLogEndpointOptionsResolver.ResolveEndpoint(options),
            "http://collector:4318/v1/logs/",
            EffectiveOtlpExporterType.HttpProtobuf);
    }

    [Fact]
    public void ResolveEndpoint_ReportsBatchWhenSdkIgnoresSimpleProcessorType()
    {
        var options = new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf,
            Endpoint = new Uri("http://collector:4318/v1/logs"),
            ExportProcessorType = ExportProcessorType.Simple
        };

        AssertEndpoint(
            OtlpLogEndpointOptionsResolver.ResolveEndpoint(options),
            "http://collector:4318/v1/logs",
            EffectiveOtlpExporterType.HttpProtobuf);
    }

    [Fact]
    public void ResolveEndpoint_AppendsGrpcLogsPathForDefaultEndpoint()
    {
        using var defaultEndpointScope = new EnvironmentVariableScope("OTEL_EXPORTER_OTLP_ENDPOINT", null);
        using var logsEndpointScope = new EnvironmentVariableScope("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", null);

        var options = new OtlpExporterOptions
        {
#pragma warning disable CS0618 // OtlpExportProtocol.Grpc is obsolete but still supported by the SDK.
            Protocol = OtlpExportProtocol.Grpc
#pragma warning restore CS0618
        };

        AssertEndpoint(
            OtlpLogEndpointOptionsResolver.ResolveEndpoint(options),
            "http://localhost:4317/opentelemetry.proto.collector.logs.v1.LogsService/Export",
            EffectiveOtlpExporterType.Grpc);
    }

    [Fact]
    public void ResolveEndpoint_DoesNotDuplicateGrpcLogsPath()
    {
        var options = new OtlpExporterOptions
        {
#pragma warning disable CS0618 // OtlpExportProtocol.Grpc is obsolete but still supported by the SDK.
            Protocol = OtlpExportProtocol.Grpc,
#pragma warning restore CS0618
            Endpoint = new Uri("http://collector:4317/opentelemetry.proto.collector.logs.v1.LogsService/Export")
        };

        AssertEndpoint(
            OtlpLogEndpointOptionsResolver.ResolveEndpoint(options),
            "http://collector:4317/opentelemetry.proto.collector.logs.v1.LogsService/Export",
            EffectiveOtlpExporterType.Grpc);
    }

    private static void AssertEndpoint(
        EffectiveOtlpEndpoint? endpoint,
        string expectedEndpoint,
        EffectiveOtlpExporterType expectedExporterType,
        EffectiveOtlpPipelineType expectedPipelineType = EffectiveOtlpPipelineType.Batch)
    {
        Assert.NotNull(endpoint);
        Assert.Equal(expectedEndpoint, endpoint.Value.Endpoint);
        Assert.Equal(expectedExporterType, endpoint.Value.ExporterType);
        Assert.Equal(expectedPipelineType, endpoint.Value.PipelineType);
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string _name;
        private readonly string? _previousValue;

        public EnvironmentVariableScope(string name, string? value)
        {
            _name = name;
            _previousValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_name, _previousValue);
        }
    }
}
