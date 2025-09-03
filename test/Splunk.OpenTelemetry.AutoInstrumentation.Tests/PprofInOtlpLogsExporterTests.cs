// <copyright file="PprofInOtlpLogsExporterTests.cs" company="Splunk Inc.">
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

using Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests
{
    public class PprofInOtlpLogsExporterTests
    {
        [Fact]
        public void WhenThreadIsSelectedForFrequentSampling_ItsSampleIsExportedAsBothContinuousAndSnapshot()
        {
            var testExporter = new TestSampleExporter();
            List<ThreadSample>? threadSamples = [
                new ThreadSample(new ThreadSample.Time(100), 1, 1, 1, "sample_thread_1", 0, false) { Frames = { "frame1", "frame2" } },
                new ThreadSample(new ThreadSample.Time(100), 2, 2, 2, "sample_thread_2", 1, true) { Frames = { "frame3" } }
            ];
            var exporter = new PprofInOtlpLogsExporter(new SampleProcessor(), testExporter, new NativeFormatParser(true));

            exporter.ExportThreadSamplesCore(threadSamples, CancellationToken.None);

            var exportedRecords = testExporter.Records;
            Assert.Equal(2, exportedRecords.Count);

            var sampleForThreadNotSelectedForSnapshots = exportedRecords[0];

            // For continuous profiling samples, `profiling.instrumentation.source` attribute can be omitted.
            Assert.DoesNotContain(sampleForThreadNotSelectedForSnapshots.Attributes, kv => kv.Key == "profiling.instrumentation.source");
            Assert.Contains(sampleForThreadNotSelectedForSnapshots.Attributes, kv => kv.Key == "profiling.data.total.frame.count" && kv.Value.IntValue == 3);

            // For snapshot samples, `profiling.instrumentation.source` attribute is required to have value of `snapshot`.
            var threadSampleForThreadSelectedForSnapshots = exportedRecords[1];
            Assert.Contains(threadSampleForThreadSelectedForSnapshots.Attributes, kv => kv.Key == "profiling.instrumentation.source" && kv.Value.StringValue == "snapshot");
            Assert.Contains(threadSampleForThreadSelectedForSnapshots.Attributes, kv => kv.Key == "profiling.data.total.frame.count" && kv.Value.IntValue == 1);
        }

        [Fact]
        public void WhenThreadIsNotSelectedForFrequentSampling_ItsSampleIsExportedAsContinuous()
        {
            var testExporter = new TestSampleExporter();
            List<ThreadSample>? threadSamples = [
                new ThreadSample(new ThreadSample.Time(100), 1, 1, 1, "sample_thread_1", 0, false),
                new ThreadSample(new ThreadSample.Time(100), 2, 2, 2, "sample_thread_2", 1, false)
            ];
            var exporter = new PprofInOtlpLogsExporter(new SampleProcessor(), testExporter, new NativeFormatParser(true));
            exporter.ExportThreadSamplesCore(threadSamples, CancellationToken.None);

            var exportedRecords = testExporter.Records;
            Assert.Single(exportedRecords);

            // For continuous profiling samples, `profiling.instrumentation.source` attribute can be omitted.
            Assert.All(exportedRecords, lr => Assert.DoesNotContain(lr.Attributes, kv => kv.Key == "profiling.instrumentation.source"));
        }
    }
}
#endif
