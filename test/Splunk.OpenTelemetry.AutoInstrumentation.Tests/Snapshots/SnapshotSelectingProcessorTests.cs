// <copyright file="SnapshotSelectingProcessorTests.cs" company="Splunk Inc.">
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

using System.Diagnostics;
using Splunk.OpenTelemetry.AutoInstrumentation.Snapshots;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests.Snapshots;

public class SnapshotSelectingProcessorTests
{
    [Fact]
    public void SelectedEntrySpan_IsMarkedAndStartsAndStopsNativeSampling()
    {
        var starts = 0;
        var stops = 0;
        var filter = new SnapshotFilter(
            _ => starts++,
            _ => stops++);
        using var processor = new SnapshotSelectingProcessor(filter, new FixedSnapshotSelector(true));
        using var activity = CreateEntryActivity();

        processor.OnStart(activity);

        Assert.Equal(1, starts);
        Assert.True(activity.GetTagItem(SnapshotConstants.SplunkSnapshotProfilingAttributeName) is true);

        processor.OnEnd(activity);

        Assert.Equal(1, stops);
    }

    [Fact]
    public void ConcurrentSelectedEntries_RespectLimitAndReleasedSlotsCanBeReused()
    {
        const int activityCount = 100;
        const int expectedLimit = 50;
        var starts = 0;
        var stops = 0;
        var filter = new SnapshotFilter(
            _ => Interlocked.Increment(ref starts),
            _ => Interlocked.Increment(ref stops));
        using var processor = new SnapshotSelectingProcessor(filter, new FixedSnapshotSelector(true));
        var activities = Enumerable.Range(0, activityCount)
            .Select(_ => CreateEntryActivity())
            .ToArray();

        try
        {
            Parallel.ForEach(activities, processor.OnStart);

            Assert.Equal(expectedLimit, starts);
            Assert.Equal(
                expectedLimit,
                activities.Count(
                    activity => activity.GetTagItem(SnapshotConstants.SplunkSnapshotProfilingAttributeName) is true));

            foreach (var activity in activities)
            {
                processor.OnEnd(activity);
            }

            Assert.Equal(expectedLimit, stops);

            using var replacement = CreateEntryActivity();
            processor.OnStart(replacement);
            processor.OnEnd(replacement);

            Assert.Equal(expectedLimit + 1, starts);
            Assert.Equal(expectedLimit + 1, stops);
        }
        finally
        {
            foreach (var activity in activities)
            {
                activity.Dispose();
            }
        }
    }

    [Fact]
    public void ExpiredEntry_IsStoppedOnce_WhenCleanupPrecedesOnEnd()
    {
        var stops = 0;
        var filter = new SnapshotFilter(
            _ => { },
            _ => stops++);
        using var processor = new SnapshotSelectingProcessor(filter, new FixedSnapshotSelector(true));
        using var activity = CreateEntryActivity();

        processor.OnStart(activity);
        var now = DateTimeOffset.UtcNow;

        processor.Clean(now.AddMinutes(14));
        Assert.Equal(0, stops);

        processor.Clean(now.AddMinutes(16));
        processor.OnEnd(activity);

        Assert.Equal(1, stops);
    }

    private static Activity CreateEntryActivity()
    {
        var traceId = ActivityTraceId.CreateRandom();
        var parentSpanId = ActivitySpanId.CreateRandom();
        var activity = new Activity("entry");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.SetParentId($"00-{traceId.ToHexString()}-{parentSpanId.ToHexString()}-01");
        activity.Start();
        return activity;
    }

    private sealed class FixedSnapshotSelector : ISnapshotSelector
    {
        private readonly bool _selected;

        public FixedSnapshotSelector(bool selected)
        {
            _selected = selected;
        }

        public bool Select(ActivityContext context)
        {
            return _selected;
        }
    }
}
