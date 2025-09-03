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
        private readonly SnapshotFilter _snapshotFilter;

        public SnapshotSelectingProcessor()
            : this(SnapshotFilter.Instance)
        {
        }

        public SnapshotSelectingProcessor(SnapshotFilter snapshotFilter)
        {
            _snapshotFilter = snapshotFilter;
        }

        public override void OnStart(Activity data)
        {
            if (SnapshotVolumeDetector.IsLoud(Baggage.Current))
            {
                if (data.IsEntry())
                {
                    data.MarkLoud();
                }

                _snapshotFilter.Add(data);
            }
        }

        public override void OnEnd(Activity data)
        {
            // Instead of relying on entry being present in Baggage.Current,
            // we could track entries added in OnStart in a ConcurrentDictionary.
            // This would help if baggage was to change between OnStart and OnEnd.
            if (SnapshotVolumeDetector.IsLoud(Baggage.Current))
            {
                _snapshotFilter.Remove(data);
            }
        }
    }
}
#endif
