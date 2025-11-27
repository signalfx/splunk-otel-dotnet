// <copyright file="SnapshotVolumePropagator.cs" company="Splunk Inc.">
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

using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Snapshots
{
    internal class SnapshotVolumePropagator : TextMapPropagator
    {
        private readonly ISnapshotSelector _selector;

        public SnapshotVolumePropagator(ISnapshotSelector selector)
        {
            _selector = selector;
        }

        public override ISet<string>? Fields { get; } = new HashSet<string>();

        public override void Inject<T>(PropagationContext context, T carrier, Action<T, string, string> setter)
        {
        }

        public override PropagationContext Extract<T>(PropagationContext context, T carrier, Func<T, string, IEnumerable<string>?> getter)
        {
            var baggage = context.Baggage;
            var volume = baggage.GetBaggage(SnapshotConstants.VolumeBaggageKeyName);
            if (IsSpecified(volume))
            {
                return context;
            }

            var newVolume = _selector.Select(context.ActivityContext) ? Volume.highest : Volume.off;

            var updatedBaggage = context.Baggage.SetBaggage(SnapshotConstants.VolumeBaggageKeyName, GetStringValue(newVolume));
            return new PropagationContext(context.ActivityContext, updatedBaggage);
        }

        private static bool IsSpecified(string? volume)
        {
            return string.Equals(volume, nameof(Volume.highest), StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(volume, nameof(Volume.off), StringComparison.OrdinalIgnoreCase);
        }

        private static string GetStringValue(Volume newVolume)
        {
            return newVolume switch
            {
                Volume.highest => nameof(Volume.highest),
                Volume.off => nameof(Volume.off),
                Volume.unspecified => nameof(Volume.unspecified),
                _ => newVolume.ToString()
            };
        }
    }
}
#endif
