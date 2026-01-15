// <copyright file="PprofInOtlpLogsExporter.cs" company="Splunk Inc.">
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

using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class PprofInOtlpLogsExporter
{
    private static readonly ILogger Log = new Logger();
    private readonly ISampleExporter _sampleExporter;
    private readonly NativeFormatParser _nativeFormatParser;

    public PprofInOtlpLogsExporter(SampleProcessor sampleProcessor, ISampleExporter sampleExporter, NativeFormatParser nativeFormatParser)
    {
        SampleProcessor = sampleProcessor;
        _sampleExporter = sampleExporter;
        _nativeFormatParser = nativeFormatParser;
    }

    public SampleProcessor SampleProcessor { get; }

    public void ExportThreadSamples(byte[] buffer, int read, CancellationToken cancellationToken)
    {
        var threadSamples = _nativeFormatParser.ParseThreadSamples(buffer, read);
        ExportThreadSamplesCore(threadSamples, cancellationToken);
    }

    public void ExportAllocationSamples(byte[] buffer, int read, CancellationToken cancellationToken)
    {
#if NET
        var allocationSamples = _nativeFormatParser.ParseAllocationSamples(buffer, read);
        var logRecord = SampleProcessor.ProcessAllocationSamples(allocationSamples);
        if (logRecord != null)
        {
            _sampleExporter.Export(logRecord, cancellationToken);
        }
#endif
    }

    public void ExportSelectedThreadSamples(byte[] buffer, int read, CancellationToken cancellationToken)
    {
        var allocationSamples = _nativeFormatParser.ParseSelectiveSamplerSamples(buffer, read);

        var logRecord = SampleProcessor.ProcessSnapshots(allocationSamples);

        if (logRecord != null)
        {
            _sampleExporter.Export(logRecord, cancellationToken);
        }
    }

    // Internal for testing.
    // Preferably, NativeFormatParser would live in upstream repository, and this method would be called
    // by the upstream infrastructure, instead of `ExportThreadSamples` method.
    internal void ExportThreadSamplesCore(List<ThreadSample>? threadSamples, CancellationToken cancellationToken)
    {
        if (threadSamples != null)
        {
            var logRecord = SampleProcessor.ProcessThreadSamples(threadSamples);
            Log.Debug($"Processed {threadSamples.Count} thread samples");
            if (logRecord != null)
            {
                Log.Debug("Exporting thread samples");
                _sampleExporter.Export(logRecord, cancellationToken);
            }

            var snapshots = ExtractSnapshots(threadSamples);

            var snapshotLogRecord = SampleProcessor.ProcessSnapshots(snapshots);
            if (snapshotLogRecord != null)
            {
                Log.Debug("Exporting snapshots");
                _sampleExporter.Export(snapshotLogRecord, cancellationToken);
            }
        }
        else
        {
            Log.Debug("No thread samples to export");
        }
    }

    private static List<ThreadSample> ExtractSnapshots(List<ThreadSample> threadSamples)
    {
        var snapshots = new List<ThreadSample>();
        foreach (var ts in threadSamples)
        {
            if (ts.SelectedForFrequentSampling)
            {
                snapshots.Add(ts);
            }
        }

        return snapshots;
    }
}
