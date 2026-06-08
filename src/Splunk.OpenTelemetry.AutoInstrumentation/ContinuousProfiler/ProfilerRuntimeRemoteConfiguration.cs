// <copyright file="ProfilerRuntimeRemoteConfiguration.cs" company="Splunk Inc.">
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

using System.Globalization;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration;

namespace Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal static class ProfilerRuntimeRemoteConfiguration
{
    public const string RemoteConfigFileName = "splunk.remote.config";
    public const string RemoteConfigContentType = "application/yaml";

    public static ProfilerRuntimeUpdateResult ApplyYaml(string yaml)
    {
        return ProfilerRuntimeConfiguration.Apply(ParseValues(yaml));
    }

    internal static IReadOnlyDictionary<string, string?> ParseValues(string yaml)
    {
        var fileName = Path.Combine(Path.GetTempPath(), $"splunk-remote-config-{Guid.NewGuid():N}.yaml");
        try
        {
            File.WriteAllText(fileName, yaml);
            return ParseValues(PluginSettings.LoadSplunkConfig(fileName));
        }
        finally
        {
            TryDelete(fileName);
        }
    }

    internal static IReadOnlyDictionary<string, string?> ParseValues(YamlRoot? configuration)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        var profilingConfig = configuration?.Distribution?.Splunk?.Profiling;

        if (profilingConfig == null)
        {
            return values;
        }

        AddCpuProfilerValues(profilingConfig, values);
        AddMemoryProfilerValues(profilingConfig, values);
        AddCallgraphsValues(profilingConfig, values);

        return values;
    }

    private static void AddCpuProfilerValues(
        ProfilerConfiguration profilingConfig,
        IDictionary<string, string?> values)
    {
        var cpuProfiler = profilingConfig.AlwaysOn?.CpuProfiler;
        var enabled = cpuProfiler != null;
        values[ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled] = FormatBool(enabled);
        if (!enabled)
        {
            return;
        }

        values[ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval] =
            cpuProfiler!.SamplingInterval.ToString(CultureInfo.InvariantCulture);
    }

    private static void AddMemoryProfilerValues(
        ProfilerConfiguration profilingConfig,
        IDictionary<string, string?> values)
    {
        var memoryProfiler = profilingConfig.AlwaysOn?.MemoryProfiler;
        var enabled = memoryProfiler != null;
        values[ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled] = FormatBool(enabled);
        if (!enabled)
        {
            return;
        }

        values[ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples] =
            memoryProfiler!.MaxMemorySamples.ToString(CultureInfo.InvariantCulture);
    }

    private static void AddCallgraphsValues(
        ProfilerConfiguration profilingConfig,
        IDictionary<string, string?> values)
    {
        var callgraphs = profilingConfig.Callgraphs;
        var enabled = callgraphs != null;
        values[ConfigurationKeys.Splunk.Snapshots.Enabled] = FormatBool(enabled);
        if (!enabled)
        {
            return;
        }

        values[ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs] =
            callgraphs!.SamplingInterval.ToString(CultureInfo.InvariantCulture);
        values[ConfigurationKeys.Splunk.Snapshots.SelectionRate] =
            callgraphs.SelectionProbability.ToString(CultureInfo.InvariantCulture);
    }

    private static void TryDelete(string fileName)
    {
        try
        {
            File.Delete(fileName);
        }
        catch
        {
            // Best effort cleanup for a transient parser input file.
        }
    }

    private static string FormatBool(bool value)
    {
        return value ? "true" : "false";
    }
}
