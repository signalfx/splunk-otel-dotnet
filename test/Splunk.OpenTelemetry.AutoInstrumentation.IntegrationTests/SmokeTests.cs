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

using OpAmp.Proto.V1;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests;

public class SmokeTests : TestHelper, IDisposable
{
    private const string ServiceName = "TestApplication.Smoke";
    private readonly Lazy<TestHttpServer> _testServer;

    public SmokeTests(ITestOutputHelper output)
        : base("Smoke", output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS", "HttpClient");
        SetEnvironmentVariable("OTEL_EXPERIMENTAL_FILE_BASED_CONFIGURATION_ENABLED", "false");

        _testServer = new Lazy<TestHttpServer>(() => TestHttpServer.CreateDefaultTestServer(output));
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
        SetExporter(collector);

        collector.ResourceExpector.ExpectDistributionResources(ServiceName);

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_BLRP_MAX_EXPORT_BATCH_SIZE", "1");

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
    public void EffectiveEnvVarConfigIsReportedToOpAmp()
    {
        using var opAmpServer = new MockOpAmpServer(Output);
        using var profilesCollector = new MockContinuousProfilerCollector(Output);

        var tracesEndpoint = "http://localhost:4318/v1/traces";
        var metricsEndpoint = "http://localhost:4319/v1/metrics";
        var profilerLogsEndpoint = $"http://localhost:{profilesCollector.Port}/v1/logs";
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", tracesEndpoint);
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", metricsEndpoint);
        SetExporter(profilesCollector);
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");
#if NET
        var logsEndpoint = "http://localhost:4320/v1/logs";
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", logsEndpoint);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");
#endif
        SetEnvironmentVariable("SPLUNK_PROFILER_ENABLED", "true");
        SetEnvironmentVariable("SPLUNK_PROFILER_MEMORY_ENABLED", "true");
        SetEnvironmentVariable("SPLUNK_PROFILER_CALL_STACK_INTERVAL", "10000");
        SetEnvironmentVariable("SPLUNK_SNAPSHOT_PROFILER_ENABLED", "true");
        SetEnvironmentVariable("SPLUNK_SNAPSHOT_SAMPLING_INTERVAL", "5000");

        EnableBytecodeInstrumentation();
        EnableDefaultExporters();

        // ILogger instrumentation is required to be able to capture logs endpoint.
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED", "false");

        var requiredEntries = new List<string>
        {
            $"OTEL_EXPORTER_OTLP_TRACES_ENDPOINTS=\"{tracesEndpoint}\"",
            $"OTEL_EXPORTER_OTLP_METRICS_ENDPOINTS=\"{metricsEndpoint}\"",
            "OTEL_SERVICE_NAME=\"TestApplication.Smoke\"",
            "SPLUNK_PROFILER_ENABLED=true",
#if NET
            "SPLUNK_PROFILER_MEMORY_ENABLED=true",
#else
            "SPLUNK_PROFILER_MEMORY_ENABLED=false",
#endif
            "SPLUNK_PROFILER_CALL_STACK_INTERVAL=\"10000ms\"",
            $"SPLUNK_PROFILER_LOGS_ENDPOINT=\"{profilerLogsEndpoint}\"",
            "SPLUNK_SNAPSHOT_PROFILER_ENABLED=true",
            "SPLUNK_SNAPSHOT_PROFILER_SAMPLING_INTERVAL=\"5000ms\""
        };
#if NET
        requiredEntries.Add($"OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS=\"{logsEndpoint}\"");
#endif
        var forbiddenEntries = new List<string>();
#if !NET
        forbiddenEntries.Add("OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS=");
#endif

        RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            payload => ContainsAll(payload, requiredEntries) && ContainsNone(payload, forbiddenEntries));
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void EffectiveEnvVarConfigUsesResolvedOtlpProtocol()
    {
        using var opAmpServer = new MockOpAmpServer(Output);

        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4317");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc");

