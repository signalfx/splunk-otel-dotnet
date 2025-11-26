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
            Assert.Equal(10000u, settings.CpuProfilerCallStackInterval);
            Assert.Equal(3000u, settings.ProfilerHttpClientTimeout);
            Assert.Equal(500u, settings.ProfilerExportInterval);
            Assert.Equal(200u, settings.MemoryProfilerMaxMemorySamplesPerMinute);
#endif
        }

        [Fact]
        internal void PluginSettings_FileBased()
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.Enabled, "true");
            Environment.SetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.FileName, "ConfigFiles/config.yaml");
            var settings = PluginSettings.FromDefaultSources();

            Assert.True(settings.TraceResponseHeaderEnabled);
#if NET
            Assert.True(settings.CpuProfilerEnabled);
            Assert.True(settings.MemoryProfilerEnabled);
            Assert.Equal("http://localhost:4444/v1/logs", settings.ProfilerLogsEndpoint.ToString());
            Assert.Equal(1000u, settings.CpuProfilerCallStackInterval);
            Assert.Equal(300u, settings.ProfilerHttpClientTimeout);
            Assert.Equal(50u, settings.ProfilerExportInterval);
            Assert.Equal(20u, settings.MemoryProfilerMaxMemorySamplesPerMinute);

            Assert.True(settings.SnapshotsEnabled);
            Assert.Equal(0.2, settings.SnapshotsSelectionRate);
            Assert.Equal(15, settings.SnapshotsSamplingInterval);
#endif
        }

        [Fact]
        internal void PluginSettings_FileBasedEnvVar()
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.Enabled, "true");
            Environment.SetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.FileName, "ConfigFiles/updatedConfig.yaml");

            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.TraceResponseHeaderEnabled, "true");
#if NET
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval, "2000");
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled, "true");
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerLogsEndpoint, "http://localhost:9999/v1/logs");
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerExportTimeout, "600");
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerExportInterval, "120");
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples, "10");

            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs, "25");
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.Snapshots.SelectionRate, "0.1");
#endif

            var settings = PluginSettings.FromDefaultSources();

            Assert.True(settings.TraceResponseHeaderEnabled);
#if NET
            Assert.True(settings.CpuProfilerEnabled);
            Assert.True(settings.MemoryProfilerEnabled);
            Assert.Equal("http://localhost:9999/v1/logs", settings.ProfilerLogsEndpoint.ToString());
            Assert.Equal(2000u, settings.CpuProfilerCallStackInterval);
            Assert.Equal(600u, settings.ProfilerHttpClientTimeout);
            Assert.Equal(120u, settings.ProfilerExportInterval);
            Assert.Equal(10u, settings.MemoryProfilerMaxMemorySamplesPerMinute);

            Assert.True(settings.SnapshotsEnabled);
            Assert.Equal(0.1, settings.SnapshotsSelectionRate);
            Assert.Equal(25, settings.SnapshotsSamplingInterval);
#endif
            ClearEnvVars();
        }

        private static void ClearEnvVars()
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.Enabled, null);
            Environment.SetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.FileName, null);
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
