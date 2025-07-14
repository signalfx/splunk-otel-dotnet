// <copyright file="SnapshotVolumePropagatorTests.cs" company="Splunk Inc.">
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

#if NET

using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Splunk.OpenTelemetry.AutoInstrumentation.Snapshots;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests.Snapshots
{
    public class SnapshotVolumePropagatorTests
    {
        [Fact]
        public void IfVolumeWasDecided_FollowTheDecision()
        {
            var propagator = new SnapshotVolumePropagator(new ProbabilisticVolumeSelector(0.0));

            var dict = new Dictionary<string, object>();

            var extractedContext = propagator.Extract(
                new PropagationContext(default, Baggage.Create(new Dictionary<string, string> { ["splunk.trace.snapshot.volume"] = "highest" })),
                dict,
                (_, _) => throw new InvalidOperationException());

            Assert.Equal("highest", extractedContext.Baggage.GetBaggage("splunk.trace.snapshot.volume"));
        }

        [Fact]
        public void IgnoreNonRootSpans()
        {
            var propagator = new SnapshotVolumePropagator(new ProbabilisticVolumeSelector(1.0));

            var dict = new Dictionary<string, object>();

            var activityContext = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
            var extractedContext = propagator.Extract(
                new PropagationContext(activityContext, Baggage.Create()),
                dict,
                (_, _) => throw new InvalidOperationException());

            Assert.Empty(extractedContext.Baggage.GetBaggage());
        }

        [Theory]
        [InlineData(1.0, "highest")]
        [InlineData(0.0, "off")]
        public void IfVolumeWasNotDecided_MakeADecision(double ratio, string expectedValue)
        {
            var propagator = new SnapshotVolumePropagator(new ProbabilisticVolumeSelector(ratio));

            var dict = new Dictionary<string, object>();

            var extractedContext = propagator.Extract(
                new PropagationContext(default, default),
                dict,
                (_, _) => throw new InvalidOperationException());

            Assert.Equal(expectedValue, extractedContext.Baggage.GetBaggage("splunk.trace.snapshot.volume"));
        }
    }
}
#endif
