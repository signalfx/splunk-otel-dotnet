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

using OpenTelemetry.Exporter;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class EffectiveConfigReporterTests
{
    [Fact]
    public void CaptureLogEndpoint_AddsLogsEndpointToPayload_ForILogger()
    {
        var reporter = CreateILoggerReporter();

        reporter.CaptureLogEndpoint(CreateHttpLogOptions("http://logs-collector:4318/v1/logs"));

        Assert.Contains("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=\"http://logs-collector:4318/v1/logs\"", reporter.BuildCurrentPayload());
    }

    [Fact]
    public void CaptureLogEndpoint_AccumulatesMultipleConfiguredOtlpLogExporters_ForILogger()
    {
        var reporter = CreateILoggerReporter();

        reporter.CaptureLogEndpoint(CreateHttpLogOptions("http://logs-collector-1:4318/v1/logs"));
        reporter.CaptureLogEndpoint(CreateHttpLogOptions("http://logs-collector-2:4318/v1/logs"));

        Assert.Contains(
            "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=\"http://logs-collector-1:4318/v1/logs\",\"http://logs-collector-2:4318/v1/logs\"",
            reporter.BuildCurrentPayload());
    }

    [Fact]
    public void BuildCurrentPayload_UsesBridgeLoggerProviderEndpoints_WhenILoggerWasNotConfigured()
    {
        var reporter = CreateReporter(() => ["http://bridge-collector:4318/v1/logs"]);

        Assert.Contains(
            "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=\"http://bridge-collector:4318/v1/logs\"",
            reporter.BuildCurrentPayload());
    }

    [Fact]
    public void BuildCurrentPayload_UsesBridgeLoggerProviderEndpoints_WhenLogEndpointWasCapturedEarlier()
    {
        var reporter = CreateReporter(() => ["http://bridge-collector:4318/v1/logs"]);

        reporter.CaptureLogEndpoint(CreateHttpLogOptions("http://options-collector:4318/v1/logs"));

        var payload = reporter.BuildCurrentPayload();
        Assert.Contains("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=\"http://bridge-collector:4318/v1/logs\"", payload);
        Assert.DoesNotContain("http://options-collector:4318/v1/logs", payload);
    }

    [Fact]
    public void BuildCurrentPayload_RemovesEarlierLogEndpoint_WhenBridgeLoggerProviderHasNoOtlpEndpoints()
    {
        var reporter = CreateReporter(() => []);

        reporter.CaptureLogEndpoint(CreateHttpLogOptions("http://options-collector:4318/v1/logs"));

        Assert.DoesNotContain("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=", reporter.BuildCurrentPayload());
    }

    [Fact]
    public void BuildCurrentPayload_RemovesEarlierLogEndpoint_WhenBridgeLoggerProviderCouldNotBeResolved()
    {
        var reporter = CreateReporter(() => null);

        reporter.CaptureLogEndpoint(CreateHttpLogOptions("http://options-collector:4318/v1/logs"));

        Assert.DoesNotContain("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=", reporter.BuildCurrentPayload());
    }

    [Fact]
    public void BuildCurrentPayload_RemovesEarlierLogEndpoint_WhenBridgeLoggerProviderResolutionFails()
    {
        var reporter = CreateReporter(() => throw new InvalidOperationException("bridge resolver failed"));

        reporter.CaptureLogEndpoint(CreateHttpLogOptions("http://options-collector:4318/v1/logs"));

        Assert.DoesNotContain("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=", reporter.BuildCurrentPayload());
    }

    [Fact]
    public void CaptureOpenTelemetryLoggerOptions_PreventsBridgeLoggerProviderEndpointReporting()
    {
        var reporter = CreateReporter(() => ["http://bridge-collector:4318/v1/logs"]);

        reporter.CaptureOpenTelemetryLoggerOptions();

        Assert.DoesNotContain("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=", reporter.BuildCurrentPayload());
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
        reporter.CaptureOpenTelemetryLoggerOptions();
        return reporter;
    }

    private static EffectiveConfigReporter CreateReporter(
        Func<IReadOnlyList<string>?>? bridgeLogEndpointResolver = null)
    {
        return new EffectiveConfigReporter(bridgeLogEndpointResolver ?? (() => null));
    }
}
