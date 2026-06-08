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

    private const string ProfilingPath = "distribution.splunk.profiling";
    private const string AlwaysOnPath = ProfilingPath + ".always_on";
    private const string CpuProfilerPath = AlwaysOnPath + ".cpu_profiler";
    private const string MemoryProfilerPath = AlwaysOnPath + ".memory_profiler";
    private const string CallgraphsPath = ProfilingPath + ".callgraphs";

    public static ProfilerRuntimeUpdateResult ApplyYaml(string yaml)
    {
        return ProfilerRuntimeConfiguration.Apply(ParseValues(yaml));
    }

    internal static IReadOnlyDictionary<string, string?> ParseValues(string yaml)
    {
        var yamlValues = RelaxedYamlMapParser.Parse(yaml);
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);

        if (!HasPath(yamlValues, ProfilingPath))
        {
            return values;
        }

        AddCpuProfilerValues(yamlValues, values);
        AddMemoryProfilerValues(yamlValues, values);
        AddCallgraphsValues(yamlValues, values);

        return values;
    }

    private static void AddCpuProfilerValues(
        IReadOnlyDictionary<string, string?> yamlValues,
        IDictionary<string, string?> values)
    {
        var enabled = HasPath(yamlValues, CpuProfilerPath);
        values[ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled] = FormatBool(enabled);
        if (!enabled)
        {
            return;
        }

        values[ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval] =
            GetValueOrDefault(yamlValues, CpuProfilerPath + ".sampling_interval", Constants.DefaultSamplingInterval);
    }

    private static void AddMemoryProfilerValues(
        IReadOnlyDictionary<string, string?> yamlValues,
        IDictionary<string, string?> values)
    {
        var enabled = HasPath(yamlValues, MemoryProfilerPath);
        values[ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled] = FormatBool(enabled);
        if (!enabled)
        {
            return;
        }

        values[ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples] =
            GetValueOrDefault(yamlValues, MemoryProfilerPath + ".max_memory_samples", Constants.DefaultMaxMemorySamples);
    }

    private static void AddCallgraphsValues(
        IReadOnlyDictionary<string, string?> yamlValues,
        IDictionary<string, string?> values)
    {
        var enabled = HasPath(yamlValues, CallgraphsPath);
        values[ConfigurationKeys.Splunk.Snapshots.Enabled] = FormatBool(enabled);
        if (!enabled)
        {
            return;
        }

        values[ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs] =
            GetValueOrDefault(yamlValues, CallgraphsPath + ".sampling_interval", Constants.DefaultSnapshotSamplingIntervalMs);
        values[ConfigurationKeys.Splunk.Snapshots.SelectionRate] =
            GetValueOrDefault(yamlValues, CallgraphsPath + ".selection_probability", Constants.DefaultSnapshotSelectionRate);
    }

    private static string GetValueOrDefault(
        IReadOnlyDictionary<string, string?> yamlValues,
        string path,
        uint defaultValue)
    {
        return yamlValues.TryGetValue(path, out var value) && !IsNullLiteral(value)
            ? value!
            : defaultValue.ToString(CultureInfo.InvariantCulture);
    }

    private static string GetValueOrDefault(
        IReadOnlyDictionary<string, string?> yamlValues,
        string path,
        double defaultValue)
    {
        return yamlValues.TryGetValue(path, out var value) && !IsNullLiteral(value)
            ? value!
            : defaultValue.ToString(CultureInfo.InvariantCulture);
    }

    private static bool HasPath(IReadOnlyDictionary<string, string?> yamlValues, string path)
    {
        foreach (var key in yamlValues.Keys)
        {
            if (key == path || key.StartsWith(path + ".", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNullLiteral(string? value)
    {
        return RelaxedYamlMapParser.IsNullLiteral(value);
    }

    private static string FormatBool(bool value)
    {
        return value ? "true" : "false";
    }
}
