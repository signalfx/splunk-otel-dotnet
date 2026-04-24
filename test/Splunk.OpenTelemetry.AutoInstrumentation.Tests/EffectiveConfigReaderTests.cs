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
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging.EffectiveConfig;

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

    // ── ResolveServiceName ─────────────────────────────────────────────────────

    [Fact]
    public void ResolveServiceName_ReturnsOtelServiceName_WhenSet()
    {
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "my-service");

        Assert.Equal("my-service", EffectiveConfigReader.ResolveServiceName(null));
    }

    [Fact]
    public void ResolveServiceName_FallsBackToResourceAttributes_WhenServiceNameNotSet()
    {
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "service.name=fallback-service,env=prod");

        Assert.Equal("fallback-service", EffectiveConfigReader.ResolveServiceName(null));
    }

    [Fact]
    public void ResolveServiceName_PrefersOtelServiceName_OverResourceAttributes()
    {
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "primary");
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "service.name=secondary");

        Assert.Equal("primary", EffectiveConfigReader.ResolveServiceName(null));
    }

    [Fact]
    public void ResolveServiceName_IsCaseInsensitive_ForServiceNameKey()
    {
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "Service.Name=my-svc");

        Assert.Equal("my-svc", EffectiveConfigReader.ResolveServiceName(null));
    }

    [Fact]
    public void ResolveServiceName_ReturnsNull_WhenNoEnvVarsSetAndNoInstrumentationType()
    {
        Assert.Null(EffectiveConfigReader.ResolveServiceName(null));
    }

    [Fact]
    public void ResolveServiceName_IgnoresEmptyServiceNameInResourceAttributes()
    {
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "service.name=,env=prod");

        Assert.Null(EffectiveConfigReader.ResolveServiceName(null));
    }

    [Fact]
    public void ResolveServiceName_IgnoresMalformedResourceAttributes()
    {
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "no-equals-sign,=valuewithnokey");

        Assert.Null(EffectiveConfigReader.ResolveServiceName(null));
    }

    // ── ResolveOtlpEndpointFallback ────────────────────────────────────────────

    [Fact]
    public void ResolveOtlpEndpointFallback_ReturnsSignalEnvVar_WhenSet()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "http://traces-collector:4318/v1/traces");

        Assert.Equal("http://traces-collector:4318/v1/traces", EffectiveConfigReader.ResolveOtlpEndpointFallback("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "OTEL_EXPORTER_OTLP_TRACES_PROTOCOL", "/v1/traces"));
    }

    [Fact]
    public void ResolveOtlpEndpointFallback_ReturnsHttpDefault_WhenNoEnvVarsSet()
    {
        Assert.Equal("http://localhost:4318/v1/traces", EffectiveConfigReader.ResolveOtlpEndpointFallback("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "OTEL_EXPORTER_OTLP_TRACES_PROTOCOL", "/v1/traces"));
        Assert.Equal("http://localhost:4318/v1/metrics", EffectiveConfigReader.ResolveOtlpEndpointFallback("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", "OTEL_EXPORTER_OTLP_METRICS_PROTOCOL", "/v1/metrics"));
        Assert.Equal("http://localhost:4318/v1/logs", EffectiveConfigReader.ResolveOtlpEndpointFallback("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", "OTEL_EXPORTER_OTLP_LOGS_PROTOCOL", "/v1/logs"));
    }

    [Fact]
    public void ResolveOtlpEndpointFallback_AppendsSignalSuffix_WhenBaseEnvVarSet()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4318");

        Assert.Equal("http://collector:4318/v1/traces", EffectiveConfigReader.ResolveOtlpEndpointFallback("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "OTEL_EXPORTER_OTLP_TRACES_PROTOCOL", "/v1/traces"));
    }

    [Fact]
    public void ResolveOtlpEndpointFallback_TrimsTrailingSlash_WhenBaseEnvVarHasTrailingSlash()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4318/");

        Assert.Equal("http://collector:4318/v1/traces", EffectiveConfigReader.ResolveOtlpEndpointFallback("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "OTEL_EXPORTER_OTLP_TRACES_PROTOCOL", "/v1/traces"));
    }

    [Fact]
    public void ResolveOtlpEndpointFallback_ReturnsHttpDefault_WhenBaseEnvVarIsInvalidUri()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "not-a-url");

        Assert.Equal("http://localhost:4318/v1/traces", EffectiveConfigReader.ResolveOtlpEndpointFallback("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "OTEL_EXPORTER_OTLP_TRACES_PROTOCOL", "/v1/traces"));
    }

    [Fact]
    public void ResolveOtlpEndpointFallback_ReturnsGrpcDefault_WhenProtocolIsGrpc()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc");

        Assert.Equal("http://localhost:4317", EffectiveConfigReader.ResolveOtlpEndpointFallback("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "OTEL_EXPORTER_OTLP_TRACES_PROTOCOL", "/v1/traces"));
    }

    [Fact]
    public void ResolveOtlpEndpointFallback_ReturnsGrpcBaseEndpoint_WhenProtocolIsGrpcAndBaseSet()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4317");

        Assert.Equal("http://collector:4317", EffectiveConfigReader.ResolveOtlpEndpointFallback("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "OTEL_EXPORTER_OTLP_TRACES_PROTOCOL", "/v1/traces"));
    }

    [Fact]
    public void ResolveOtlpEndpointFallback_SignalProtocolOverridesBaseProtocol()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_PROTOCOL", "http/protobuf");

        Assert.Equal("http://localhost:4318/v1/traces", EffectiveConfigReader.ResolveOtlpEndpointFallback("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "OTEL_EXPORTER_OTLP_TRACES_PROTOCOL", "/v1/traces"));
    }

    // ── Read ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Read_ReflectsSplunkSettingsValues()
    {
        var settings = new PluginSettings(new NameValueConfigurationSource(new NameValueCollection
        {
            ["SPLUNK_PROFILER_ENABLED"] = "true",
            ["SPLUNK_SNAPSHOT_PROFILER_ENABLED"] = "true",
            ["SPLUNK_SNAPSHOT_SAMPLING_INTERVAL"] = "100",
        }));

        var config = EffectiveConfigReader.Read(settings);

        Assert.Equal("True", config["SPLUNK_PROFILER_ENABLED"]);
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
        Environment.SetEnvironmentVariable("SPLUNK_PROFILER_ENABLED", null);
        Environment.SetEnvironmentVariable("SPLUNK_PROFILER_MEMORY_ENABLED", null);
        Environment.SetEnvironmentVariable("SPLUNK_SNAPSHOT_PROFILER_ENABLED", null);
        Environment.SetEnvironmentVariable("SPLUNK_SNAPSHOT_SAMPLING_INTERVAL", null);
        Environment.SetEnvironmentVariable("SPLUNK_PROFILER_CALL_STACK_INTERVAL", null);
    }
}
