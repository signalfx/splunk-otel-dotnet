// <copyright file="SmokeTests.cs" company="Splunk Inc.">
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

// <copyright file="SmokeTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
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

using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests;

public class SmokeTests : TestHelper, IDisposable
{
    private const string ServiceName = "TestApplication.Smoke";
    private readonly TestHttpServer _testServer;

    public SmokeTests(ITestOutputHelper output)
        : base("Smoke", output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS", "HttpClient");
        SetEnvironmentVariable("OTEL_EXPERIMENTAL_FILE_BASED_CONFIGURATION_ENABLED", "false");

        _testServer = TestHttpServer.CreateDefaultTestServer(output);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTraces()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("MyCompany.MyProduct.MyLibrary");
#if NETFRAMEWORK
        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpWebRequest");
#else
        collector.Expect("System.Net.Http");
#endif

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication(TestSettingsWithDefaultArgs());

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SdkOptionLimits()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect(
            "MyCompany.MyProduct.MyLibrary",
            span => span.Attributes.FirstOrDefault(att => att.Key == "long")?.Value.StringValue == new string('*', 12000));

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication(TestSettingsWithDefaultArgs());

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitMetrics()
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);
        collector.Expect("MyCompany.MyProduct.MyLibrary", metric => metric.Name == "MyFruitCounter");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication(TestSettingsWithDefaultArgs());

        collector.AssertExpectations();
    }

#if NET
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitLogs()
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);
        collector.Expect(logRecord => Convert.ToString(logRecord.Body) == "{ \"stringValue\": \"SmokeTest app log\" }");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_BLRP_MAX_EXPORT_BATCH_SIZE", "1");
        RunTestApplication(TestSettingsWithDefaultArgs());

        collector.AssertExpectations();
    }

#endif

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void TracesResource()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.ResourceExpector.ExpectDistributionResources(ServiceName);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication(TestSettingsWithDefaultArgs());

        collector.ResourceExpector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void MetricsResource()
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);

        collector.ResourceExpector.ExpectDistributionResources(ServiceName);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication(TestSettingsWithDefaultArgs());

        collector.ResourceExpector.AssertExpectations();
    }

#if NET // The feature is not supported on .NET Framework
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void LogsResource()
    {
        using var collector = new MockLogsCollector(Output);
        EnableFileBasedConfig("log-exporter-config.yaml");
        SetFileBasedExporter(collector);

        collector.ResourceExpector.ExpectDistributionResources(ServiceName);

        EnableBytecodeInstrumentation();

        RunTestApplication(TestSettingsWithDefaultArgs());

        collector.ResourceExpector.AssertExpectations();
    }