        EnableBytecodeInstrumentation();
        EnableDefaultExporters();
        DisableAllInstrumentations();

#if NETFRAMEWORK
        var expectedTracesEndpoint = "http://collector:4317/v1/traces";
        var expectedMetricsEndpoint = "http://collector:4317/v1/metrics";
#else
        var expectedTracesEndpoint = "http://collector:4317/opentelemetry.proto.collector.trace.v1.TraceService/Export";
        var expectedMetricsEndpoint = "http://collector:4317/opentelemetry.proto.collector.metrics.v1.MetricsService/Export";
#endif

        var requiredEntries = new[]
        {
            $"OTEL_EXPORTER_OTLP_TRACES_ENDPOINTS=\"{expectedTracesEndpoint}\"",
            $"OTEL_EXPORTER_OTLP_METRICS_ENDPOINTS=\"{expectedMetricsEndpoint}\""
        };

        RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            payload => ContainsAll(payload, requiredEntries));
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void EffectiveEnvVarConfigOmitsOtlpEndpointsWhenOtlpExportersAreDisabled()
    {
        using var opAmpServer = new MockOpAmpServer(Output);

        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "http://traces-collector:4318/v1/traces");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", "http://metrics-collector:4318/v1/metrics");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", "http://logs-collector:4318/v1/logs");

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_TRACES_EXPORTER", "none");
        SetEnvironmentVariable("OTEL_METRICS_EXPORTER", "none");
        SetEnvironmentVariable("OTEL_LOGS_EXPORTER", "none");
        DisableAllInstrumentations();

        var requiredEntries = new[]
        {
            "OTEL_SERVICE_NAME=\"TestApplication.Smoke\""
        };
        var forbiddenEntries = new[]
        {
            "OTEL_EXPORTER_OTLP_TRACES_ENDPOINTS=",
            "OTEL_EXPORTER_OTLP_METRICS_ENDPOINTS=",
            "OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS="
        };

        RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            payload => ContainsAll(payload, requiredEntries) && ContainsNone(payload, forbiddenEntries));
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void EffectiveYamlConfigIsReportedToOpAmp()
    {
        using var opAmpServer = new MockOpAmpServer(Output);
        using var profilesCollector = new MockContinuousProfilerCollector(Output);
        var profilerLogsEndpoint = $"http://localhost:{profilesCollector.Port}/v1/logs";

        EnableBytecodeInstrumentation();
        EnableFileBasedConfig("config.yaml");
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");
        SetExporter(profilesCollector);

        // Set traces and service name via env var; yaml substitutes them in.
        // Metrics endpoint is intentionally not set; yaml fallback value is used instead.
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "http://traces-collector:4318/v1/traces");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", "http://logs-collector:4318/v1/logs");
        SetEnvironmentVariable("OTEL_SERVICE_NAME", "env-var-service");

        var requiredEntries = new[]
        {
            "OTEL_EXPORTER_OTLP_TRACES_ENDPOINTS=\"http://traces-collector:4318/v1/traces\"",
            "OTEL_EXPORTER_OTLP_METRICS_ENDPOINTS=\"http://localhost:4318/v1/metrics\"",
            "OTEL_SERVICE_NAME=\"env-var-service\"",
            "SPLUNK_PROFILER_ENABLED=true",
#if NET
            "SPLUNK_PROFILER_MEMORY_ENABLED=true",
#else
            "SPLUNK_PROFILER_MEMORY_ENABLED=false",
#endif
            "SPLUNK_PROFILER_CALL_STACK_INTERVAL=\"10000ms\"",
            $"SPLUNK_PROFILER_LOGS_ENDPOINT=\"{profilerLogsEndpoint}\"",
            "SPLUNK_SNAPSHOT_PROFILER_ENABLED=true",
            "SPLUNK_SNAPSHOT_PROFILER_SAMPLING_INTERVAL=\"5000ms\""
        };

        RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            payload => ContainsAll(payload, requiredEntries) &&
                       ContainsNone(payload, ["OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS="]));
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void EffectiveYamlConfigUsesYamlDefaultsWhenOtlpEndpointsAreOmitted()
    {
        using var opAmpServer = new MockOpAmpServer(Output);

        EnableBytecodeInstrumentation();
        EnableFileBasedConfig("config-otlp-defaults.yaml");
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://env-collector:4318");
        SetEnvironmentVariable("OTEL_SERVICE_NAME", "stale-env-service");

        var requiredEntries = new[]
        {
            "OTEL_EXPORTER_OTLP_TRACES_ENDPOINTS=\"http://localhost:4318/v1/traces\"",
            "OTEL_EXPORTER_OTLP_METRICS_ENDPOINTS=\"http://localhost:4318/v1/metrics\"",
            "OTEL_SERVICE_NAME=\"yaml-defaults-service\""
        };

        var forbiddenEntries = new[]
        {
            "OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS=",
            "http://env-collector:4318",
            "OTEL_SERVICE_NAME=\"stale-env-service\""
        };

        RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            payload => ContainsAll(payload, requiredEntries) && ContainsNone(payload, forbiddenEntries));
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void EffectiveYamlConfigCombinesMultipleOtlpEndpointsForSameSignal()
    {
        using var opAmpServer = new MockOpAmpServer(Output);

        var tracesEndpoint1 = "http://localhost:4318/v1/traces";
        var tracesEndpoint2 = "http://localhost:4319/v1/traces";
        var metricsEndpoint1 = "http://localhost:4318/v1/metrics";
        var metricsEndpoint2 = "http://localhost:4319/v1/metrics";
        var logsEndpoint1 = "http://localhost:4318/v1/logs";
        var logsEndpoint2 = "http://localhost:4319/v1/logs";

        EnableBytecodeInstrumentation();
        EnableDefaultExporters();
        EnableFileBasedConfig("config-multiple-otlp-endpoints.yaml");
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");

        var expectedTracesValue = $"\"{tracesEndpoint1}\",\"{tracesEndpoint2}\"";
        var expectedMetricsValue = $"\"{metricsEndpoint1}\",\"{metricsEndpoint2}\"";
        var expectedLogsValue = $"\"{logsEndpoint1}\",\"{logsEndpoint2}\"";
        RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            payload =>
                GetEffectiveConfigValues(payload, "OTEL_EXPORTER_OTLP_TRACES_ENDPOINTS").SequenceEqual([expectedTracesValue]) &&
                GetEffectiveConfigValues(payload, "OTEL_EXPORTER_OTLP_METRICS_ENDPOINTS").SequenceEqual([expectedMetricsValue]) &&
#if NET
                GetEffectiveConfigValues(payload, "OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS").SequenceEqual([expectedLogsValue]));
