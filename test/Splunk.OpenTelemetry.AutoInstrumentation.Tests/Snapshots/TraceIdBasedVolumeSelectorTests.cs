// <copyright file="TraceIdBasedVolumeSelectorTests.cs" company="Splunk Inc.">
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
using Splunk.OpenTelemetry.AutoInstrumentation.Snapshots;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests.Snapshots
{
    public class TraceIdBasedVolumeSelectorTests
    {
        [Fact]
        public void Select_ShouldReturnFalse_ForDefaultContext()
        {
            var selector = new TraceIdBasedSnapshotSelector(1.0);
            var context = default(System.Diagnostics.ActivityContext);

            var result = selector.Select(context);

            Assert.False(result);
        }
    }
}
#endif
