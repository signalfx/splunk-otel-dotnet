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

using System.Text;
using OpenTelemetry.Exporter;
using OpenTelemetry.OpAmp.Client.Messages;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Model;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class EffectiveConfigReporterTests
{
    [Fact]
    public void CaptureLogExporterOptions_AddsLogsEndpointToPayload_ForILogger()
    {
        var reporter = CreateILoggerReporter();

        reporter.CaptureLogExporterOptions(CreateHttpLogOptions("http://logs-collector:4318/v1/logs"));

        var payload = reporter.BuildCurrentPayload();
        var body = GetBody(payload);
        Assert.Equal("environment", payload.FileName);
        Assert.Equal("text/plain; format=properties; vendor=splunk; v=1.0.0", payload.ContentType);
        Assert.Contains("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=http://logs-collector:4318/v1/logs", body);
    }

    [Fact]
    public void BuildCurrentPayload_UsesBridgeLoggerProviderEndpoints_WhenILoggerWasNotConfigured()
    {
        var reporter = CreateReporter(() => [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")]);

        Assert.Contains(
            "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=http://bridge-collector:4318/v1/logs",
            GetBody(reporter.BuildCurrentPayload()));
    }

    [Fact]
    public void BuildCurrentPayload_UsesBridgeLoggerProviderEndpoints_WhenOptionsHookRanWithoutILogger()
    {
        var reporter = CreateReporter(() => [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")]);

        reporter.CaptureLogExporterOptions(CreateHttpLogOptions("http://options-collector:4318/v1/logs"));

        var payload = GetBody(reporter.BuildCurrentPayload());
        Assert.Contains("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=http://bridge-collector:4318/v1/logs", payload);
        Assert.DoesNotContain("http://options-collector:4318/v1/logs", payload);
    }

    [Fact]
    public void CaptureLogExporterOptions_IgnoresEndpoint_WhenILoggerWasNotConfigured()
    {
        var reporter = CreateReporter();

        reporter.CaptureLogExporterOptions(CreateHttpLogOptions("http://options-collector:4318/v1/logs"));

        var payload = GetBody(reporter.BuildCurrentPayload());
        Assert.Contains("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=none", payload);
        Assert.DoesNotContain("http://options-collector:4318/v1/logs", payload);
    }

    [Fact]
    public void BuildCurrentPayload_UsesDefaultLogsEndpoint_WhenBridgeLoggerProviderHasNoOtlpEndpoints()
    {
        var reporter = CreateReporter(() => []);

        Assert.Contains(
            "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=none",
            GetBody(reporter.BuildCurrentPayload()));
    }

    [Fact]
    public void BuildCurrentPayload_UsesDefaultLogsEndpoint_WhenBridgeLoggerProviderCouldNotBeResolved()
    {
        var reporter = CreateReporter(() => null);

        Assert.Contains(
            "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=none",
            GetBody(reporter.BuildCurrentPayload()));
    }

    [Fact]
    public void BuildCurrentPayload_UsesDefaultLogsEndpoint_WhenBridgeLoggerProviderResolutionFails()
    {
        var reporter = CreateReporter(() => throw new InvalidOperationException("bridge resolver failed"));

        Assert.Contains(
            "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=none",
            GetBody(reporter.BuildCurrentPayload()));
    }

    [Fact]
    public void MarkOpenTelemetryLoggerConfigured_PreventsBridgeLoggerProviderEndpointReporting()
    {
        var reporter = CreateReporter(() => [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")]);

        reporter.MarkOpenTelemetryLoggerConfigured();

        var payload = GetBody(reporter.BuildCurrentPayload());
        Assert.Contains("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=none", payload);
        Assert.DoesNotContain("http://bridge-collector:4318/v1/logs", payload);
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
        Func<IReadOnlyList<EffectiveOtlpEndpoint>?>? bridgeLogEndpointResolver = null)
    {
        return new EffectiveConfigReporter(bridgeLogEndpointResolver ?? (() => null));
    }

    private static string GetBody(EffectiveConfigFile file)
    {
        return Encoding.UTF8.GetString(file.Content.ToArray());
    }
}
