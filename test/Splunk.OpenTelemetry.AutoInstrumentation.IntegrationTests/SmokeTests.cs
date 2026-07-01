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
using YamlDotNet.RepresentationModel;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests;

public class SmokeTests : TestHelper, IDisposable
{
    private const string ServiceName = "TestApplication.Smoke";
    private const string ExpectedYamlConfigContentType = "application/yaml; vendor=splunk; v=1.0.0";
    private const string ExpectedEnvVarConfigContentType = "text/plain; format=properties; vendor=splunk; v=1.0.0";
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

        var expectedPayload = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"] = tracesEndpoint,
            ["OTEL_EXPORTER_OTLP_METRICS_ENDPOINT"] = metricsEndpoint,
#if NET
            ["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"] = logsEndpoint,
#else
            ["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"] = "none",
#endif
            ["SPLUNK_PROFILER_ENABLED"] = "true",
#if NET
            ["SPLUNK_PROFILER_MEMORY_ENABLED"] = "true",
#else
            ["SPLUNK_PROFILER_MEMORY_ENABLED"] = "false",
#endif
            ["SPLUNK_SNAPSHOT_PROFILER_ENABLED"] = "true",
            ["SPLUNK_SNAPSHOT_PROFILER_SAMPLING_INTERVAL"] = "5000",
            ["SPLUNK_PROFILER_CALL_STACK_INTERVAL"] = "10000",
            ["OTEL_CONFIG_FILE"] = "null",
            ["OTEL_EXPERIMENTAL_CONFIG_FILE"] = "null"
        };

        var payload = RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            payload => EnvironmentConfigPayloadMatches(payload, expectedPayload),
            "environment",
            ExpectedEnvVarConfigContentType);

        AssertEnvironmentConfigPayload(payload, expectedPayload);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void EffectiveEnvVarConfigReportsBridgeLoggerProviderEndpoint()
    {
        // Upstream evaluates LoggerProviderFactory while aggregating resources for
        // OpAMP initialization. Enabling the NLog bridge therefore creates the
        // LoggerProvider even when the application does not reference NLog or emit
        // any NLog events. This test verifies that we resolve that already-created
        // provider through the upstream private static field.

        using var opAmpServer = new MockOpAmpServer(Output);

        const string logsEndpoint = "http://logs-collector:4318/v1/logs";

        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");
        SetEnvironmentVariable("OTEL_LOGS_EXPORTER", "otlp");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", logsEndpoint);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLE_NLOG_BRIDGE", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ILOGGER_INSTRUMENTATION_ENABLED", "false");

        EnableBytecodeInstrumentation();

        RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            payload => payload.IndexOf(
                $"OTEL_EXPORTER_OTLP_LOGS_ENDPOINT={logsEndpoint}", StringComparison.Ordinal) >= 0,
            "environment",
            ExpectedEnvVarConfigContentType);
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

        var expectedPayload = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"] = expectedTracesEndpoint,
            ["OTEL_EXPORTER_OTLP_METRICS_ENDPOINT"] = expectedMetricsEndpoint,
            ["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"] = "none",
            ["SPLUNK_PROFILER_ENABLED"] = "false",
            ["SPLUNK_PROFILER_MEMORY_ENABLED"] = "false",
            ["SPLUNK_SNAPSHOT_PROFILER_ENABLED"] = "false",
            ["SPLUNK_SNAPSHOT_PROFILER_SAMPLING_INTERVAL"] = "40",
            ["SPLUNK_PROFILER_CALL_STACK_INTERVAL"] = "10000",
            ["OTEL_CONFIG_FILE"] = "null",
            ["OTEL_EXPERIMENTAL_CONFIG_FILE"] = "null"
        };

        var payload = RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            payload => EnvironmentConfigPayloadMatches(payload, expectedPayload),
            "environment",
            ExpectedEnvVarConfigContentType);

        AssertEnvironmentConfigPayload(payload, expectedPayload);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void EffectiveEnvVarConfigUsesDefaultsWhenOtlpExportersAreDisabled()
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

        var expectedPayload = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"] = "none",
            ["OTEL_EXPORTER_OTLP_METRICS_ENDPOINT"] = "none",
            ["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"] = "none",
            ["SPLUNK_PROFILER_ENABLED"] = "false",
            ["SPLUNK_PROFILER_MEMORY_ENABLED"] = "false",
            ["SPLUNK_SNAPSHOT_PROFILER_ENABLED"] = "false",
            ["SPLUNK_SNAPSHOT_PROFILER_SAMPLING_INTERVAL"] = "40",
            ["SPLUNK_PROFILER_CALL_STACK_INTERVAL"] = "10000",
            ["OTEL_CONFIG_FILE"] = "null",
            ["OTEL_EXPERIMENTAL_CONFIG_FILE"] = "null"
        };

        var payload = RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            payload => EnvironmentConfigPayloadMatches(payload, expectedPayload),
            "environment",
            ExpectedEnvVarConfigContentType);

        AssertEnvironmentConfigPayload(payload, expectedPayload);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public Task EffectiveYamlConfigIsReportedToOpAmp()
    {
        using var opAmpServer = new MockOpAmpServer(Output);
        using var profilesCollector = new MockContinuousProfilerCollector(Output);

        var configFile = GetFileBasedConfigPath("config.yaml");
        EnableBytecodeInstrumentation();
        EnableFileBasedConfig("config.yaml");
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");
        SetExporter(profilesCollector);

        // Set traces and service name via env var; yaml substitutes them in.
        // Metrics endpoint is intentionally not set; yaml fallback value is used instead.
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "http://traces-collector:4318/v1/traces");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", "http://logs-collector:4318/v1/logs");

        var payload = RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            HasEffectiveConfigPayload,
            configFile,
            ExpectedYamlConfigContentType);

        return VerifyEffectiveConfigPayload(payload, configFile);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public Task EffectiveYamlConfigUsesYamlDefaultsWhenOtlpEndpointsAreOmitted()
    {
        using var opAmpServer = new MockOpAmpServer(Output);

        var configFile = GetFileBasedConfigPath("config-otlp-defaults.yaml");
        EnableBytecodeInstrumentation();
        EnableFileBasedConfig("config-otlp-defaults.yaml");
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://env-collector:4318");

        var payload = RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            HasEffectiveConfigPayload,
            configFile,
            ExpectedYamlConfigContentType);

        return VerifyEffectiveConfigPayload(payload, configFile);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public Task EffectiveYamlConfigCombinesMultipleOtlpEndpointsForSameSignal()
    {
        using var opAmpServer = new MockOpAmpServer(Output);

        var configFile = GetFileBasedConfigPath("config-multiple-otlp-endpoints.yaml");
        EnableBytecodeInstrumentation();
        EnableDefaultExporters();
        EnableFileBasedConfig("config-multiple-otlp-endpoints.yaml");
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");

        var payload = RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            HasReceivedFinalEffectiveConfig,
            configFile,
            ExpectedYamlConfigContentType);

        return VerifyEffectiveConfigPayload(payload, configFile);
    }