#else
                GetEffectiveConfigValues(payload, "OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS").Length == 0);
#endif
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void RemoteYamlConfigIsAppliedFromOpAmp()
    {
        using var opAmpServer = new MockOpAmpServer(Output);

        opAmpServer.OfferRemoteConfig(
            """
            distribution:
              splunk:
                profiling:
                  always_on:
                    cpu_profiler:
                      sampling_interval: 1200
                    memory_profiler:
                      max_memory_samples: 123
                  callgraphs:
                    sampling_interval: 300
                    selection_probability: 1.0
                    high_resolution_timer_enabled: true
            """);

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");
        SetEnvironmentVariable("SPLUNK_OPAMP_REMOTE_CONFIG", "true");
        SetEnvironmentVariable("SPLUNK_PROFILER_ENABLED", "false");
        SetEnvironmentVariable("SPLUNK_PROFILER_MEMORY_ENABLED", "false");
        SetEnvironmentVariable("SPLUNK_SNAPSHOT_PROFILER_ENABLED", "false");

        var requiredEntries = new[]
        {
            "SPLUNK_PROFILER_ENABLED=true",
#if NET
            "SPLUNK_PROFILER_MEMORY_ENABLED=true",
#else
            "SPLUNK_PROFILER_MEMORY_ENABLED=false",
#endif
            "SPLUNK_PROFILER_CALL_STACK_INTERVAL=\"1200ms\"",
            "SPLUNK_PROFILER_LOGS_ENDPOINT=\"http://localhost:4318/v1/logs\"",
            "SPLUNK_SNAPSHOT_PROFILER_ENABLED=true",
            "SPLUNK_SNAPSHOT_PROFILER_SAMPLING_INTERVAL=\"300ms\""
        };

        RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            payload => ContainsAll(payload, requiredEntries),
            frame => ReportsEffectiveConfigCapability(frame) && AcceptsRemoteConfigCapability(frame),
            "Reports effective config and accepts remote config capability");
    }

    public void Dispose()
    {
        if (_testServer.IsValueCreated)
        {
            _testServer.Value.Dispose();
        }
    }

    private static bool ContainsAll(string payload, IEnumerable<string> requiredEntries)
    {
        return requiredEntries.All(entry => payload.IndexOf(entry, StringComparison.Ordinal) >= 0);
    }

    private static bool ContainsNone(string payload, IEnumerable<string> forbiddenEntries)
    {
        return forbiddenEntries.All(entry => payload.IndexOf(entry, StringComparison.Ordinal) < 0);
    }

    private static string[] GetEffectiveConfigValues(string effectiveConfig, string key)
    {
        var prefix = key + "=";
        return effectiveConfig
            .Split(['\n'], StringSplitOptions.None)
            .Where(line => line.StartsWith(prefix, StringComparison.Ordinal))
            .Select(line => line.Substring(prefix.Length))
            .ToArray();
    }

    private static bool ReportsEffectiveConfigCapability(AgentToServer frame)
    {
        const ulong reportsEffectiveConfigCapability = (ulong)AgentCapabilities.ReportsEffectiveConfig;
        return (frame.Capabilities & reportsEffectiveConfigCapability) != 0;
    }

    private static bool AcceptsRemoteConfigCapability(AgentToServer frame)
    {
        const ulong acceptsRemoteConfigCapability = (ulong)AgentCapabilities.AcceptsRemoteConfig;
        return (frame.Capabilities & acceptsRemoteConfigCapability) != 0;
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
            Arguments = $"--test-server-port {_testServer.Value.Port}"
        };
    }

    private void RunTestApplicationAndAssertEffectiveConfig(
        MockOpAmpServer opAmpServer,
        Func<string, bool> effectiveConfigPredicate,
        Func<AgentToServer, bool>? capabilityPredicate = null,
        string capabilityDescription = "Reports effective config capability")
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", $"http://localhost:{opAmpServer.Port}/v1/opamp");

        opAmpServer.Expect(capabilityPredicate ?? ReportsEffectiveConfigCapability, capabilityDescription);
        opAmpServer.ExpectEffectiveConfigPayload(
            EffectiveConfigReporter.EffectiveConfigFileName,
            EffectiveConfigReporter.EffectiveConfigContentType,
            effectiveConfigPredicate,
            "Has expected single-file effective config payload");
        SetEnvironmentVariable("LONG_RUNNING", "true");

        using var process = StartTestApplication();
        Output.WriteLine("ProcessName: " + process?.ProcessName);
        using var helper = new ProcessHelper(process);

        Assert.NotNull(process);
        try
        {
            opAmpServer.AssertExpectations();
        }
        finally
        {
            if (!process!.HasExited)
            {
                process.Kill();
            }

            var processExited = process.WaitForExit((int)TestTimeout.ProcessExit.TotalMilliseconds);
            helper.Drain(TimeSpan.FromSeconds(5));
            Output.WriteLine("ProcessId: " + process.Id);
            Output.WriteLine(processExited ? "Exit Code: " + process.ExitCode : "Exit Code: <process still running>");
            if (processExited)
            {
                Output.WriteResult(helper);
            }
        }

        opAmpServer.AssertEffectiveConfigPayloads(
            EffectiveConfigReporter.EffectiveConfigFileName,
            EffectiveConfigReporter.EffectiveConfigContentType,
            effectiveConfigPredicate);
    }

    private void DisableAllInstrumentations()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED", "false");
    }
}
