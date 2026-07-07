// <copyright file="EffectiveLogEndpointTrackerTests.cs" company="Splunk Inc.">
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

using OpenTelemetry;
using OpenTelemetry.Exporter;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class EffectiveLogEndpointTrackerTests
{
    [Fact]
    public void CaptureLogExporterOptions_AddsLogsEndpoint_ForILogger()
    {
        var tracker = CreateILoggerTracker();

        Assert.True(tracker.CaptureLogExporterOptions(CreateHttpLogOptions("http://logs-collector:4318/v1/logs")));
        Assert.Equal(
            [EffectiveOtlpEndpoint.Http("http://logs-collector:4318/v1/logs")],
            tracker.GetEndpoints());
    }

    [Fact]
    public void CaptureLogExporterOptions_ReturnsFalse_WhenEndpointAlreadyExists()
    {
        var tracker = CreateILoggerTracker();
        var options = CreateHttpLogOptions("http://logs-collector:4318/v1/logs");

        Assert.True(tracker.CaptureLogExporterOptions(options));
        Assert.False(tracker.CaptureLogExporterOptions(options));
        Assert.Single(tracker.GetEndpoints());
    }

    [Fact]
    public void CaptureLogExporterOptions_PreservesEndpointsWithDifferentExporterTypes()
    {
        var tracker = CreateILoggerTracker();

        Assert.True(tracker.CaptureLogExporterOptions(CreateHttpLogOptions("http://logs-collector:4318/v1/logs")));
        Assert.True(tracker.CaptureLogExporterOptions(CreateGrpcLogOptions("http://logs-collector:4318")));

        Assert.Collection(
            tracker.GetEndpoints(),
            endpoint => Assert.Equal(EffectiveOtlpExporterType.HttpProtobuf, endpoint.ExporterType),
            endpoint => Assert.Equal(EffectiveOtlpExporterType.Grpc, endpoint.ExporterType));
    }

    [Fact]
    public void CaptureLogExporterOptions_ReportsBatchWhenSdkIgnoresSimpleProcessorType()
    {
        var tracker = CreateILoggerTracker();

        Assert.True(tracker.CaptureLogExporterOptions(
            CreateHttpLogOptions("http://logs-collector:4318/v1/logs", ExportProcessorType.Simple)));

        Assert.Equal(
            [EffectiveOtlpEndpoint.Http("http://logs-collector:4318/v1/logs", EffectiveOtlpPipelineType.Batch)],
            tracker.GetEndpoints());
    }

    [Fact]
    public void CaptureLogExporterOptions_KeepsFailureAfterLaterCaptureSucceeds()
    {
        var tracker = CreateILoggerTracker();
        var unsupportedOptions = CreateHttpLogOptions("http://unsupported-collector:4318/v1/logs");
        unsupportedOptions.Protocol = (OtlpExportProtocol)42;

        Assert.False(tracker.CaptureLogExporterOptions(unsupportedOptions));
        Assert.True(tracker.CaptureLogExporterOptions(
            CreateHttpLogOptions("http://logs-collector:4318/v1/logs")));

        Assert.Throws<InvalidOperationException>(() => tracker.GetEndpoints());
    }

    [Fact]
    public void GetEndpoints_SetsBridgeLoggerProviderEndpoints_WhenILoggerWasNotConfigured()
    {
        var tracker = CreateTracker(() => [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")]);

        Assert.Equal(
            [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")],
            tracker.GetEndpoints());
    }

    [Fact]
    public void GetEndpoints_SetsBridgeLoggerProviderEndpoints_WhenOptionsHookRanWithoutILogger()
    {
        var tracker = CreateTracker(() => [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")]);

        Assert.False(tracker.CaptureLogExporterOptions(CreateHttpLogOptions("http://options-collector:4318/v1/logs")));
        Assert.Equal(
            [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")],
            tracker.GetEndpoints());
    }

    [Fact]
    public void CaptureLogExporterOptions_IgnoresEndpoint_WhenILoggerWasNotConfigured()
    {
        var tracker = CreateTracker();

        Assert.False(tracker.CaptureLogExporterOptions(CreateHttpLogOptions("http://options-collector:4318/v1/logs")));
        Assert.Empty(tracker.GetEndpoints());
    }

    [Fact]
    public void GetEndpoints_ReturnsEmpty_WhenBridgeLoggerProviderHasNoOtlpEndpoints()
    {
        var tracker = CreateTracker(() => []);

        Assert.Empty(tracker.GetEndpoints());
    }

    [Fact]
    public void GetEndpoints_ReturnsEmpty_WhenBridgeLoggerProviderCouldNotBeResolved()
    {
        var tracker = CreateTracker(() => null);

        Assert.Empty(tracker.GetEndpoints());
    }

    [Fact]
    public void GetEndpoints_ClearsBridgeEndpoints_WhenResolverReturnsNull()
    {
        IReadOnlyList<EffectiveOtlpEndpoint>? bridgeEndpoints =
            [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")];
        var tracker = CreateTracker(() => bridgeEndpoints);

        Assert.Single(tracker.GetEndpoints());

        bridgeEndpoints = null;

        Assert.Empty(tracker.GetEndpoints());
    }

    [Fact]
    public void GetEndpoints_Throws_WhenBridgeLoggerProviderResolutionFails()
    {
        var tracker = CreateTracker(() => throw new InvalidOperationException("bridge resolver failed"));

        var exception = Assert.Throws<InvalidOperationException>(() => tracker.GetEndpoints());
        Assert.Equal("bridge resolver failed", exception.Message);
    }

    [Fact]
    public void MarkOpenTelemetryLoggerConfigured_ClearsBridgeEndpointsAndPreventsBridgeLoggerProviderEndpointReporting()
    {
        var tracker = CreateTracker(() => [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")]);

        Assert.Equal(
            [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")],
            tracker.GetEndpoints());

        Assert.True(tracker.MarkOpenTelemetryLoggerConfigured());

        Assert.Empty(tracker.GetEndpoints());
        Assert.True(tracker.CaptureLogExporterOptions(
            CreateHttpLogOptions("http://logs-collector:4318/v1/logs")));
        Assert.False(tracker.MarkOpenTelemetryLoggerConfigured());
        Assert.Single(tracker.GetEndpoints());
    }

    private static OtlpExporterOptions CreateHttpLogOptions(
        string endpoint,
        ExportProcessorType processorType = ExportProcessorType.Batch)
    {
        return new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf,
            Endpoint = new Uri(endpoint),
            ExportProcessorType = processorType
        };
    }

    private static OtlpExporterOptions CreateGrpcLogOptions(string endpoint)
    {
        return new OtlpExporterOptions
        {
#pragma warning disable CS0618 // OtlpExportProtocol.Grpc is obsolete but still supported by the SDK.
            Protocol = OtlpExportProtocol.Grpc,
#pragma warning restore CS0618
            Endpoint = new Uri(endpoint)
        };
    }

    private static EffectiveLogEndpointTracker CreateILoggerTracker()
    {
        var tracker = CreateTracker();
        tracker.MarkOpenTelemetryLoggerConfigured();
        return tracker;
    }

    private static EffectiveLogEndpointTracker CreateTracker(
        Func<IReadOnlyList<EffectiveOtlpEndpoint>?>? bridgeLogEndpointResolver = null)
    {
        return new EffectiveLogEndpointTracker(bridgeLogEndpointResolver ?? (() => null));
    }
}
