// <copyright file="EffectiveConfigRecorderTests.cs" company="Splunk Inc.">
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

public class EffectiveConfigRecorderTests
{
    [Fact]
    public void CreateSnapshot_CapturesBridgeLoggerProviderEndpoints()
    {
        var endpoint = EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs");
        var recorder = CreateRecorder(() => [endpoint]);

        var snapshot = recorder.CreateSnapshot(EffectiveProfilerFeatures.None);

        Assert.Equal([endpoint], snapshot.LogEndpoints);
    }

    [Fact]
    public void CreateSnapshot_ReturnsNoSignalEndpoints_WhenSdkIsDisabled()
    {
        var recorder = new EffectiveConfigRecorder(
            CreateStaticSettings(),
            openTelemetrySdkDisabled: true,
            () => throw new InvalidOperationException("The bridge resolver should not be called."));

        var snapshot = recorder.CreateSnapshot(EffectiveProfilerFeatures.None);

        Assert.Empty(snapshot.TraceEndpoints);
        Assert.Empty(snapshot.MetricEndpoints);
        Assert.Empty(snapshot.LogEndpoints);
    }

    [Fact]
    public void ValidateCompatibility_PreservesCapturedStateAndAllowsLaterUpdates()
    {
        var beforePreflight = EffectiveOtlpEndpoint.Http("http://before-preflight:4318/v1/logs");
        var afterPreflight = EffectiveOtlpEndpoint.Http("http://after-preflight:4318/v1/logs");
        var recorder = CreateRecorder(() => null);
        recorder.MarkOpenTelemetryLoggerConfigured();
        Assert.True(recorder.CaptureLogExporterOptions(CreateLogExporterOptions(beforePreflight.Endpoint)));

        recorder.ValidateCompatibility();

        Assert.Equal(
            [beforePreflight],
            recorder.CreateSnapshot(EffectiveProfilerFeatures.None).LogEndpoints);

        Assert.True(recorder.CaptureLogExporterOptions(CreateLogExporterOptions(afterPreflight.Endpoint)));

        Assert.Equal(
            [beforePreflight, afterPreflight],
            recorder.CreateSnapshot(EffectiveProfilerFeatures.None).LogEndpoints);
    }

    private static EffectiveConfigRecorder CreateRecorder(
        Func<IReadOnlyList<EffectiveOtlpEndpoint>?> bridgeLogEndpointResolver)
    {
        return new EffectiveConfigRecorder(
            CreateStaticSettings(),
            openTelemetrySdkDisabled: false,
            bridgeLogEndpointResolver);
    }

    private static EffectiveConfigStaticSettings CreateStaticSettings()
    {
        return new EffectiveConfigStaticSettings(
            new PluginSettings(new NameValueConfigurationSource(new NameValueCollection())));
    }

    private static OtlpExporterOptions CreateLogExporterOptions(string endpoint)
    {
        return new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf,
            Endpoint = new Uri(endpoint)
        };
    }
}
