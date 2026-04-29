// <copyright file="EffectiveConfigReader.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal static class EffectiveConfigReader
{
    public static IReadOnlyDictionary<string, string> Read(PluginSettings settings)
    {
        var config = new Dictionary<string, string>();
        PopulateSplunkSettings(config, settings);
        return config;
    }

    private static void PopulateSplunkSettings(Dictionary<string, string> config, PluginSettings settings)
    {
        config[ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled] = settings.CpuProfilerEnabled.ToString();
#if NET
        config[ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled] = settings.MemoryProfilerEnabled.ToString();
#else
        config[ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled] = false.ToString();
#endif
        config[ConfigurationKeys.Splunk.Snapshots.Enabled] = settings.SnapshotsEnabled.ToString();
        config[ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs] = settings.SnapshotsSamplingInterval.ToString();
        config[ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval] = settings.CpuProfilerCallStackInterval.ToString();
    }
}
