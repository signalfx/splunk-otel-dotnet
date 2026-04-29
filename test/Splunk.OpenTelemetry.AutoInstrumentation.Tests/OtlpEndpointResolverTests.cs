// <copyright file="OtlpEndpointResolverTests.cs" company="Splunk Inc.">
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

using System.Collections.Specialized;
using OpenTelemetry.Exporter;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class OtlpEndpointResolverTests : IDisposable
{
    public OtlpEndpointResolverTests()
    {
        ClearEnvVars();
    }

    public void Dispose()
    {
        ClearEnvVars();
    }

    [Fact]
    public void ResolveFromOptions_AppendsHttpSignalPath_WhenSdkWillAppendSignalPath()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4318");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");
        var options = new OtlpExporterOptions();

        Assert.Equal(
            "http://collector:4318/v1/traces",
            OtlpEndpointResolver.ResolveFromOptions(options, EffectiveConfigKeys.TracesEndpoint));
    }

    [Fact]
    public void ResolveFromOptions_DoesNotAppendHttpSignalPath_WhenPathAlreadyExistsWithDifferentCasing()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4318/V1/TRACES");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");
        var options = new OtlpExporterOptions();

        Assert.Equal(
            "http://collector:4318/V1/TRACES",
            OtlpEndpointResolver.ResolveFromOptions(options, EffectiveConfigKeys.TracesEndpoint));
    }

    [Fact]
    public void ResolveFromOptions_NormalizesTrailingSlash_WhenPathAlreadyExists()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4318/v1/traces/");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");
        var options = new OtlpExporterOptions();

        Assert.Equal(
            "http://collector:4318/v1/traces",
            OtlpEndpointResolver.ResolveFromOptions(options, EffectiveConfigKeys.TracesEndpoint));
    }

    [Fact]
    public void ResolveFromOptions_DoesNotAppendHttpSignalPath_WhenEndpointSetterWasUsed()
    {
        var options = new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf,
            Endpoint = new Uri("http://collector:4318/custom-traces"),
        };

        Assert.Equal(
            "http://collector:4318/custom-traces",
            OtlpEndpointResolver.ResolveFromOptions(options, EffectiveConfigKeys.TracesEndpoint));
    }

    [Fact]
    public void ResolveFromOptions_AppendsGrpcSignalPath()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4317");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc");
        var options = new OtlpExporterOptions();

        Assert.Equal(
            "http://collector:4317/opentelemetry.proto.collector.trace.v1.TraceService/Export",
            OtlpEndpointResolver.ResolveFromOptions(options, EffectiveConfigKeys.TracesEndpoint));
    }

    [Fact]
    public void ResolveFromOptions_UsesSplunkRealmOverrideWithoutAppendingHttpSignalPath()
    {
        var settings = new PluginSettings(new NameValueConfigurationSource(new NameValueCollection
        {
            ["SPLUNK_REALM"] = "us0",
            ["SPLUNK_ACCESS_TOKEN"] = "token",
        }));
        var options = new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf,
        };

        new Traces(settings).ConfigureTracesOptions(options);

        Assert.Equal(
            "https://ingest.us0.observability.splunkcloud.com/v2/trace/otlp",
            OtlpEndpointResolver.ResolveFromOptions(options, EffectiveConfigKeys.TracesEndpoint));
    }

    private static void ClearEnvVars()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", null);
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", null);
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", null);
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", null);
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_PROTOCOL", null);
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_PROTOCOL", null);
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_PROTOCOL", null);
        Environment.SetEnvironmentVariable("SPLUNK_REALM", null);
        Environment.SetEnvironmentVariable("SPLUNK_ACCESS_TOKEN", null);
    }
}
