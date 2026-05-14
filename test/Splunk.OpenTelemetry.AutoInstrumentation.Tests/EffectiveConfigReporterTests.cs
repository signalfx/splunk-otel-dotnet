// <copyright file="EffectiveConfigReporterTests.cs" company="Splunk Inc.">
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

public class EffectiveConfigReporterTests
{
    [Fact]
    public void CaptureLogExporterOptions_AddsLogsEndpointToPayload_ForILogger()
    {
        var reporter = CreateILoggerReporter();

        reporter.CaptureLogExporterOptions(CreateHttpLogOptions("http://logs-collector:4318/v1/logs"));

        Assert.Contains("OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS=\"http://logs-collector:4318/v1/logs\"", reporter.BuildCurrentPayload());
    }

    [Fact]
    public void CaptureLogExporterOptions_AccumulatesMultipleConfiguredOtlpLogExporters_ForILogger()
    {
        var reporter = CreateILoggerReporter();

        reporter.CaptureLogExporterOptions(CreateHttpLogOptions("http://logs-collector-1:4318/v1/logs"));
        reporter.CaptureLogExporterOptions(CreateHttpLogOptions("http://logs-collector-2:4318/v1/logs"));

        Assert.Contains(
            "OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS=\"http://logs-collector-1:4318/v1/logs\",\"http://logs-collector-2:4318/v1/logs\"",
            reporter.BuildCurrentPayload());
    }

    [Fact]
    public void BuildCurrentPayload_UsesBridgeLoggerProviderEndpoints_WhenILoggerWasNotConfigured()
    {
        var reporter = CreateReporter(() => ["http://bridge-collector:4318/v1/logs"]);

        Assert.Contains(
            "OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS=\"http://bridge-collector:4318/v1/logs\"",
            reporter.BuildCurrentPayload());
    }

    [Fact]
    public void BuildCurrentPayload_UsesLineFeedLineEndings()
    {
        var configuration = new NameValueCollection
        {
            { ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled, "true" },
            { ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled, "true" },
            { ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval, "10000" },
            { ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerLogsEndpoint, "http://profiler-collector:4318/v1/logs" },
            { ConfigurationKeys.Splunk.Snapshots.Enabled, "true" },
            { ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs, "5000" }
        };
        var reporter = CreateReporter();

        reporter.CaptureSplunkSettings(new PluginSettings(new NameValueConfigurationSource(configuration)));

        var payload = reporter.BuildCurrentPayload();
        Assert.Contains('\n', payload);
        Assert.DoesNotContain("\r", payload);
    }

    [Fact]
    public void BuildCurrentPayload_UsesBridgeLoggerProviderEndpoints_WhenOptionsHookRanWithoutILogger()
    {
        var reporter = CreateReporter(() => ["http://bridge-collector:4318/v1/logs"]);

        reporter.CaptureLogExporterOptions(CreateHttpLogOptions("http://options-collector:4318/v1/logs"));

        var payload = reporter.BuildCurrentPayload();
        Assert.Contains("OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS=\"http://bridge-collector:4318/v1/logs\"", payload);
        Assert.DoesNotContain("http://options-collector:4318/v1/logs", payload);
    }

    [Fact]
    public void CaptureLogExporterOptions_IgnoresEndpoint_WhenILoggerWasNotConfigured()
    {
        var reporter = CreateReporter();

        reporter.CaptureLogExporterOptions(CreateHttpLogOptions("http://options-collector:4318/v1/logs"));

        Assert.DoesNotContain("OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS=", reporter.BuildCurrentPayload());
    }

    [Fact]
    public void BuildCurrentPayload_DoesNotReportLogs_WhenBridgeLoggerProviderHasNoOtlpEndpoints()
    {
        var reporter = CreateReporter(() => []);

        Assert.DoesNotContain("OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS=", reporter.BuildCurrentPayload());
    }

    [Fact]
    public void BuildCurrentPayload_DoesNotReportLogs_WhenBridgeLoggerProviderCouldNotBeResolved()
    {
        var reporter = CreateReporter(() => null);

        Assert.DoesNotContain("OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS=", reporter.BuildCurrentPayload());
    }

    [Fact]
    public void BuildCurrentPayload_DoesNotReportLogs_WhenBridgeLoggerProviderResolutionFails()
    {
        var reporter = CreateReporter(() => throw new InvalidOperationException("bridge resolver failed"));

        Assert.DoesNotContain("OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS=", reporter.BuildCurrentPayload());
    }

    [Fact]
    public void MarkOpenTelemetryLoggerConfigured_PreventsBridgeLoggerProviderEndpointReporting()
    {
        var reporter = CreateReporter(() => ["http://bridge-collector:4318/v1/logs"]);

        reporter.MarkOpenTelemetryLoggerConfigured();

        Assert.DoesNotContain("OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS=", reporter.BuildCurrentPayload());
    }

    private static OtlpExporterOptions CreateHttpLogOptions(string endpoint)
    {
        return new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf,
            Endpoint = new Uri(endpoint)
        };
    }

    private static EffectiveConfigReporter CreateILoggerReporter()
    {
        var reporter = CreateReporter();
        reporter.MarkOpenTelemetryLoggerConfigured();
        return reporter;
    }

    private static EffectiveConfigReporter CreateReporter(
        Func<IReadOnlyList<string>?>? bridgeLogEndpointResolver = null)
    {
        return new EffectiveConfigReporter(bridgeLogEndpointResolver ?? (() => null));
    }
}
