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

using OpenTelemetry.Exporter;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class EffectiveLogEndpointTrackerTests
{
    [Fact]
    public void CaptureLogExporterOptions_AddsLogsEndpoint_ForILogger()
    {
        var state = new EffectiveConfigState();
        var tracker = CreateILoggerTracker(state);

        Assert.True(tracker.CaptureLogExporterOptions(CreateHttpLogOptions("http://logs-collector:4318/v1/logs")));
        Assert.Equal(
            [EffectiveOtlpEndpoint.Http("http://logs-collector:4318/v1/logs")],
            state.CreateSnapshot().LogEndpoints);
    }

    [Fact]
    public void CaptureBridgeLogEndpointsIfNeeded_SetsBridgeLoggerProviderEndpoints_WhenILoggerWasNotConfigured()
    {
        var state = new EffectiveConfigState();
        var tracker = CreateTracker(state, () => [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")]);

        tracker.CaptureBridgeLogEndpointsIfNeeded();

        Assert.Equal(
            [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")],
            state.CreateSnapshot().LogEndpoints);
    }

    [Fact]
    public void CaptureBridgeLogEndpointsIfNeeded_SetsBridgeLoggerProviderEndpoints_WhenOptionsHookRanWithoutILogger()
    {
        var state = new EffectiveConfigState();
        var tracker = CreateTracker(state, () => [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")]);

        Assert.False(tracker.CaptureLogExporterOptions(CreateHttpLogOptions("http://options-collector:4318/v1/logs")));
        tracker.CaptureBridgeLogEndpointsIfNeeded();

        Assert.Equal(
            [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")],
            state.CreateSnapshot().LogEndpoints);
    }

    [Fact]
    public void CaptureLogExporterOptions_IgnoresEndpoint_WhenILoggerWasNotConfigured()
    {
        var state = new EffectiveConfigState();
        var tracker = CreateTracker(state);

        Assert.False(tracker.CaptureLogExporterOptions(CreateHttpLogOptions("http://options-collector:4318/v1/logs")));
        tracker.CaptureBridgeLogEndpointsIfNeeded();

        Assert.Empty(state.CreateSnapshot().LogEndpoints);
    }

    [Fact]
    public void CaptureBridgeLogEndpointsIfNeeded_LeavesLogEndpointsEmpty_WhenBridgeLoggerProviderHasNoOtlpEndpoints()
    {
        var state = new EffectiveConfigState();
        var tracker = CreateTracker(state, () => []);

        tracker.CaptureBridgeLogEndpointsIfNeeded();

        Assert.Empty(state.CreateSnapshot().LogEndpoints);
    }

    [Fact]
    public void CaptureBridgeLogEndpointsIfNeeded_LeavesLogEndpointsEmpty_WhenBridgeLoggerProviderCouldNotBeResolved()
    {
        var state = new EffectiveConfigState();
        var tracker = CreateTracker(state, () => null);

        tracker.CaptureBridgeLogEndpointsIfNeeded();

        Assert.Empty(state.CreateSnapshot().LogEndpoints);
    }

    [Fact]
    public void CaptureBridgeLogEndpointsIfNeeded_LeavesLogEndpointsEmpty_WhenBridgeLoggerProviderResolutionFails()
    {
        var state = new EffectiveConfigState();
        var tracker = CreateTracker(state, () => throw new InvalidOperationException("bridge resolver failed"));

        tracker.CaptureBridgeLogEndpointsIfNeeded();

        Assert.Empty(state.CreateSnapshot().LogEndpoints);
    }

    [Fact]
    public void MarkOpenTelemetryLoggerConfigured_ClearsBridgeEndpointsAndPreventsBridgeLoggerProviderEndpointReporting()
    {
        var state = new EffectiveConfigState();
        var tracker = CreateTracker(state, () => [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")]);

        tracker.CaptureBridgeLogEndpointsIfNeeded();
        Assert.Equal(
            [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")],
            state.CreateSnapshot().LogEndpoints);

        Assert.True(tracker.MarkOpenTelemetryLoggerConfigured());
        tracker.CaptureBridgeLogEndpointsIfNeeded();

        Assert.Empty(state.CreateSnapshot().LogEndpoints);
    }

    private static OtlpExporterOptions CreateHttpLogOptions(string endpoint)
    {
        return new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf,
            Endpoint = new Uri(endpoint)
        };
    }

    private static EffectiveLogEndpointTracker CreateILoggerTracker(EffectiveConfigState state)
    {
        var tracker = CreateTracker(state);
        tracker.MarkOpenTelemetryLoggerConfigured();
        return tracker;
    }

    private static EffectiveLogEndpointTracker CreateTracker(
        EffectiveConfigState state,
        Func<IReadOnlyList<EffectiveOtlpEndpoint>?>? bridgeLogEndpointResolver = null)
    {
        return new EffectiveLogEndpointTracker(state, bridgeLogEndpointResolver ?? (() => null));
    }
}