#endif

    [Fact]
    public void ManagedLogsHaveNoSensitiveData()
    {
        var tempLogsDirectory = DirectoryHelpers.CreateTempDirectory();
        var secretIdentificators = new[] { "api", "Token", "SeCrEt", "KEY", "PASSWORD", "PASS", "PWD", "HEADER", "CREDENTIALS" };

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY", tempLogsDirectory.FullName);
        SetEnvironmentVariable("OTEL_LOG_LEVEL", "debug");

        foreach (var item in secretIdentificators)
        {
            SetEnvironmentVariable($"OTEL_{item}_VALUE", "this is secret!");
        }

        try
        {
            RunTestApplication(TestSettingsWithDefaultArgs());

            var managedLog = tempLogsDirectory.GetFiles("otel-dotnet-auto-*-Splunk-*.log").Single();
            var managedLogContent = File.ReadAllText(managedLog.FullName);
            Assert.False(string.IsNullOrWhiteSpace(managedLogContent));

            var environmentVariables = ParseSettingsLog(managedLogContent, "Environment Variables:");
            VerifyVariables(environmentVariables);

#if NETFRAMEWORK
            var appSettings = ParseSettingsLog(managedLogContent, "AppSettings:");
            VerifyVariables(appSettings);
#endif
        }
        finally
        {
            tempLogsDirectory.Delete(true);
        }

        void VerifyVariables(ICollection<KeyValuePair<string, string>> options)
        {
            Assert.NotEmpty(options);

            var secretVariables = options
                .Where(item => secretIdentificators.Any(i => item.Key.Contains(i)))
                .ToList();

            Assert.NotEmpty(secretVariables);
            Assert.All(secretVariables, secret => Assert.Equal("<hidden>", secret.Value));
        }
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void EffectiveEnvVarConfigIsLogged()
    {
        var tempLogsDirectory = DirectoryHelpers.CreateTempDirectory();

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY", tempLogsDirectory.FullName);
        SetEnvironmentVariable("OTEL_LOG_LEVEL", "debug");
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "http://traces-collector:4318/v1/traces");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", "http://metrics-collector:4318/v1/metrics");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", "http://logs-collector:4318/v1/logs");
        SetEnvironmentVariable("SPLUNK_PROFILER_ENABLED", "true");
        SetEnvironmentVariable("SPLUNK_PROFILER_MEMORY_ENABLED", "true");
        SetEnvironmentVariable("SPLUNK_PROFILER_CALL_STACK_INTERVAL", "10000");
        SetEnvironmentVariable("SPLUNK_SNAPSHOT_PROFILER_ENABLED", "true");
        SetEnvironmentVariable("SPLUNK_SNAPSHOT_SAMPLING_INTERVAL", "5000");

        EnableBytecodeInstrumentation();
        EnableDefaultExporters();

        try
        {
            RunTestApplication();

            var logContent = File.ReadAllText(tempLogsDirectory.GetFiles("otel-dotnet-auto-*-Splunk-*.log").Single().FullName);
            var effectiveConfig = ExtractEffectiveConfigEntries(logContent);
            Assert.False(string.IsNullOrWhiteSpace(effectiveConfig));

            Assert.Contains("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT=http://traces-collector:4318/v1/traces", effectiveConfig);
            Assert.Contains("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT=http://metrics-collector:4318/v1/metrics", effectiveConfig);
            Assert.DoesNotContain("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=", effectiveConfig);
            Assert.Contains($"OTEL_SERVICE_NAME={ServiceName}", effectiveConfig);
            Assert.Contains("SPLUNK_PROFILER_ENABLED=True", effectiveConfig);
#if NET
            Assert.Contains("SPLUNK_PROFILER_MEMORY_ENABLED=True", effectiveConfig);
#else
            Assert.Contains("SPLUNK_PROFILER_MEMORY_ENABLED=False", effectiveConfig);
#endif
            Assert.Contains("SPLUNK_PROFILER_CALL_STACK_INTERVAL=10000", effectiveConfig);
            Assert.Contains("SPLUNK_SNAPSHOT_PROFILER_ENABLED=True", effectiveConfig);
            Assert.Contains("SPLUNK_SNAPSHOT_SAMPLING_INTERVAL=5000", effectiveConfig);
        }
        finally
        {
            tempLogsDirectory.Delete(true);
        }
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void EffectiveEnvVarConfigUsesSplunkRealmEndpoints()
    {
        var tempLogsDirectory = DirectoryHelpers.CreateTempDirectory();

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY", tempLogsDirectory.FullName);
        SetEnvironmentVariable("OTEL_LOG_LEVEL", "debug");
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");
        SetEnvironmentVariable("SPLUNK_REALM", "us0");
        SetEnvironmentVariable("SPLUNK_ACCESS_TOKEN", "token");

        EnableBytecodeInstrumentation();
        EnableDefaultExporters();

        try
        {
            RunTestApplication();

            var logContent = File.ReadAllText(tempLogsDirectory.GetFiles("otel-dotnet-auto-*-Splunk-*.log").Single().FullName);
            var effectiveConfig = ExtractEffectiveConfigEntries(logContent);
            Assert.False(string.IsNullOrWhiteSpace(effectiveConfig));

            Assert.Contains("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT=https://ingest.us0.observability.splunkcloud.com/v2/trace/otlp", effectiveConfig);
            Assert.Contains("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT=https://ingest.us0.observability.splunkcloud.com/v2/datapoint/otlp", effectiveConfig);
            Assert.DoesNotContain("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=", effectiveConfig);
        }
        finally
        {
            tempLogsDirectory.Delete(true);
        }
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void EffectiveEnvVarConfigUsesResolvedOtlpProtocol()
    {
        var tempLogsDirectory = DirectoryHelpers.CreateTempDirectory();

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY", tempLogsDirectory.FullName);
        SetEnvironmentVariable("OTEL_LOG_LEVEL", "debug");
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4317");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc");

        EnableBytecodeInstrumentation();
        EnableDefaultExporters();

        try
        {
            RunTestApplication();

            var logContent = File.ReadAllText(tempLogsDirectory.GetFiles("otel-dotnet-auto-*-Splunk-*.log").Single().FullName);
            var effectiveConfig = ExtractEffectiveConfigEntries(logContent);
            Assert.False(string.IsNullOrWhiteSpace(effectiveConfig));

#if NETFRAMEWORK
            Assert.Contains("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT=http://collector:4317/v1/traces", effectiveConfig);
            Assert.Contains("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT=http://collector:4317/v1/metrics", effectiveConfig);
            Assert.DoesNotContain("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=", effectiveConfig);
#else
            Assert.Contains("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT=http://collector:4317/opentelemetry.proto.collector.trace.v1.TraceService/Export", effectiveConfig);
            Assert.Contains("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT=http://collector:4317/opentelemetry.proto.collector.metrics.v1.MetricsService/Export", effectiveConfig);
            Assert.DoesNotContain("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=", effectiveConfig);
#endif
        }
        finally
        {
            tempLogsDirectory.Delete(true);
        }
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void EffectiveEnvVarConfigOmitsOtlpEndpointsWhenOtlpExportersAreDisabled()
    {
        var tempLogsDirectory = DirectoryHelpers.CreateTempDirectory();

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY", tempLogsDirectory.FullName);
        SetEnvironmentVariable("OTEL_LOG_LEVEL", "debug");
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "http://traces-collector:4318/v1/traces");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", "http://metrics-collector:4318/v1/metrics");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", "http://logs-collector:4318/v1/logs");

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_TRACES_EXPORTER", "none");
        SetEnvironmentVariable("OTEL_METRICS_EXPORTER", "none");
        SetEnvironmentVariable("OTEL_LOGS_EXPORTER", "none");

        try
        {
            RunTestApplication();

            var logContent = File.ReadAllText(tempLogsDirectory.GetFiles("otel-dotnet-auto-*-Splunk-*.log").Single().FullName);
            var effectiveConfig = ExtractEffectiveConfigEntries(logContent);
            Assert.False(string.IsNullOrWhiteSpace(effectiveConfig));

            Assert.DoesNotContain("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT=", effectiveConfig);
            Assert.DoesNotContain("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT=", effectiveConfig);
            Assert.DoesNotContain("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=", effectiveConfig);
            Assert.Contains($"OTEL_SERVICE_NAME={ServiceName}", effectiveConfig);
        }
        finally
        {
            tempLogsDirectory.Delete(true);
        }
    }

#if NET // File-based configuration is not supported on .NET Framework
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void EffectiveYamlConfigIsLogged()
    {
        var tempLogsDirectory = DirectoryHelpers.CreateTempDirectory();

        EnableBytecodeInstrumentation();
        EnableFileBasedConfig("config.yaml");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY", tempLogsDirectory.FullName);
        SetEnvironmentVariable("OTEL_LOG_LEVEL", "debug");
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");

        // Set traces and service name via env var — yaml substitutes them in.
        // Metrics endpoint is intentionally not set — yaml fallback value is used instead.
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "http://traces-collector:4318/v1/traces");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", "http://logs-collector:4318/v1/logs");
        SetEnvironmentVariable("OTEL_SERVICE_NAME", "env-var-service");

        try
        {
            RunTestApplication();

            var logContent = File.ReadAllText(tempLogsDirectory.GetFiles("otel-dotnet-auto-*-Splunk-*.log").Single().FullName);
            var effectiveConfig = ExtractEffectiveConfigEntries(logContent);
            Assert.False(string.IsNullOrWhiteSpace(effectiveConfig));

            Assert.Contains("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT=http://traces-collector:4318/v1/traces", effectiveConfig);
            Assert.Contains("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT=http://localhost:4318/v1/metrics", effectiveConfig);
            Assert.DoesNotContain("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=", effectiveConfig);
            Assert.Contains("OTEL_SERVICE_NAME=env-var-service", effectiveConfig);
            Assert.Contains("SPLUNK_PROFILER_ENABLED=True", effectiveConfig);
            Assert.Contains("SPLUNK_PROFILER_MEMORY_ENABLED=True", effectiveConfig);
            Assert.Contains("SPLUNK_PROFILER_CALL_STACK_INTERVAL=10000", effectiveConfig);
            Assert.Contains("SPLUNK_SNAPSHOT_PROFILER_ENABLED=True", effectiveConfig);
            Assert.Contains("SPLUNK_SNAPSHOT_SAMPLING_INTERVAL=5000", effectiveConfig);
        }
        finally
        {
            tempLogsDirectory.Delete(true);
        }
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void EffectiveYamlConfigUsesYamlDefaultsWhenOtlpEndpointsAreOmitted()
    {
        var tempLogsDirectory = DirectoryHelpers.CreateTempDirectory();

        EnableBytecodeInstrumentation();
        EnableFileBasedConfig("config-otlp-defaults.yaml");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY", tempLogsDirectory.FullName);
        SetEnvironmentVariable("OTEL_LOG_LEVEL", "debug");
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://env-collector:4318");
        SetEnvironmentVariable("OTEL_SERVICE_NAME", "stale-env-service");

        try
        {
            RunTestApplication();

            var logContent = File.ReadAllText(tempLogsDirectory.GetFiles("otel-dotnet-auto-*-Splunk-*.log").Single().FullName);
            var effectiveConfig = ExtractEffectiveConfigEntries(logContent);
            Assert.False(string.IsNullOrWhiteSpace(effectiveConfig));

            Assert.Contains("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT=http://localhost:4318/v1/traces", effectiveConfig);
            Assert.Contains("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT=http://localhost:4318/v1/metrics", effectiveConfig);
            Assert.DoesNotContain("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=", effectiveConfig);
            Assert.DoesNotContain("http://env-collector:4318", effectiveConfig);
            Assert.Contains("OTEL_SERVICE_NAME=yaml-defaults-service", effectiveConfig);
            Assert.DoesNotContain("OTEL_SERVICE_NAME=stale-env-service", effectiveConfig);
        }
        finally
        {
            tempLogsDirectory.Delete(true);
        }
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void EffectiveYamlConfigCombinesMultipleOtlpEndpointsForSameSignal()
    {
        var tempLogsDirectory = DirectoryHelpers.CreateTempDirectory();

        EnableBytecodeInstrumentation();
        EnableDefaultExporters();
        EnableFileBasedConfig("config-multiple-otlp-endpoints.yaml");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY", tempLogsDirectory.FullName);
        SetEnvironmentVariable("OTEL_LOG_LEVEL", "debug");
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");

        try
        {
            RunTestApplication();

            var logContent = File.ReadAllText(tempLogsDirectory.GetFiles("otel-dotnet-auto-*-Splunk-*.log").Single().FullName);
            var effectiveConfig = ExtractEffectiveConfigEntries(logContent);
            Assert.False(string.IsNullOrWhiteSpace(effectiveConfig));

            Assert.Equal(
                new[] { "http://localhost:4318/v1/traces,http://localhost:4319/v1/traces" },
                GetEffectiveConfigValues(effectiveConfig, EffectiveConfigKeys.TracesEndpoint));
            Assert.Equal(
                new[] { "http://localhost:4318/v1/metrics,http://localhost:4319/v1/metrics" },
                GetEffectiveConfigValues(effectiveConfig, EffectiveConfigKeys.MetricsEndpoint));
            Assert.Empty(GetEffectiveConfigValues(effectiveConfig, EffectiveConfigKeys.LogsEndpoint));
        }
        finally
        {
            tempLogsDirectory.Delete(true);
        }
    }

#endif

    public void Dispose()
    {
        _testServer.Dispose();
    }

    private static string ExtractEffectiveConfigEntries(string output)
    {
        var entries = output
            .Split([Environment.NewLine], StringSplitOptions.None)
            .Select(line =>
            {
                var prefixIndex = line.IndexOf(EffectiveConfigLogFormatter.Prefix, StringComparison.Ordinal);
                return prefixIndex < 0
                    ? null
                    : line.Substring(prefixIndex + EffectiveConfigLogFormatter.Prefix.Length).Trim();
            })
            .Where(entry => !string.IsNullOrEmpty(entry))
            .Select(entry => entry!);

        return string.Join(Environment.NewLine, entries);
    }

    private static string[] GetEffectiveConfigValues(string effectiveConfig, string key)
    {
        var prefix = key + "=";
        return effectiveConfig
            .Split([Environment.NewLine], StringSplitOptions.None)
            .Where(line => line.StartsWith(prefix, StringComparison.Ordinal))
            .Select(line => line.Substring(prefix.Length))
            .ToArray();
    }

    private static ICollection<KeyValuePair<string, string>> ParseSettingsLog(string log, string marker)
    {
        var lines = log.Split([Environment.NewLine], StringSplitOptions.None);
        var variables = lines
            .SkipWhile(x => !x.EndsWith(marker))
            .Skip(1)
            .TakeWhile(x => x.StartsWith("\t"))
            .Select(x => x.Trim())
            .ToEnvironmentVariablesList();

        return variables;
    }

    private TestSettings TestSettingsWithDefaultArgs()
    {
        return new TestSettings
        {
            Arguments = $"--test-server-port {_testServer.Port}"
        };
    }
}
