// <copyright file="SnapshotSelectingProcessor.cs" company="Splunk Inc.">
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

using System.Collections.Concurrent;
using System.Diagnostics;
using OpenTelemetry;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Snapshots;

internal class SnapshotSelectingProcessor : BaseProcessor<Activity>
{
    private const int SnapshotLocalRootLimit = 50;
    private static readonly int CleanUpPeriodMs = GetCleanUpPeriodMs();
    private static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromMinutes(15);
    private static readonly ILogger Log = new Logger();

    private readonly SnapshotFilter _snapshotFilter;
    private readonly ISnapshotSelector _snapshotSelector;
    private readonly ConcurrentDictionary<(ActivitySpanId, ActivityTraceId), DateTimeOffset> _localRootSpans = new();
    private readonly Timer _timer;

    public SnapshotSelectingProcessor(SnapshotFilter snapshotFilter, ISnapshotSelector snapshotSelector)
    {
        _snapshotFilter = snapshotFilter;
        _snapshotSelector = snapshotSelector;
        _timer = new Timer(Clean, null, CleanUpPeriodMs, CleanUpPeriodMs);
    }

    public override void OnStart(Activity data)
    {
        if (!data.IsLocalRoot())
        {
            return;
        }

        if (!_snapshotSelector.Select(data.Context))
        {
            return;
        }

        if (_localRootSpans.Count > SnapshotLocalRootLimit)
        {
            Log.Warning("Too many traces selected for snapshotting.");
            return;
        }

        data.MarkLoud();
        _snapshotFilter.Add(data.TraceId);
        var cacheKey = (data.SpanId, data.TraceId);
        if (!_localRootSpans.TryAdd(cacheKey, DateTimeOffset.UtcNow + DefaultTimeToLive))
        {
            Log.Warning("Local root span already registered.");
        }
    }

    public override void OnEnd(Activity data)
    {
        var cacheKey = (data.SpanId, data.TraceId);
        if (_localRootSpans.TryRemove(cacheKey, out _))
        {
            _snapshotFilter.Remove(data.TraceId);
        }
    }

    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        _timer.Dispose();
        return true;
    }

    private static int GetCleanUpPeriodMs()
    {
        return Convert.ToInt32(DefaultTimeToLive.TotalMilliseconds);
    }

    private void Clean(object? state)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kvp in _localRootSpans)
        {
            if (now > kvp.Value)
            {
                _localRootSpans.TryRemove(kvp.Key, out _);
                _snapshotFilter.Remove(kvp.Key.Item2);
            }
        }
    }
}
