// <copyright file="TraceIdBasedSnapshotSelector.cs" company="Splunk Inc.">
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
using System.Globalization;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Snapshots;

internal class TraceIdBasedSnapshotSelector : ISnapshotSelector
{
    private const int RandomnessHexLength = 7;
    private const uint MaxRandomnessValue = 0x0FFFFFFF;

    private readonly uint _threshold;

    public TraceIdBasedSnapshotSelector(double selectionProbability)
    {
        _threshold = (uint)(selectionProbability * MaxRandomnessValue);
    }

    public bool Select(ActivityContext context)
    {
        if (context.TraceId == default)
        {
            return false;
        }

        var traceId = context.TraceId.ToHexString();
        var randomnessHex = traceId.Substring(traceId.Length - RandomnessHexLength, RandomnessHexLength);
        var randomness = uint.Parse(randomnessHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return randomness <= _threshold;
    }
}
