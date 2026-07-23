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
using System.Text;
using OpenTelemetry.Exporter;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class EffectiveConfigReporterTests
{
    [Fact]
    public void BuildCurrentPayload_UsesUpdatedProfilerRuntimeState()
    {
        var recorder = new EffectiveConfigRecorder(
            new EffectiveConfigStaticSettings(
                new PluginSettings(new NameValueConfigurationSource(new NameValueCollection()))),
            openTelemetrySdkDisabled: true);
        var reporter = EffectiveConfigReporter.CreateValidated(recorder, EffectiveProfilerFeatures.None);

        reporter.UpdateProfilerState(EffectiveProfilerFeatures.Cpu, cpuProfilerCallStackInterval: 1234);

        var body = Encoding.UTF8.GetString(reporter.BuildCurrentPayload().Content.ToArray());
        Assert.Contains("SPLUNK_PROFILER_ENABLED=true", body, StringComparison.Ordinal);
        Assert.Contains("SPLUNK_PROFILER_CALL_STACK_INTERVAL=1234", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildCurrentPayload_CreatesEffectiveConfigFileFromCurrentRecorderState()
    {
        const string logsEndpoint = "http://logs-collector:4318/v1/logs";
        var recorder = new EffectiveConfigRecorder(
            new EffectiveConfigStaticSettings(
                new PluginSettings(new NameValueConfigurationSource(new NameValueCollection()))),
            openTelemetrySdkDisabled: false,
            () => null);
        var reporter = EffectiveConfigReporter.CreateValidated(recorder, EffectiveProfilerFeatures.None);
        recorder.MarkOpenTelemetryLoggerConfigured();
        Assert.True(recorder.CaptureLogExporterOptions(new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf,
            Endpoint = new Uri(logsEndpoint)
        }));

        var payload = reporter.BuildCurrentPayload();

        Assert.Equal("environment", payload.FileName);
        Assert.Equal("text/plain; format=properties; vendor=splunk; v=1.0.0", payload.ContentType);
        Assert.Contains(
            $"OTEL_EXPORTER_OTLP_LOGS_ENDPOINT={logsEndpoint}",
            Encoding.UTF8.GetString(payload.Content.ToArray()));
    }
}
