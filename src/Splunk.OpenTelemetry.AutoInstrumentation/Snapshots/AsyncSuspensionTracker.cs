// <copyright file="AsyncSuspensionTracker.cs" company="Splunk Inc.">
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
using System.Reflection;
using OpenTelemetry;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Snapshots
{
    internal class AsyncSuspensionTracker : IDisposable
    {
        private static readonly ILogger Log = new Logger();

        private static AsyncLocal<Activity?>? _supportingActivityAsyncLocal;
        private readonly CurrentThreadSampler _currentThreadSampler;
        private readonly Func<ActivityListener?> _listenerExtractorFactory;
        private ActivityListener? _autoInstrumentationActivityListener;

        public AsyncSuspensionTracker(CurrentThreadSampler currentThreadSampler)
            : this(currentThreadSampler, TryExtractAutoInstrumentationActivityListener)
        {
        }

        internal AsyncSuspensionTracker(CurrentThreadSampler currentThreadSampler, Func<ActivityListener?> listenerExtractorFactory)
        {
            _supportingActivityAsyncLocal = new AsyncLocal<Activity?>(ActivityChanged);
            _currentThreadSampler = currentThreadSampler;
            _listenerExtractorFactory = listenerExtractorFactory;
            Activity.CurrentChanged += ActivityCurrentChanged;
        }

        public void Dispose()
        {
            Activity.CurrentChanged -= ActivityCurrentChanged;
        }

        private static void ActivityCurrentChanged(object? sender, ActivityChangedEventArgs e)
        {
            if (_supportingActivityAsyncLocal != null)
            {
                _supportingActivityAsyncLocal.Value = e.Current;
            }
        }

        private static ActivityListener? TryExtractAutoInstrumentationActivityListener()
        {
            try
            {
                var tracerProviderSdk =
                    Type.GetType("OpenTelemetry.AutoInstrumentation.Instrumentation, OpenTelemetry.AutoInstrumentation")!
                        .GetField("_tracerProvider", BindingFlags.Static | BindingFlags.NonPublic)!
                        .GetValue(null);
                return (ActivityListener)tracerProviderSdk!
                    .GetType()
                    .GetField("listener", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(tracerProviderSdk)!;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to extract listener from OpenTelemetry.AutoInstrumentation");
            }

            return null;
        }

        private void ActivityChanged(AsyncLocalValueChangedArgs<Activity?> sender)
        {
            var currentActivity = sender.CurrentValue;
            if (currentActivity is not null && !currentActivity.IsStopped && IsFromSubscribedSource(currentActivity))
            {
                if (SnapshotVolumeDetector.IsLoud(Baggage.Current))
                {
                    if (currentActivity.IsEntry())
                    {
                        currentActivity.MarkLoud();
                    }

                    _currentThreadSampler.StartSampling();
                }
                else
                {
                    _currentThreadSampler.StopSampling();
                }
            }
            else
            {
                _currentThreadSampler.StopSampling();
            }
        }

        private bool IsFromSubscribedSource(Activity currentActivity)
        {
            // Tracking (subscription to ActivityChanged event) starts early (suspension tracker is added as instrumentation, in BeforeConfigureTracerProvider).
            // It is possible to get a callback before autoinstrumentation creates TracerProviderSdk instance.
            _autoInstrumentationActivityListener ??= _listenerExtractorFactory();
            return _autoInstrumentationActivityListener != null && _autoInstrumentationActivityListener.ShouldListenTo!(currentActivity.Source);
        }
    }
}
#endif

