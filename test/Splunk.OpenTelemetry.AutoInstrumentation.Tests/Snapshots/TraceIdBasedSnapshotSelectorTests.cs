// <copyright file="TraceIdBasedSnapshotSelectorTests.cs" company="Splunk Inc.">
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
using Splunk.OpenTelemetry.AutoInstrumentation.Snapshots;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests.Snapshots
{
    public class TraceIdBasedSnapshotSelectorTests
    {
        [Fact]
        public void Select_ShouldReturnFalse_ForDefaultTraceId()
        {
            var result = TraceIdBasedSnapshotSelector.Select(default, selectionProbability: 1.0);

            Assert.False(result);
        }

        [Fact]
        public void Select_ShouldReturnTrue_WhenTraceIdRandomnessMatchesThreshold()
        {
            var traceId = CreateTraceIdFromRandomness("7ffffff");

            var result = TraceIdBasedSnapshotSelector.Select(traceId, selectionProbability: 0.5);

            Assert.True(result);
        }

        [Fact]
        public void Select_ShouldReturnFalse_WhenTraceIdRandomnessExceedsThreshold()
        {
            var traceId = CreateTraceIdFromRandomness("8000000");

            var result = TraceIdBasedSnapshotSelector.Select(traceId, selectionProbability: 0.5);

            Assert.False(result);
        }

        [Fact]
        public void Select_ShouldReturnFalse_WhenProbabilityYieldsZeroThreshold()
        {
            var traceId = CreateTraceIdFromRandomness("0000000");

            var result = TraceIdBasedSnapshotSelector.Select(traceId, selectionProbability: 1e-10);

            Assert.False(result);
        }

        private static ActivityTraceId CreateTraceIdFromRandomness(string randomnessHex)
        {
            // The leading "1" keeps the trace ID valid when the suffix is all zeros.
            var traceIdHex = "1" + randomnessHex.PadLeft(31, '0');
            return ActivityTraceId.CreateFromString(traceIdHex.AsSpan());
        }
    }
}
#endif
