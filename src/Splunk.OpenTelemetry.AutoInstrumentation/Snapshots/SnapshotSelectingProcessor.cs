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

using System.Diagnostics;
using OpenTelemetry;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Snapshots;

internal class SnapshotSelectingProcessor : BaseProcessor<Activity>
{
    private const int SnapshotLocalRootLimit = 50;
    private static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromMinutes(15);
    private static readonly ILogger Log = new Logger();

    private readonly object _lock = new();
    private readonly SnapshotFilter _snapshotFilter;
    private readonly ISnapshotSelector _snapshotSelector;
    private readonly Dictionary<(ActivitySpanId SpanId, ActivityTraceId TraceId), DateTimeOffset> _localRootSpans = new();
    private readonly Timer _timer;

    public SnapshotSelectingProcessor(SnapshotFilter snapshotFilter, ISnapshotSelector snapshotSelector)
    {
        _snapshotFilter = snapshotFilter;
        _snapshotSelector = snapshotSelector;
        _timer = new Timer(Clean, null, DefaultTimeToLive, DefaultTimeToLive);
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

        var cacheKey = (data.SpanId, data.TraceId);
        lock (_lock)
        {
            if (_localRootSpans.ContainsKey(cacheKey))
            {
                Log.Warning("Local root span already registered.");
                return;
            }

            if (_localRootSpans.Count >= SnapshotLocalRootLimit)
            {
                Log.Warning("Too many traces selected for snapshotting.");
                return;
            }

            _snapshotFilter.Add(data.TraceId);
            _localRootSpans.Add(cacheKey, DateTimeOffset.UtcNow + DefaultTimeToLive);
            data.MarkLoud();
        }
    }

    public override void OnEnd(Activity data)
    {
        if (!data.IsLocalRoot())
        {
            return;
        }

        var cacheKey = (data.SpanId, data.TraceId);
        lock (_lock)
        {
            if (_localRootSpans.Remove(cacheKey))
            {
                _snapshotFilter.Remove(data.TraceId);
            }
        }
    }

    internal void Clean(DateTimeOffset now)
    {
        lock (_lock)
        {
            var expiredSpans = new List<(ActivitySpanId SpanId, ActivityTraceId TraceId)>();

            foreach (var localRootSpan in _localRootSpans)
            {
                if (now >= localRootSpan.Value)
                {
                    expiredSpans.Add(localRootSpan.Key);
                }
            }

            foreach (var cacheKey in expiredSpans)
            {
                _localRootSpans.Remove(cacheKey);
                _snapshotFilter.Remove(cacheKey.TraceId);
            }
        }
    }

    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        _timer.Dispose();
        return true;
    }

    private void Clean(object? state)
    {
        Clean(DateTimeOffset.UtcNow);
    }
}
