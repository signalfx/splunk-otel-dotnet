// <copyright file="EffectiveProviderEndpointTrackerTests.cs" company="Splunk Inc.">
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

using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class EffectiveProviderEndpointTrackerTests
{
    [Fact]
    public void Capture_ReplacesEndpointsAndReportsWhetherTheyChanged()
    {
        IReadOnlyList<EffectiveOtlpEndpoint> resolvedEndpoints =
            [EffectiveOtlpEndpoint.Http("http://first-collector:4318/v1/traces")];
        var tracker = CreateTracker(_ => resolvedEndpoints);

        var provider = new object();
        Assert.True(tracker.Capture(provider));
        Assert.False(tracker.Capture(provider));

        resolvedEndpoints = [EffectiveOtlpEndpoint.Http("http://second-collector:4318/v1/traces")];

        Assert.True(tracker.Capture(provider));
        Assert.Equal(resolvedEndpoints, tracker.GetEndpoints());
    }

    [Fact]
    public void Capture_MarksTrackerUnhealthyUntilResolutionSucceeds()
    {
        var resolutionFails = false;
        var tracker = CreateTracker(
            _ => resolutionFails
                ? throw new InvalidOperationException("resolution failed")
                : [EffectiveOtlpEndpoint.Http("http://collector:4318/v1/traces")]);

        var provider = new object();
        Assert.True(tracker.Capture(provider));

        resolutionFails = true;

        Assert.False(tracker.Capture(provider));
        Assert.Throws<InvalidOperationException>(() => tracker.GetEndpoints());
        Assert.Throws<InvalidOperationException>(() => tracker.ValidateState());
        Assert.False(tracker.Capture(provider));

        resolutionFails = false;

        Assert.False(tracker.Capture(provider));
        tracker.ValidateState();
        Assert.Single(tracker.GetEndpoints());
    }

    [Fact]
    public void GetEndpoints_ReturnsImmutableSnapshot()
    {
        IReadOnlyList<EffectiveOtlpEndpoint> resolvedEndpoints =
            [EffectiveOtlpEndpoint.Http("http://first-collector:4318/v1/traces")];
        var tracker = CreateTracker(_ => resolvedEndpoints);
        var provider = new object();
        tracker.Capture(provider);
        var snapshot = tracker.GetEndpoints();

        resolvedEndpoints = [EffectiveOtlpEndpoint.Http("http://second-collector:4318/v1/traces")];
        tracker.Capture(provider);

        Assert.Equal(
            [EffectiveOtlpEndpoint.Http("http://first-collector:4318/v1/traces")],
            snapshot);
    }

    [Fact]
    public void Capture_RejectsEndpointCountOverLimitWithoutRetainingIt()
    {
        IReadOnlyList<EffectiveOtlpEndpoint> resolvedEndpoints = Enumerable.Range(
                0,
                EffectiveConfigLimits.MaxEndpointCount + 1)
            .Select(index => EffectiveOtlpEndpoint.Http($"http://collector-{index}:4318/v1/traces"))
            .ToArray();
        var tracker = CreateTracker(_ => resolvedEndpoints);
        var provider = new object();

        Assert.False(tracker.Capture(provider));
        Assert.Throws<InvalidOperationException>(() => tracker.GetEndpoints());

        resolvedEndpoints = [EffectiveOtlpEndpoint.Http("http://collector:4318/v1/traces")];

        Assert.True(tracker.Capture(provider));
        Assert.Single(tracker.GetEndpoints());
    }

    private static EffectiveProviderEndpointTracker<object> CreateTracker(
        Func<object, IReadOnlyList<EffectiveOtlpEndpoint>> resolver)
    {
        return new EffectiveProviderEndpointTracker<object>(resolver);
    }
}
