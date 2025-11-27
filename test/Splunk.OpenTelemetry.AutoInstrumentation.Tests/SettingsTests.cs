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

using System.Collections.Specialized;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;

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

#if NET
        [Fact]
        public void MaxSnapshotSelectionRate_IsBoundedByTheValueFromTheSpec()
        {
            var settings = new PluginSettings(new NameValueConfigurationSource(
                new NameValueCollection
                {
                    ["SPLUNK_SNAPSHOT_SELECTION_PROBABILITY"] = "0.5"
                }));
            Assert.Equal(0.1, settings.SnapshotsSelectionRate);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-0.1")]
        public void SnapshotSelectionRate_HasToBeInBounds(string rate)
        {
            var settings = new PluginSettings(new NameValueConfigurationSource(
                new NameValueCollection
                {
                    ["SPLUNK_SNAPSHOT_SELECTION_PROBABILITY"] = rate
                }));
            Assert.Equal(0.01, settings.SnapshotsSelectionRate);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        public void SnapshotSamplingInterval_HasToBeInBounds(string interval)
        {
            var settings = new PluginSettings(new NameValueConfigurationSource(
                new NameValueCollection
                {
                    ["SPLUNK_SNAPSHOT_SAMPLING_INTERVAL"] = interval
                }));
            Assert.Equal(60, settings.SnapshotsSamplingInterval);
        }
#endif

        [Fact]
        internal void PluginSettings_DefaultValues()
        {
            var settings = PluginSettings.FromDefaultSources();

            Assert.Equal("none", settings.Realm);
            Assert.Null(settings.AccessToken);
            Assert.True(settings.TraceResponseHeaderEnabled);
#if NET
            Assert.Equal("http://localhost:4318/v1/logs", settings.ProfilerLogsEndpoint.ToString());
            Assert.False(settings.CpuProfilerEnabled);
            Assert.False(settings.MemoryProfilerEnabled);
            Assert.False(settings.SnapshotsEnabled);
            Assert.False(settings.HighResolutionTimerEnabled);
            Assert.Equal(10000u, settings.CpuProfilerCallStackInterval);
            Assert.Equal(3000u, settings.ProfilerHttpClientTimeout);
            Assert.Equal(500u, settings.ProfilerExportInterval);
            Assert.Equal(200u, settings.MemoryProfilerMaxMemorySamplesPerMinute);
#endif
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
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerExportTimeout, null);
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerExportInterval, null);
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples, null);
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.Snapshots.Enabled, null);
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs, null);
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.Snapshots.SelectionRate, null);
#endif
        }
    }
}
