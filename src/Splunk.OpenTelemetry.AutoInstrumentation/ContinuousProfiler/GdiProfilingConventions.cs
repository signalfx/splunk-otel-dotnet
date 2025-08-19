// <copyright file="GdiProfilingConventions.cs" company="Splunk Inc.">
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
using Splunk.OpenTelemetry.AutoInstrumentation.Proto.Common.V1;

namespace Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

/// <summary>
/// Holds the GDI profiling semantic conventions.
/// <see href="https://github.com/signalfx/gdi-specification/blob/v1.5.0/specification/semantic_conventions.md"/>
/// </summary>
internal static class GdiProfilingConventions
{
    private const string OpenTelemetryProfiling = "otel.profiling";
    private const string Version = "0.1.0";

    public static class OpenTelemetry
    {
        public static readonly InstrumentationScope InstrumentationLibrary = new()
        {
            Name = OpenTelemetryProfiling,
            Version = Version
        };
    }

    public static class LogRecord
    {
        public static class Attributes
        {
            public static readonly KeyValue Source = new()
            {
                Key = "com.splunk.sourcetype",
                Value = new AnyValue
                {
                    StringValue = OpenTelemetryProfiling
                }
            };

            public static KeyValue Type(string sampleType)
            {
                return new KeyValue
                {
                    Key = "profiling.data.type",
                    Value = new AnyValue
                    {
                        StringValue = sampleType
                    }
                };
            }

            public static KeyValue Format(string format)
            {
                return new KeyValue
                {
                    Key = "profiling.data.format",
                    Value = new AnyValue
                    {
                        StringValue = format
                    }
                };
            }

            public static KeyValue InstrumentationSource(string source)
            {
                return new KeyValue
                {
                    Key = "profiling.instrumentation.source",
                    Value = new AnyValue
                    {
                        StringValue = source
                    }
                };
            }
        }
    }
}
#endif
