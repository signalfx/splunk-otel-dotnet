// <copyright file="ISampleExporter.cs" company="Splunk Inc.">
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
using Splunk.OpenTelemetry.AutoInstrumentation.Proto.Logs.V1;

namespace Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler
{
    /// <summary>
    /// Exports log records containing samples.
    /// </summary>
    internal interface ISampleExporter
    {
        /// <summary>
        /// Exports log record.
        /// </summary>
        /// <param name="logRecord">Log record containing samples.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        void Export(LogRecord logRecord, CancellationToken cancellationToken);
    }
}
#endif
