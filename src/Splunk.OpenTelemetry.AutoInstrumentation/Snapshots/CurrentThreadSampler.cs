// <copyright file="CurrentThreadSampler.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.Snapshots
{
    internal class CurrentThreadSampler
    {
        private static readonly Lazy<CurrentThreadSampler> InstanceFactory = new(() => new CurrentThreadSampler(NativeMethods.StartSamplingDelegate, NativeMethods.StopSamplingDelegate));

        private readonly Action? _startSamplingDelegate;
        private readonly Action? _stopSamplingDelegate;

        internal CurrentThreadSampler(Action? startSamplingDelegate, Action? stopSamplingDelegate)
        {
            _startSamplingDelegate = startSamplingDelegate;
            _stopSamplingDelegate = stopSamplingDelegate;
        }

        public static CurrentThreadSampler Instance => InstanceFactory.Value;

        public void StartSampling()
        {
            _startSamplingDelegate?.Invoke();
        }

        public void StopSampling()
        {
            _stopSamplingDelegate?.Invoke();
        }
    }
}
#endif
