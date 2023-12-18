// <copyright file="SampleExporter.cs" company="Splunk Inc.">
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
using Splunk.OpenTelemetry.AutoInstrumentation.Proto.Common.V1;
using Splunk.OpenTelemetry.AutoInstrumentation.Proto.Logs.V1;
using Splunk.OpenTelemetry.AutoInstrumentation.Proto.Resource.V1;

namespace Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

/// <summary>
/// Exports Cpu/Allocation samples, accumulating LogRecords created by provided native buffer processors.
/// </summary>
internal class SampleExporter
{
    private static readonly ILogger Logger = new Logger();

    private readonly OtlpHttpLogSender _logSender;

    private readonly LogsData _logsData;

    public SampleExporter(OtlpHttpLogSender logSender)
    {
        _logSender = logSender ?? throw new ArgumentNullException(nameof(logSender));
        // The same LogsData instance is used on all export messages. With the exception of the list of
        // LogRecords, the Logs property, all other fields are prepopulated.
        _logsData = CreateLogsData();
    }

    public void Export(LogRecord logRecord)
    {
        var logRecords = _logsData.ResourceLogs[0].ScopeLogs[0].LogRecords;
        try
        {
            logRecords.Add(logRecord);

            if (logRecords.Count > 0)
            {
                _logSender.Send(_logsData);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error processing samples.");
        }
        finally
        {
            // The exporter reuses the logRecords object, but the actual log records are not
            // needed after serialization, release the log records so they can be garbage collected.
            logRecords.Clear();
        }
    }

    private static LogsData CreateLogsData()
    {
        var resource = new Resource();
        var profilingAttributes = OtelResource
                                 .GetCommonAttributes()
                                 .Select(kv =>
                                             new KeyValue
                                             {
                                                 Key = kv.Key,
                                                 Value = new AnyValue
                                                 {
                                                     StringValue = kv.Value
                                                 }
                                             });
        resource.Attributes.AddRange(profilingAttributes);

        return new LogsData
        {
            ResourceLogs =
            {
                new ResourceLogs
                {
                    ScopeLogs =
                    {
                        new ScopeLogs
                        {
                            Scope = GdiProfilingConventions.OpenTelemetry.InstrumentationLibrary
                        }
                    },
                    Resource = resource
                }
            }
        };
    }
}
