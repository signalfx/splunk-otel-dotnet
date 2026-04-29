// <copyright file="EffectiveConfigReaderTests.cs" company="Splunk Inc.">
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

public class EffectiveConfigReaderTests : IDisposable
{
    public EffectiveConfigReaderTests()
    {
        ClearEnvVars();
    }

    public void Dispose()
    {
        ClearEnvVars();
    }

    // ResolveServiceName

    [Fact]
    public void ResolveServiceName_ReturnsOtelServiceName_WhenSet()
    {
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "my-service");

        Assert.Equal("my-service", ServiceNameResolver.Resolve(null));
    }

    [Fact]
    public void ResolveServiceName_FallsBackToResourceAttributes_WhenServiceNameNotSet()
    {
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "service.name=fallback-service,env=prod");

        Assert.Equal("fallback-service", ServiceNameResolver.Resolve(null));
    }

    [Fact]
    public void ResolveServiceName_PrefersOtelServiceName_OverResourceAttributes()
    {
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "primary");
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "service.name=secondary");

        Assert.Equal("primary", ServiceNameResolver.Resolve(null));
    }

    [Fact]
    public void ResolveServiceName_IgnoresResourceAttributesWithDifferentServiceNameCasing()
    {
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "Service.Name=my-svc");

        Assert.Null(ServiceNameResolver.Resolve(null));
    }

    [Fact]
    public void ResolveServiceName_ReturnsNull_WhenNoEnvVarsSetAndNoInstrumentationType()
    {
        Assert.Null(ServiceNameResolver.Resolve(null));
    }

    [Fact]
    public void ResolveServiceName_IgnoresEmptyServiceNameInResourceAttributes()
    {
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "service.name=,env=prod");

        Assert.Null(ServiceNameResolver.Resolve(null));
    }

    [Fact]
    public void ResolveServiceName_IgnoresMalformedResourceAttributes()
    {
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "no-equals-sign,=valuewithnokey");

        Assert.Null(ServiceNameResolver.Resolve(null));
    }

    [Fact]
    public void ReadServiceNameFromResources_IgnoresWrongCasedServiceName()
    {
        var resources = new[]
        {
            new KeyValuePair<string, object>("Service.Name", "wrong-cased-service"),
        };

        Assert.Null(ServiceNameResolver.ReadFromResources(resources));
    }

    // Endpoint log entries

    [Fact]
    public void FormatLogEntry_AddsEffectiveConfigPrefix()
    {
        Assert.Equal(
            "Effective configuration: OTEL_EXPORTER_OTLP_TRACES_ENDPOINT=http://collector:4318/v1/traces",
            EffectiveConfigLog.FormatEntry(
                EffectiveConfigKeys.TracesEndpoint,
                "http://collector:4318/v1/traces"));
    }

    [Fact]
    public void EffectiveConfigValueAccumulator_AppendsValuesForSameKey()
    {
        var accumulator = new EffectiveConfigValueAccumulator();

        accumulator.Add(
            EffectiveConfigKeys.TracesEndpoint,
            "http://collector-1:4318/v1/traces");
        Assert.Equal(
            "http://collector-1:4318/v1/traces",
            accumulator.GetValue(EffectiveConfigKeys.TracesEndpoint));

        accumulator.Add(
            EffectiveConfigKeys.TracesEndpoint,
            "http://collector-2:4318/v1/traces");
        Assert.Equal(
            "http://collector-1:4318/v1/traces,http://collector-2:4318/v1/traces",
            accumulator.GetValue(EffectiveConfigKeys.TracesEndpoint));

        accumulator.Add(
            EffectiveConfigKeys.MetricsEndpoint,
            "http://metrics-collector:4318/v1/metrics");
        Assert.Equal(
            "http://metrics-collector:4318/v1/metrics",
            accumulator.GetValue(EffectiveConfigKeys.MetricsEndpoint));
    }

    [Fact]
    public void EffectiveConfigValueAccumulator_EscapesCommaAndPercent()
    {
        var accumulator = new EffectiveConfigValueAccumulator();

        accumulator.Add(EffectiveConfigKeys.TracesEndpoint, "http://collector/path,with%2Ccomma");

        Assert.Equal(
            "http://collector/path%2Cwith%252Ccomma",
            accumulator.GetValue(EffectiveConfigKeys.TracesEndpoint));
    }

    [Fact]
    public void ResolveOtlpEndpointFromOptions_AppendsHttpSignalPath_WhenSdkWillAppendSignalPath()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4318");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");
        var options = new OtlpExporterOptions();

        Assert.Equal(
            "http://collector:4318/v1/traces",
            OtlpEndpointResolver.ResolveFromOptions(options, EffectiveConfigKeys.TracesEndpoint));
    }

    [Fact]
    public void ResolveOtlpEndpointFromOptions_DoesNotAppendHttpSignalPath_WhenPathAlreadyExistsWithDifferentCasing()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4318/V1/TRACES");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");
        var options = new OtlpExporterOptions();

        Assert.Equal(
            "http://collector:4318/V1/TRACES",
            OtlpEndpointResolver.ResolveFromOptions(options, EffectiveConfigKeys.TracesEndpoint));
    }

    [Fact]
    public void ResolveOtlpEndpointFromOptions_NormalizesTrailingSlash_WhenPathAlreadyExists()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4318/v1/traces/");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");
        var options = new OtlpExporterOptions();

        Assert.Equal(
            "http://collector:4318/v1/traces",
            OtlpEndpointResolver.ResolveFromOptions(options, EffectiveConfigKeys.TracesEndpoint));
    }

    [Fact]
    public void ResolveOtlpEndpointFromOptions_DoesNotAppendHttpSignalPath_WhenEndpointSetterWasUsed()
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
    public void ResolveOtlpEndpointFromOptions_AppendsGrpcSignalPath()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4317");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc");
        var options = new OtlpExporterOptions();

        Assert.Equal(
            "http://collector:4317/opentelemetry.proto.collector.trace.v1.TraceService/Export",
            OtlpEndpointResolver.ResolveFromOptions(options, EffectiveConfigKeys.TracesEndpoint));
    }

    [Fact]
    public void ResolveOtlpEndpointFromOptions_UsesSplunkRealmOverrideWithoutAppendingHttpSignalPath()
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

    [Fact]
    public void Read_ReflectsSplunkSettingsValues()
    {
        var settings = new PluginSettings(new NameValueConfigurationSource(new NameValueCollection
        {
            ["SPLUNK_PROFILER_ENABLED"] = "true",
            ["SPLUNK_PROFILER_MEMORY_ENABLED"] = "true",
            ["SPLUNK_SNAPSHOT_PROFILER_ENABLED"] = "true",
            ["SPLUNK_SNAPSHOT_SAMPLING_INTERVAL"] = "100",
        }));

        var config = EffectiveConfigReader.Read(settings);

        Assert.Equal("True", config["SPLUNK_PROFILER_ENABLED"]);
#if NET
        Assert.Equal("True", config["SPLUNK_PROFILER_MEMORY_ENABLED"]);
#else
        Assert.Equal("False", config["SPLUNK_PROFILER_MEMORY_ENABLED"]);
#endif
        Assert.Equal("True", config["SPLUNK_SNAPSHOT_PROFILER_ENABLED"]);
        Assert.Equal("100", config["SPLUNK_SNAPSHOT_SAMPLING_INTERVAL"]);
    }

    private static void ClearEnvVars()
    {
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", null);
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", null);
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
        Environment.SetEnvironmentVariable("SPLUNK_PROFILER_ENABLED", null);
        Environment.SetEnvironmentVariable("SPLUNK_PROFILER_MEMORY_ENABLED", null);
        Environment.SetEnvironmentVariable("SPLUNK_SNAPSHOT_PROFILER_ENABLED", null);
        Environment.SetEnvironmentVariable("SPLUNK_SNAPSHOT_SAMPLING_INTERVAL", null);
        Environment.SetEnvironmentVariable("SPLUNK_PROFILER_CALL_STACK_INTERVAL", null);
    }
}
