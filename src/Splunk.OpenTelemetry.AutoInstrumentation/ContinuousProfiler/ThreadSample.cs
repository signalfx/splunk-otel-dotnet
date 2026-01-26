// <copyright file="ThreadSample.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class ThreadSample
{
    public ThreadSample(
        Time timestamp,
        long spanId,
        long traceIdHigh,
        long traceIdLow,
        string threadName,
        uint threadIndex,
        bool selectedForFrequentSampling)
    {
        Timestamp = timestamp;
        SpanId = spanId;
        TraceIdHigh = traceIdHigh;
        TraceIdLow = traceIdLow;
        ThreadName = threadName;
        ThreadIndex = threadIndex;
        SelectedForFrequentSampling = selectedForFrequentSampling;
    }

    public Time Timestamp { get; set; }

    public long SpanId { get; set; }

    public long TraceIdHigh { get; set; }

    public long TraceIdLow { get; set; }

    public string ThreadName { get; set; }

    public uint ThreadIndex { get; set; }

    public bool SelectedForFrequentSampling { get; set; }

    public IList<string> Frames { get; } = new List<string>();

    internal class Time
    {
        public Time(long milliseconds)
        {
            Milliseconds = milliseconds;
            Nanoseconds = (ulong)milliseconds * 1_000_000u;
        }

        public ulong Nanoseconds { get; }

        public long Milliseconds { get; }
    }
}
