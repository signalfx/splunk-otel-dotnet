// <copyright file="SnapshotSelectingProcessor.cs" company="Splunk Inc.">
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
using OpenTelemetry;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Snapshots
{
    internal class SnapshotSelectingProcessor : BaseProcessor<Activity>
    {
        private readonly SnapshotProcessorHelper _snapshotProcessorHelper;

        public SnapshotSelectingProcessor(SnapshotProcessorHelper snapshotProcessorHelper)
        {
            _snapshotProcessorHelper = snapshotProcessorHelper;
        }

        public override void OnStart(Activity data)
        {
            _snapshotProcessorHelper.ProcessSpanStart(data);
        }

        public override void OnEnd(Activity data)
        {
            _snapshotProcessorHelper.ProcessSpanStop(data);
        }
    }
}
#endif
