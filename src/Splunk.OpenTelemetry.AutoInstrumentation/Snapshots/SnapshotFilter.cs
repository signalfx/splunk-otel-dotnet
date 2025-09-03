// <copyright file="SnapshotFilter.cs" company="Splunk Inc.">
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

#if NET

namespace Splunk.OpenTelemetry.AutoInstrumentation.Snapshots
{
    internal class SnapshotFilter
    {
        private static readonly Lazy<SnapshotFilter> InstanceFactory = new(() => new SnapshotFilter(NativeMethods.StartSamplingDelegate, NativeMethods.StopSamplingDelegate));

        private readonly Action<Activity>? _startSamplingDelegate;
        private readonly Action<Activity>? _stopSamplingDelegate;

        internal SnapshotFilter(Action<Activity>? startSamplingDelegate, Action<Activity>? stopSamplingDelegate)
        {
            _startSamplingDelegate = startSamplingDelegate;
            _stopSamplingDelegate = stopSamplingDelegate;
        }

        public static SnapshotFilter Instance => InstanceFactory.Value;

        public void Add(Activity data)
        {
            _startSamplingDelegate?.Invoke(data);
        }

        public void Remove(Activity data)
        {
            _stopSamplingDelegate?.Invoke(data);
        }
    }
}
#endif
