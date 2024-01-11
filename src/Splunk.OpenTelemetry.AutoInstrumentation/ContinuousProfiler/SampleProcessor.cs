// <copyright file="SampleProcessor.cs" company="Splunk Inc.">
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

#if NET6_0_OR_GREATER

using System.IO.Compression;
using System.Runtime.CompilerServices;
using Splunk.OpenTelemetry.AutoInstrumentation.Pprof.Proto.Profile;
using Splunk.OpenTelemetry.AutoInstrumentation.Proto.Common.V1;
using Splunk.OpenTelemetry.AutoInstrumentation.Proto.Logs.V1;
using Splunk.OpenTelemetry.AutoInstrumentation.Vendors.ProtoBuf;

namespace Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class SampleProcessor
{
    private const string TotalFrameCountAttributeName = "profiling.data.total.frame.count";
    private readonly KeyValue _format;
    private readonly KeyValue _profilingDataTypeCpu;
    private readonly KeyValue _profilingDataTypeAllocation;
    private readonly TimeSpan _threadSamplingPeriod;

    public SampleProcessor(TimeSpan threadSamplingPeriod)
    {
        _format = GdiProfilingConventions.LogRecord.Attributes.Format("pprof-gzip-base64");
        _profilingDataTypeCpu = GdiProfilingConventions.LogRecord.Attributes.Type("cpu");
        _profilingDataTypeAllocation = GdiProfilingConventions.LogRecord.Attributes.Type("allocation");

        _threadSamplingPeriod = threadSamplingPeriod;
    }

    public LogRecord? ProcessThreadSamples(List<ThreadSample>? threadSamples)
    {
        if (threadSamples == null || threadSamples.Count < 1)
        {
            return null;
        }

        var cpuProfile = BuildCpuProfile(threadSamples);
        var totalFrameCount = CountFrames(threadSamples);

        return BuildLogRecord(cpuProfile, _profilingDataTypeCpu, totalFrameCount);
    }

    public LogRecord? ProcessAllocationSamples(List<AllocationSample> allocationSamples)
    {
        if (allocationSamples == null || allocationSamples.Count < 1)
        {
            return null;
        }

        var allocationProfile = BuildAllocationProfile(allocationSamples);
        var totalFrameCount = CountFrames(allocationSamples);

        return BuildLogRecord(allocationProfile, _profilingDataTypeAllocation, totalFrameCount);
    }

    private static int CountFrames(List<AllocationSample> samples)
    {
        var sum = 0;
        for (var i = 0; i < samples.Count; i++)
        {
            sum += samples[i].ThreadSample.Frames.Count;
        }

        return sum;
    }

    private static int CountFrames(List<ThreadSample> samples)
    {
        var sum = 0;
        for (var i = 0; i < samples.Count; i++)
        {
            sum += samples[i].Frames.Count;
        }

        return sum;
    }

    private static string Serialize(Profile profile)
    {
        using var memoryStream = new MemoryStream();
        using (var compressionStream = new GZipStream(memoryStream, CompressionMode.Compress))
        {
            Serializer.Serialize(compressionStream, profile);
            compressionStream.Flush();
        }

        var byteArray = memoryStream.ToArray();
        return Convert.ToBase64String(byteArray);
    }

    private static SampleBuilder CreateSampleBuilder(Pprof pprof, ThreadSample threadSample)
    {
        var sampleBuilder = new SampleBuilder();

        pprof.AddLabel(sampleBuilder, "source.event.time", threadSample.Timestamp.Milliseconds);

        if (threadSample.SpanId != 0 || threadSample.TraceIdHigh != 0 || threadSample.TraceIdLow != 0)
        {
            pprof.AddLabel(sampleBuilder, "span_id", threadSample.SpanId.ToString("x16"));
            pprof.AddLabel(sampleBuilder, "trace_id", TraceIdHelper.ToString(threadSample.TraceIdHigh, threadSample.TraceIdLow));
        }

        for (var index = 0; index < threadSample.Frames.Count; index++)
        {
            var methodName = threadSample.Frames[index];
            sampleBuilder.AddLocationId(pprof.GetLocationId(methodName));
        }

        pprof.AddLabel(sampleBuilder, "thread.id", threadSample.ManagedId);
        pprof.AddLabel(sampleBuilder, "thread.name", threadSample.ThreadName);
        return sampleBuilder;
    }

    private static string BuildAllocationProfile(List<AllocationSample> allocationSamples)
    {
        var pprof = new Pprof();
        for (var index = 0; index < allocationSamples.Count; index++)
        {
            var allocationSample = allocationSamples[index];
            var sampleBuilder = CreateSampleBuilder(pprof, allocationSample.ThreadSample);

            sampleBuilder.SetValue(allocationSample.AllocationSizeBytes);
            pprof.Profile.Samples.Add(sampleBuilder.Build());
        }

        return Serialize(pprof.Profile);
    }

    private string BuildCpuProfile(List<ThreadSample> threadSamples)
    {
        var pprof = new Pprof();
        for (var index = 0; index < threadSamples.Count; index++)
        {
            var threadSample = threadSamples[index];
            var sampleBuilder = CreateSampleBuilder(pprof, threadSample);

            pprof.AddLabel(sampleBuilder, "source.event.period", (long)_threadSamplingPeriod.TotalMilliseconds);
            pprof.Profile.Samples.Add(sampleBuilder.Build());
        }

        return Serialize(pprof.Profile);
    }

    private LogRecord BuildLogRecord(string cpuProfile, KeyValue profilingDataType, int totalFrameCount)
    {
        // The stack follows the experimental GDI conventions described at
        // https://github.com/signalfx/gdi-specification/blob/29cbcbc969531d50ccfd0b6a4198bb8a89cedebb/specification/semantic_conventions.md#logrecord-message-fields

        var frameCountAttribute = new KeyValue { Key = TotalFrameCountAttributeName, Value = new AnyValue { IntValue = totalFrameCount } };
        return new LogRecord
        {
            Attributes =
            {
                GdiProfilingConventions.LogRecord.Attributes.Source, profilingDataType, _format, frameCountAttribute
            },
            Body = new AnyValue
            {
                StringValue = cpuProfile
            }
        };
    }

    private static class TraceIdHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToString(long higher, long lower) => ToString((ulong)higher, (ulong)lower);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ToString(ulong higher, ulong lower) => $"{higher:x16}{lower:x16}";
    }
}

#endif
