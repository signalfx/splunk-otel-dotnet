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
    public void EffectiveEnvVarConfigIsReportedToOpAmp()
    {
        using var opAmpServer = new MockOpAmpServer(Output);

        var tracesEndpoint = "http://localhost:4318/v1/traces";
        var metricsEndpoint = "http://localhost:4319/v1/metrics";
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", tracesEndpoint);
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", metricsEndpoint);
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

        var requiredEntries = new List<string>
        {
            EndpointEntry(EffectiveConfigKeys.TracesEndpoint, tracesEndpoint),
            EndpointEntry(EffectiveConfigKeys.MetricsEndpoint, metricsEndpoint),
            StringEntry(EffectiveConfigKeys.ServiceName, ServiceName),
            "SPLUNK_PROFILER_ENABLED=true",
#if NET
            "SPLUNK_PROFILER_MEMORY_ENABLED=true",
#else
            "SPLUNK_PROFILER_MEMORY_ENABLED=false",
#endif
            "SPLUNK_PROFILER_CALL_STACK_INTERVAL=\"10000ms\"",
            "SPLUNK_SNAPSHOT_PROFILER_ENABLED=true",
            "SPLUNK_SNAPSHOT_SAMPLING_INTERVAL=\"5000ms\""
        };
#if NET
        requiredEntries.Add(EndpointEntry(EffectiveConfigKeys.LogsEndpoint, logsEndpoint));
#endif
        var forbiddenEntries = new List<string>();
#if !NET
        forbiddenEntries.Add($"{EffectiveConfigKeys.LogsEndpoint}=");
#endif

        RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            payload => ContainsAll(payload, requiredEntries) && ContainsNone(payload, forbiddenEntries));
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void EffectiveEnvVarConfigUsesSplunkRealmEndpoints()
    {
        using var opAmpServer = new MockOpAmpServer(Output);

        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");
        SetEnvironmentVariable("SPLUNK_REALM", "us0");
        SetEnvironmentVariable("SPLUNK_ACCESS_TOKEN", "token");

        EnableBytecodeInstrumentation();
        EnableDefaultExporters();

        var requiredEntries = new[]
        {
            EndpointEntry(EffectiveConfigKeys.TracesEndpoint, "https://ingest.us0.observability.splunkcloud.com/v2/trace/otlp"),
            EndpointEntry(EffectiveConfigKeys.MetricsEndpoint, "https://ingest.us0.observability.splunkcloud.com/v2/datapoint/otlp")
        };

        RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            payload => ContainsAll(payload, requiredEntries));
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

#if NETFRAMEWORK
        var expectedTracesEndpoint = "http://collector:4317/v1/traces";
        var expectedMetricsEndpoint = "http://collector:4317/v1/metrics";
#else
        var expectedTracesEndpoint = "http://collector:4317/opentelemetry.proto.collector.trace.v1.TraceService/Export";
        var expectedMetricsEndpoint = "http://collector:4317/opentelemetry.proto.collector.metrics.v1.MetricsService/Export";
#endif

        var requiredEntries = new[]
        {
            EndpointEntry(EffectiveConfigKeys.TracesEndpoint, expectedTracesEndpoint),
            EndpointEntry(EffectiveConfigKeys.MetricsEndpoint, expectedMetricsEndpoint)
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

        var requiredEntries = new[]
        {
            StringEntry(EffectiveConfigKeys.ServiceName, ServiceName)
        };
        var forbiddenEntries = new[]
        {
            $"{EffectiveConfigKeys.TracesEndpoint}=",
            $"{EffectiveConfigKeys.MetricsEndpoint}=",
            $"{EffectiveConfigKeys.LogsEndpoint}="
        };

        RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            payload => ContainsAll(payload, requiredEntries) && ContainsNone(payload, forbiddenEntries));
    }