#if NET
    [Fact]
    [Trait("Category", "EndToEnd")]
    public Task EffectiveYamlConfigPreservesGrpcOtlpExporter()
    {
        using var opAmpServer = new MockOpAmpServer(Output);

        var configFile = GetFileBasedConfigPath("config-otlp-grpc.yaml");
        EnableBytecodeInstrumentation();
        EnableFileBasedConfig("config-otlp-grpc.yaml");
        SetEnvironmentVariable("SKIP_TELEMETRY_EMISSION", "true");

        var payload = RunTestApplicationAndAssertEffectiveConfig(
            opAmpServer,
            HasEffectiveConfigPayload,
            configFile,
            ExpectedYamlConfigContentType);

        return VerifyEffectiveConfigPayload(payload, configFile);
    }

#endif

    public void Dispose()
    {
        if (_testServer.IsValueCreated)
        {
            _testServer.Value.Dispose();
        }
    }

    private static bool HasEffectiveConfigPayload(string payload)
    {
        return !string.IsNullOrWhiteSpace(payload);
    }

    private static bool HasReceivedFinalEffectiveConfig(string payload)
    {
        // On .NET, ILogger endpoint capture can happen after the initial OpAmp
        // effective config report is sent, so logger_provider may arrive in a
        // subsequent effective config message.
#if NET
        return HasYamlSequenceItemCount(payload, "logger_provider", "processors", 2);
#else
        return HasEffectiveConfigPayload(payload);
#endif
    }

    private static bool HasYamlSequenceItemCount(string payload, string sectionName, string sequenceName, int expectedItemCount)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            var yaml = new YamlStream();
            using var reader = new StringReader(payload);
            yaml.Load(reader);

            if (yaml.Documents.Count == 0 ||
                yaml.Documents[0].RootNode is not YamlMappingNode root ||
                !TryGetMappingChild(root, sectionName, out var sectionNode) ||
                sectionNode is not YamlMappingNode section ||
                !TryGetMappingChild(section, sequenceName, out var sequenceNode) ||
                sequenceNode is not YamlSequenceNode sequence)
            {
                return false;
            }

            return sequence.Children.Count >= expectedItemCount;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetMappingChild(YamlMappingNode mapping, string key, out YamlNode value)
    {
        return mapping.Children.TryGetValue(new YamlScalarNode(key), out value!);
    }

    private static bool EnvironmentConfigPayloadMatches(
        string payload,
        IReadOnlyDictionary<string, string> expected)
    {
        try
        {
            var actual = ParseEnvironmentConfigPayload(payload);
            return expected.Count == actual.Count &&
                expected.All(entry => actual.TryGetValue(entry.Key, out var value) && string.Equals(value, entry.Value, StringComparison.Ordinal));
        }
        catch
        {
            return false;
        }
    }

    private static void AssertEnvironmentConfigPayload(
        string payload,
        IReadOnlyDictionary<string, string> expected)
    {
        var actual = ParseEnvironmentConfigPayload(payload);

        Assert.Equal(expected.Keys.OrderBy(key => key, StringComparer.Ordinal), actual.Keys.OrderBy(key => key, StringComparer.Ordinal));
        foreach (var entry in expected)
        {
            Assert.Equal(entry.Value, actual[entry.Key]);
        }
    }

    private static IReadOnlyDictionary<string, string> ParseEnvironmentConfigPayload(string payload)
    {
        var normalizedPayload = NormalizeLineEndings(payload);
        if (normalizedPayload.EndsWith("\n", StringComparison.Ordinal))
        {
            normalizedPayload = normalizedPayload.Substring(0, normalizedPayload.Length - 1);
        }

        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in normalizedPayload.Split('\n'))
        {
            if (string.IsNullOrEmpty(line))
            {
                throw new InvalidOperationException("Environment effective config payload must not contain blank lines.");
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                throw new InvalidOperationException($"Environment effective config line must be in key=value form: '{line}'.");
            }

            var key = line.Substring(0, separatorIndex);
            var value = line.Substring(separatorIndex + 1);
            if (entries.ContainsKey(key))
            {
                throw new InvalidOperationException($"Environment effective config contains duplicate key '{key}'.");
            }

            entries.Add(key, value);
        }

        return entries;
    }

    private static string NormalizeLineEndings(string value)
    {
        return value
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");
    }

    private static string RemoveTrailingNewLine(string value)
    {
        return value.EndsWith("\n", StringComparison.Ordinal)
            ? value.Substring(0, value.Length - 1)
            : value;
    }

    private static string ToYamlDoubleQuotedString(string value)
    {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private static Task VerifyEffectiveConfigPayload(string payload, string configFile)
    {
        var normalizedPayload = RemoveTrailingNewLine(NormalizeLineEndings(payload))
            .Replace(ToYamlDoubleQuotedString(configFile), "\"{configFile}\"");

        var verification = Verifier.Verify(normalizedPayload).DisableDiff();
#if NETFRAMEWORK
        verification = verification.UseTextForParameters("netfx");
#endif

        return verification;
    }

    private static bool ReportsEffectiveConfigCapability(AgentToServer frame)
    {
        const ulong reportsEffectiveConfigCapability = (ulong)AgentCapabilities.ReportsEffectiveConfig;
        return (frame.Capabilities & reportsEffectiveConfigCapability) != 0;
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

    private string GetFileBasedConfigPath(string fileName)
    {
        return Path.Combine(EnvironmentHelper.GetTestApplicationApplicationOutputDirectory(), fileName);
    }

    private TestSettings TestSettingsWithDefaultArgs()
    {
        return new TestSettings
        {
            Arguments = $"--test-server-port {_testServer.Value.Port}"
        };
    }

    private string RunTestApplicationAndAssertEffectiveConfig(
        MockOpAmpServer opAmpServer,
        Func<string, bool> effectiveConfigPredicate,
        string fileName,
        string contentType)
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", $"http://localhost:{opAmpServer.Port}/v1/opamp");

        opAmpServer.Expect(ReportsEffectiveConfigCapability, "Reports effective config capability");
        opAmpServer.ExpectEffectiveConfigPayload(
            fileName,
            contentType,
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
            if (!process.HasExited)
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

        return opAmpServer.AssertEffectiveConfigPayloads(
            fileName,
            contentType,
            effectiveConfigPredicate);
    }

    private void DisableAllInstrumentations()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED", "false");
    }
}
