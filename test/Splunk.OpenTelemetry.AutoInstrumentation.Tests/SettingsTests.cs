// <copyright file="SettingsTests.cs" company="Splunk Inc.">
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

using FluentAssertions.Execution;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests
{
    public class SettingsTests : IDisposable
    {
        public SettingsTests()
        {
            ClearEnvVars();
        }

        public void Dispose()
        {
            ClearEnvVars();
        }

        [Fact]
        internal void PluginSettings_DefaultValues()
        {
            var settings = PluginSettings.FromDefaultSources();

            using (new AssertionScope())
            {
                settings.Realm.Should().Be("none");
                settings.AccessToken.Should().BeNull();
                settings.TraceResponseHeaderEnabled.Should().BeTrue();
#if NET
                settings.ProfilerLogsEndpoint.Should().Be("http://localhost:4318/v1/logs");
                settings.CpuProfilerEnabled.Should().BeFalse();
                settings.MemoryProfilerEnabled.Should().BeFalse();
                settings.CpuProfilerCallStackInterval.Should().Be(10000u);
#endif
            }
        }

        private static void ClearEnvVars()
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.Realm, null);
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.AccessToken, null);
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.TraceResponseHeaderEnabled, null);
#if NET
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled, null);
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval, null);
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled, null);
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerLogsEndpoint, null);
#endif
        }
    }
}
