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

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class SnapshotSelectingProcessorTests : IDisposable
{
    private readonly ActivitySource _source = new("Test.Snapshots");
    private readonly ActivityListener _listener;

    public SnapshotSelectingProcessorTests()
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;

        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [Fact]
    public void LocalRoot_SelectedTrace_IsMarkedLoud()
    {
        var filter = new SnapshotFilter(null, null);
        var processor = new SnapshotSelectingProcessor(filter, new AlwaysSelectSelector());

        using var root = _source.StartActivity("root");
        Assert.NotNull(root);

        processor.OnStart(root);

        Assert.Equal("true", root.GetTagItem(SnapshotConstants.SplunkSnapshotProfilingAttributeName));
    }

    [Fact]
    public void LocalRoot_NotSelectedTrace_IsNotMarkedLoud()
    {
        var filter = new SnapshotFilter(null, null);
        var processor = new SnapshotSelectingProcessor(filter, new NeverSelectSelector());

        using var root = _source.StartActivity("root");
        Assert.NotNull(root);

        processor.OnStart(root);

        Assert.Null(root.GetTagItem(SnapshotConstants.SplunkSnapshotProfilingAttributeName));
    }

    [Fact]
    public void ChildSpan_PropagatesTag_WhenTraceIsSelected()
    {
        var filter = new SnapshotFilter(null, null);
        var processor = new SnapshotSelectingProcessor(filter, new AlwaysSelectSelector());

        using var root = _source.StartActivity("root");
        Assert.NotNull(root);
        processor.OnStart(root);

        using var child = _source.StartActivity("child");
        Assert.NotNull(child);
        Assert.NotNull(child.Parent);
        Assert.Null(child.GetTagItem(SnapshotConstants.SplunkSnapshotProfilingAttributeName));
        processor.OnStart(child);

        Assert.Equal("true", child.GetTagItem(SnapshotConstants.SplunkSnapshotProfilingAttributeName));
    }

    [Fact]
    public void ChildSpan_DoesNotGetTag_WhenTraceIsNotSelected()
    {
        var filter = new SnapshotFilter(null, null);
        var processor = new SnapshotSelectingProcessor(filter, new NeverSelectSelector());

        using var root = _source.StartActivity("root");
        Assert.NotNull(root);
        processor.OnStart(root);

        using var child = _source.StartActivity("child");
        Assert.NotNull(child);
        processor.OnStart(child);

        Assert.Null(child.GetTagItem(SnapshotConstants.SplunkSnapshotProfilingAttributeName));
    }

    [Fact]
    public void ChildSpan_DoesNotGetTag_AfterLocalRootEnds()
    {
        var filter = new SnapshotFilter(null, null);
        var processor = new SnapshotSelectingProcessor(filter, new AlwaysSelectSelector());

        using var root = _source.StartActivity("root");
        Assert.NotNull(root);
        processor.OnStart(root);
        processor.OnEnd(root);

        using var lateChild = _source.StartActivity("late-child");
        Assert.NotNull(lateChild);
        processor.OnStart(lateChild);

        Assert.Null(lateChild.GetTagItem(SnapshotConstants.SplunkSnapshotProfilingAttributeName));
    }

    public void Dispose()
    {
        _listener.Dispose();
        _source.Dispose();
    }

    private class AlwaysSelectSelector : ISnapshotSelector
    {
        public bool Select(ActivityContext context) => true;
    }

    private class NeverSelectSelector : ISnapshotSelector
    {
        public bool Select(ActivityContext context) => false;
    }
}
