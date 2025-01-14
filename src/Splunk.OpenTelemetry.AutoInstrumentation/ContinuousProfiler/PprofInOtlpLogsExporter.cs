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

#if NET
namespace Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class PprofInOtlpLogsExporter
{
    private readonly SampleProcessor _sampleProcessor;
    private readonly SampleExporter _sampleExporter;

    public PprofInOtlpLogsExporter(SampleProcessor sampleProcessor, SampleExporter sampleExporter)
    {
        _sampleProcessor = sampleProcessor;
        _sampleExporter = sampleExporter;
    }

    public void ExportThreadSamples(byte[] buffer, int read, CancellationToken cancellationToken)
    {
        var threadSamples = SampleNativeFormatParser.ParseThreadSamples(buffer, read);
        var logRecord = _sampleProcessor.ProcessThreadSamples(threadSamples);
        if (logRecord != null)
        {
            _sampleExporter.Export(logRecord, cancellationToken);
        }
    }

    public void ExportAllocationSamples(byte[] buffer, int read, CancellationToken cancellationToken)
    {
        var allocationSamples = SampleNativeFormatParser.ParseAllocationSamples(buffer, read);
        var logRecord = _sampleProcessor.ProcessAllocationSamples(allocationSamples);
        if (logRecord != null)
        {
            _sampleExporter.Export(logRecord, cancellationToken);
        }
    }
}
#endif
