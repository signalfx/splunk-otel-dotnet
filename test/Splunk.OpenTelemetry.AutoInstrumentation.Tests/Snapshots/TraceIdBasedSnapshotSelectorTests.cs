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
        public void Select_ShouldReturnFalse_ForDefaultContext()
        {
            var selector = new TraceIdBasedSnapshotSelector(1.0);
            var context = default(ActivityContext);

            var result = selector.Select(context);

            Assert.False(result);
        }

        [Fact]
        public void Select_ShouldReturnTrue_WhenTraceIdRandomnessMatchesThreshold()
        {
            var selector = new TraceIdBasedSnapshotSelector(0.5);
            var context = CreateContextFromRandomness("7ffffff");

            var result = selector.Select(context);

            Assert.True(result);
        }

        [Fact]
        public void Select_ShouldReturnFalse_WhenTraceIdRandomnessExceedsThreshold()
        {
            var selector = new TraceIdBasedSnapshotSelector(0.5);
            var context = CreateContextFromRandomness("8000000");

            var result = selector.Select(context);

            Assert.False(result);
        }

        private static ActivityContext CreateContextFromRandomness(string randomnessHex)
        {
            var traceId = randomnessHex.PadLeft(32, '0');
            return new ActivityContext(ActivityTraceId.CreateFromString(traceId.AsSpan()), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        }
    }
}
#endif
