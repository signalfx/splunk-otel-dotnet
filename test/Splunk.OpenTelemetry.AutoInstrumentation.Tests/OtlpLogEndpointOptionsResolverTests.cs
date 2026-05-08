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

using OpenTelemetry.Exporter;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

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

        Assert.Equal("http://localhost:4318/v1/logs", OtlpLogEndpointOptionsResolver.ResolveEndpoint(options));
    }

    [Fact]
    public void ResolveEndpoint_KeepsExplicitHttpEndpoint()
    {
        var options = new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf,
            Endpoint = new Uri("http://collector:4318/v1/logs")
        };

        Assert.Equal("http://collector:4318/v1/logs", OtlpLogEndpointOptionsResolver.ResolveEndpoint(options));
    }

    [Fact]
    public void ResolveEndpoint_PreservesExplicitHttpEndpointTrailingSlash()
    {
        var options = new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf,
            Endpoint = new Uri("http://collector:4318/v1/logs/")
        };

        Assert.Equal("http://collector:4318/v1/logs/", OtlpLogEndpointOptionsResolver.ResolveEndpoint(options));
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

        Assert.Equal(
            "http://localhost:4317/opentelemetry.proto.collector.logs.v1.LogsService/Export",
            OtlpLogEndpointOptionsResolver.ResolveEndpoint(options));
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

        Assert.Equal(
            "http://collector:4317/opentelemetry.proto.collector.logs.v1.LogsService/Export",
            OtlpLogEndpointOptionsResolver.ResolveEndpoint(options));
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