#if NET // File-based configuration is not supported on .NET Framework
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void EffectiveYamlConfigIsReportedToOpAmp()
    {
        using var opAmpServer = new MockOpAmpServer(Output);

        EnableBytecodeInstrumentation();
        EnableFileBasedConfig("config.yaml");
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");

        // Set traces and service name via env var; yaml substitutes them in.
        // Metrics endpoint is intentionally not set; yaml fallback value is used instead.
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "http://traces-collector:4318/v1/traces");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", "http://logs-collector:4318/v1/logs");
        SetEnvironmentVariable("OTEL_SERVICE_NAME", "env-var-service");

        var requiredEntries = new[]
        {
            EndpointEntry(EffectiveConfigKeys.TracesEndpoint, "http://traces-collector:4318/v1/traces"),
            EndpointEntry(EffectiveConfigKeys.MetricsEndpoint, "http://localhost:4318/v1/metrics"),
            StringEntry(EffectiveConfigKeys.ServiceName, "env-var-service"),
            "SPLUNK_PROFILER_ENABLED=true",
            "SPLUNK_PROFILER_MEMORY_ENABLED=true",
            "SPLUNK_PROFILER_CALL_STACK_INTERVAL=\"10000ms\"",
            "SPLUNK_SNAPSHOT_PROFILER_ENABLED=true",
            "SPLUNK_SNAPSHOT_SAMPLING_INTERVAL=\"5000ms\""
        };

        RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            payload => ContainsAll(payload, requiredEntries) &&
                       ContainsNone(payload, [$"{EffectiveConfigKeys.LogsEndpoint}="]));
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
            EndpointEntry(EffectiveConfigKeys.TracesEndpoint, "http://localhost:4318/v1/traces"),
            EndpointEntry(EffectiveConfigKeys.MetricsEndpoint, "http://localhost:4318/v1/metrics"),
            StringEntry(EffectiveConfigKeys.ServiceName, "yaml-defaults-service")
        };

        var forbiddenEntries = new[]
        {
            $"{EffectiveConfigKeys.LogsEndpoint}=",
            "http://env-collector:4318",
            StringEntry(EffectiveConfigKeys.ServiceName, "stale-env-service")
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
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");

        var expectedTracesValue = EndpointValue(tracesEndpoint1, tracesEndpoint2);
        var expectedMetricsValue = EndpointValue(metricsEndpoint1, metricsEndpoint2);
        var expectedLogsValue = EndpointValue(logsEndpoint1, logsEndpoint2);
        RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            payload =>
                GetEffectiveConfigValues(payload, EffectiveConfigKeys.TracesEndpoint).SequenceEqual([expectedTracesValue]) &&
                GetEffectiveConfigValues(payload, EffectiveConfigKeys.MetricsEndpoint).SequenceEqual([expectedMetricsValue]) &&
                GetEffectiveConfigValues(payload, EffectiveConfigKeys.LogsEndpoint).SequenceEqual([expectedLogsValue]));
    }

#endif

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

    private static string EndpointEntry(string key, string endpoint)
    {
        return $"{key}={EndpointValue(endpoint)}";
    }

    private static string StringEntry(string key, string value)
    {
        return $"{key}={QuoteValue(value)}";
    }

    private static string EndpointValue(params string[] endpoints)
    {
        return string.Join(",", endpoints.Select(QuoteValue));
    }

    private static string QuoteValue(string value)
    {
        return "\"" + value + "\"";
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

    private static bool ReportsEffectiveConfigCapability(AgentToServer frame)
    {
        const ulong reportsEffectiveConfigCapability = (ulong)AgentCapabilities.ReportsEffectiveConfig;
        return (frame.Capabilities & reportsEffectiveConfigCapability) != 0;
    }

    private static bool TryReadEffectiveConfigPayload(AgentToServer frame, out string payload)
    {
        payload = string.Empty;

        var configMap = frame.EffectiveConfig?.ConfigMap?.ConfigMap;
        if (configMap == null ||
            configMap.Count != 1 ||
            !configMap.TryGetValue(EffectiveConfigReporter.EffectiveConfigFileName, out var configFile) ||
            configFile.ContentType != EffectiveConfigReporter.EffectiveConfigContentType)
        {
            return false;
        }

        payload = configFile.Body.ToStringUtf8();
        return !string.IsNullOrWhiteSpace(payload);
    }

    private static void AssertEffectiveConfigFiles(MockOpAmpServer opAmpServer, int maxEffectiveConfigFiles)
    {
        var frames = opAmpServer.GetEffectiveConfigFrames();
        Assert.NotEmpty(frames);

        var fileCounts = frames.Select(frame => frame.Files.Count).ToArray();
        if (fileCounts.Any(count => count != 1))
        {
            Assert.Fail($"Expected each effective config frame to contain exactly one file, but received file counts: {string.Join(", ", fileCounts)}.");
        }

        var files = frames.SelectMany(frame => frame.Files).ToArray();
        if (files.Length > maxEffectiveConfigFiles)
        {
            Assert.Fail($"Expected at most {maxEffectiveConfigFiles} effective config files, but received {files.Length}.");
        }

        Assert.All(files, file =>
        {
            Assert.Equal(EffectiveConfigReporter.EffectiveConfigFileName, file.Name);
            Assert.Equal(EffectiveConfigReporter.EffectiveConfigContentType, file.ContentType);
            Assert.False(string.IsNullOrWhiteSpace(file.Body));
        });

        var duplicatePayloads = files
            .GroupBy(file => file.Body, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        Assert.Empty(duplicatePayloads);
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
        int maxEffectiveConfigFiles = 2)
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", $"http://localhost:{opAmpServer.Port}/v1/opamp");

        opAmpServer.Expect(ReportsEffectiveConfigCapability, "Reports effective config capability");
        opAmpServer.Expect(
            frame => TryReadEffectiveConfigPayload(frame, out var payload) && effectiveConfigPredicate(payload),
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

        AssertEffectiveConfigFiles(opAmpServer, maxEffectiveConfigFiles);
    }
}
