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
using OpenTelemetry.Trace;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Snapshots;

internal class TraceIdBasedSnapshotSelector : ISnapshotSelector
{
    private readonly TraceIdRatioBasedSampler _sampler;

    public TraceIdBasedSnapshotSelector(double ratio)
    {
        _sampler = new TraceIdRatioBasedSampler(ratio);
    }

    public bool Select(ActivityContext context)
    {
        // context.IsValid() checks if context != default
        if (context.TraceId == default)
        {
            return false;
        }

        // Only TraceId is used from sampling parameters
        var samplingParameters = new SamplingParameters(default, context.TraceId, string.Empty, ActivityKind.Internal);
        return _sampler.ShouldSample(samplingParameters).Decision == SamplingDecision.RecordAndSample;
    }
}
