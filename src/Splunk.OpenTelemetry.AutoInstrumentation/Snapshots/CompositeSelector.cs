// <copyright file="CompositeSelector.cs" company="Splunk Inc.">
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

using System.Diagnostics;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Snapshots
{
    internal class CompositeSelector : ISnapshotSelector
    {
        private readonly ProbabilisticVolumeSelector _probabilisticSelector;
        private readonly TraceIdBasedSnapshotSelector _traceIdRatioBasedSelector;

        public CompositeSelector(double ratio)
        {
            _probabilisticSelector = new ProbabilisticVolumeSelector(ratio);
            _traceIdRatioBasedSelector = new TraceIdBasedSnapshotSelector(ratio);
        }

        public bool Select(ActivityContext context)
        {
            return _probabilisticSelector.Select(context) || _traceIdRatioBasedSelector.Select(context);
        }
    }
}
#endif
