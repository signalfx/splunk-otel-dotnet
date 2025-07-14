// <copyright file="AsyncSuspensionTrackerTests.cs" company="Splunk Inc.">
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
using Splunk.OpenTelemetry.AutoInstrumentation.Snapshots;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests.Snapshots
{
    public class AsyncSuspensionTrackerTests
    {
        [Fact]
        public void IfActivityIsNotFromSubscribedSource_ShouldBeIgnored()
        {
            var defaultBaggage = Baggage.Current;
            var startCalled = false;

            try
            {
                Baggage.Current = Baggage.Create(new Dictionary<string, string> { ["splunk.trace.snapshot.volume"] = "highest" });

                using var suspensionTrackerListener = CreateTestListener("Subscribed");
                using var otherListener = CreateTestListener("NotSubscribed");

                var sampler = new CurrentThreadSampler(() => startCalled = true, () => { });
                // ReSharper disable once AccessToDisposedClosure
                using var tracker = new AsyncSuspensionTracker(sampler, () => suspensionTrackerListener);

                using var source = new ActivitySource("NotSubscribed");
                ActivitySource.AddActivityListener(suspensionTrackerListener);

                // Add another so that activity is created
                ActivitySource.AddActivityListener(otherListener);

                using var activity = source.StartActivity();

                Assert.NotNull(activity);
                Assert.False(startCalled);
            }
            finally
            {
                // restore defaults
                Baggage.Current = defaultBaggage;
            }
        }

        [Fact]
        public void IfActivityIsFromSubscribedSource_ShouldBeSampled()
        {
            var defaultBaggage = Baggage.Current;

            var startCalled = false;
            var stopCalled = false;

            try
            {
                Baggage.Current = Baggage.Create(new Dictionary<string, string> { ["splunk.trace.snapshot.volume"] = "highest" });

                using var suspensionTrackerListener = CreateTestListener("Subscribed");

                var sampler = new CurrentThreadSampler(() => startCalled = true, () => { stopCalled = true; });
                // ReSharper disable once AccessToDisposedClosure
                using var tracker = new AsyncSuspensionTracker(sampler, () => suspensionTrackerListener);

                using var source = new ActivitySource("Subscribed");
                ActivitySource.AddActivityListener(suspensionTrackerListener);

                var activity = source.StartActivity();

                Assert.NotNull(activity);
                Assert.True(startCalled);
                Assert.False(stopCalled);

                activity.Stop();
                Assert.True(stopCalled);
            }
            finally
            {
                // restore defaults
                Baggage.Current = defaultBaggage;
            }
        }

        [Fact]
        public void IfActivityIsFromSubscribedSource_ButVolumeIsNotConsideredLoud_ShouldNotBeSampled()
        {
            var defaultBaggage = Baggage.Current;

            var startCalled = false;

            try
            {
                Baggage.Current = Baggage.Create(new Dictionary<string, string> { ["splunk.trace.snapshot.volume"] = "off" });

                using var suspensionTrackerListener = CreateTestListener("Subscribed");

                // ReSharper disable once AccessToDisposedClosure
                var sampler = new CurrentThreadSampler(() => startCalled = true, () => { });
                using var tracker = new AsyncSuspensionTracker(sampler, () => suspensionTrackerListener);

                using var source = new ActivitySource("Subscribed");
                ActivitySource.AddActivityListener(suspensionTrackerListener);

                using var activity = source.StartActivity();

                Assert.NotNull(activity);
                Assert.False(startCalled);
            }
            finally
            {
                // restore defaults
                Baggage.Current = defaultBaggage;
            }
        }

        private static ActivityListener CreateTestListener(string subscribedSourceName)
        {
            return new ActivityListener
            {
                ShouldListenTo = activitySource => activitySource.Name == subscribedSourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                    ActivitySamplingResult.AllDataAndRecorded
            };
        }
    }
}
#endif
