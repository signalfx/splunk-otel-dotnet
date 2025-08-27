// <copyright file="ConfigurationKeys.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal static class ConfigurationKeys
{
    public static class Splunk
    {
        /// <summary>
        /// Configuration key for realm.
        /// </summary>
        public const string Realm = "SPLUNK_REALM";

        /// <summary>
        /// Configuration key for access token.
        /// </summary>
        public const string AccessToken = "SPLUNK_ACCESS_TOKEN";

        /// <summary>
        /// Configuration key for enabling splunk context server timing header.
        /// </summary>
        public const string TraceResponseHeaderEnabled = "SPLUNK_TRACE_RESPONSE_HEADER_ENABLED";

#if NET
        public static class AlwaysOnProfiler
        {
            /// <summary>
            /// Configuration key for interval at which call stacks are sampled (in ms).
            /// </summary>
            public const string CallStackInterval = "SPLUNK_PROFILER_CALL_STACK_INTERVAL";

            /// <summary>
            /// Configuration key for enabling CPU profiler.
            /// </summary>
            public const string CpuProfilerEnabled = "SPLUNK_PROFILER_ENABLED";

            /// <summary>
            /// Configuration key for enabling memory profiler.
            /// </summary>
            public const string MemoryProfilerEnabled = "SPLUNK_PROFILER_MEMORY_ENABLED";

            /// <summary>
            /// Configuration key for endpoint where profiling data is sent. Defaults to the value in `OTLP_EXPORTER_OTLP_ENDPOINT`.
            /// </summary>
            public const string ProfilerLogsEndpoint = "SPLUNK_PROFILER_LOGS_ENDPOINT";

            /// <summary>
            /// Configuration key for Profiler exporter HTTP Client Timeout. Defaults to 3s.
            /// </summary>
            public const string ProfilerExportHTTPClientTimeout = "SPLUNK_PROFILER_EXPORTER_HTTP_CLIENT_TIMEOUT";
        }
#endif
    }

    public static class OpenTelemetry
    {
        /// <summary>
        /// Configuration key for OTLP endpoint.
        /// </summary>
        public const string OtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";
    }
}
