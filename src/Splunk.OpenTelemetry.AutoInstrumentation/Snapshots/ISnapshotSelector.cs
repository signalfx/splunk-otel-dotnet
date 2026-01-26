// <copyright file="ISnapshotSelector.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.Snapshots
{
    /// <summary>
    /// Represents snapshot selector.
    /// </summary>
    internal interface ISnapshotSelector
    {
        /// <summary>
        /// Method for selecting trace for capturing snapshots.
        /// </summary>
        /// <param name="context">Current span context.</param>
        /// <returns>Returns <c>true</c> if snapshots should be captured for the trace; otherwise, <c>false</c>.</returns>
        public bool Select(ActivityContext context);
    }
}
