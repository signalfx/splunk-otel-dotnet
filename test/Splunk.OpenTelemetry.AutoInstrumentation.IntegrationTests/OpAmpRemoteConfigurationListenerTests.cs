// <copyright file="OpAmpRemoteConfigurationListenerTests.cs" company="Splunk Inc.">
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
using System.Reflection;
using System.Text;
using OpenTelemetry.OpAmp.Client.Messages;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration;
using Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;
using Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests;

public class OpAmpRemoteConfigurationListenerTests
{
    private const string CpuProfilerYaml = "distribution:\n  splunk:\n    profiling:\n      always_on:\n        cpu_profiler:\n";
    private const string ValidCpuYaml = CpuProfilerYaml + "          sampling_interval: 1234\n";
    private const string MemoryProfilerYaml = ValidCpuYaml + "        memory_profiler:\n";
    private const string CallgraphsYaml = CpuProfilerYaml + "          sampling_interval: 1200\n      callgraphs:\n";

    [Theory]
    [InlineData("distribution: [")]
    [InlineData(CpuProfilerYaml + "          sampling_interval: invalid\n", "Line: 6, Col: 30")]
    [InlineData(CpuProfilerYaml + "          sampling_interval: -1\n", "Line: 6, Col: 30")]
    [InlineData(CpuProfilerYaml + "          sampling_interval: 0\n")]
    [InlineData(CpuProfilerYaml + "          sampling_interval: 4294967296\n", "Line: 6, Col: 30")]
    [InlineData(CpuProfilerYaml + "          sampling_interval: 1000\n      callgraphs:\n        sampling_interval: 40\n        selection_probability: 0.75\n")]
    [InlineData(CpuProfilerYaml + "          sampling_interval: 120\n      callgraphs: {}\n")]
    [InlineData(CpuProfilerYaml + "      callgraphs: {}\n")]
    [InlineData(MemoryProfilerYaml + "          max_memory_samples: -1\n", "Line: 8, Col: 31")]
    [InlineData(MemoryProfilerYaml + "          max_memory_samples: 0\n")]
    [InlineData(MemoryProfilerYaml + "          max_memory_samples: 201\n")]
    [InlineData(CallgraphsYaml + "        sampling_interval: invalid\n")]
    [InlineData(CallgraphsYaml + "        sampling_interval: null\n")]
    [InlineData(CallgraphsYaml + "        selection_probability: invalid\n")]
    [InlineData(CallgraphsYaml + "        selection_probability: null\n")]
    [InlineData(CallgraphsYaml + "        selection_probability: -0.1\n")]
    [InlineData(CallgraphsYaml + "        selection_probability: 0\n")]
    [InlineData(CallgraphsYaml + "        selection_probability: 1.1\n")]
    [InlineData(CallgraphsYaml + "        selection_probability: NaN\n")]
    [InlineData(CallgraphsYaml + "        selection_probability: Infinity\n")]
    public void InvalidPayloadLeavesPreviousConfigurationActiveAndIsNotRetried(string invalidYaml, string? expectedError = null)
    {
        var assemblyPath = Path.Combine(
            EnvironmentHelper.GetFalloutBuildOutput(),
            "net",
            "OpenTelemetry.AutoInstrumentation.dll");
        _ = Assembly.LoadFrom(assemblyPath);
        ProfilerRuntimeConfiguration.Initialize(new PluginSettings(new YamlRoot()) { SnapshotsSamplingInterval = 120 });

        var configHash = new byte[] { 1, 2, 3 };
        var reports = new List<RemoteConfigStatusReport>();
        var parseAttempts = 0;
        var appliedCount = 0;
        var nativeApplyCount = 0;
        var timestamp = 0L;
        var listener = new OpAmpRemoteConfigurationListener(
            () => appliedCount++,
            report =>
            {
                reports.Add(report);
                return Task.CompletedTask;
            },
            configuration => ProfilerRuntimeConfiguration.Apply(configuration, (_, _) => nativeApplyCount++),
            () => timestamp,
            yaml =>
            {
                parseAttempts++;
                return RemoteConfiguration.ParseYaml(yaml);
            });

        const string InitialYaml = CpuProfilerYaml + "          sampling_interval: 2000\n";
        listener.HandleRemoteConfiguration([1], RemoteConfiguration.RemoteConfigContentType, Encoding.UTF8.GetBytes(InitialYaml));
        Assert.Equal(RemoteConfigStatusCode.Applied, reports.Last().Status);
        var previous = ProfilerRuntimeConfiguration.Current;

        foreach (var nextTimestamp in new[] { 0L, 30 * Stopwatch.Frequency, 60 * Stopwatch.Frequency, 3600 * Stopwatch.Frequency })
        {
            timestamp = nextTimestamp;
            listener.HandleRemoteConfiguration(configHash.ToArray(), RemoteConfiguration.RemoteConfigContentType, Encoding.UTF8.GetBytes(invalidYaml));
        }

        Assert.Equal(2, parseAttempts);
        Assert.Equal(1, appliedCount);
        Assert.Equal(1, nativeApplyCount);
        Assert.Same(previous, ProfilerRuntimeConfiguration.Current);
        Assert.Equal(new[] { RemoteConfigStatusCode.Applying, RemoteConfigStatusCode.Failed }, reports.Skip(2).Select(report => report.Status));
        Assert.All(reports.Skip(2), report => Assert.Equal(configHash, report.LastRemoteConfigHash.ToArray()));
        Assert.False(string.IsNullOrEmpty(reports.Last().ErrorMessage));
        if (expectedError != null)
        {
            Assert.Contains(expectedError, reports.Last().ErrorMessage, StringComparison.Ordinal);
        }

        var nextConfigHash = new byte[] { 4, 5, 6 };
        listener.HandleRemoteConfiguration(nextConfigHash, RemoteConfiguration.RemoteConfigContentType, Encoding.UTF8.GetBytes(ValidCpuYaml));

        Assert.Equal(3, parseAttempts);
        Assert.Equal(2, appliedCount);
        Assert.Equal(2, nativeApplyCount);
        Assert.Equal(1234u, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);
        Assert.Equal(new[] { RemoteConfigStatusCode.Applying, RemoteConfigStatusCode.Applied }, reports.Skip(4).Select(report => report.Status));
        Assert.All(reports.Skip(4), report => Assert.Equal(nextConfigHash, report.LastRemoteConfigHash.ToArray()));
    }

    [Theory]
    [InlineData("callgraphs:", 40u, 0.01)]
    [InlineData("callgraphs: {}", 80u, 0.01)]
    [InlineData("callgraphs:\n        sampling_interval: 80\n        selection_probability: 0.75", 40u, 0.75)]
    [InlineData("callgraphs:\n        sampling_interval: 0\n        selection_probability: 0.75", 40u, 0.75)]
    [InlineData("callgraphs:\n        sampling_interval: 4294967295\n        selection_probability: 1", 40u, 1.0)]
    public void CallgraphsUseFileBasedYamlParserAndKeepStartupSamplingInterval(
        string callgraphs,
        uint startupInterval,
        double expectedProbability)
    {
        _ = Assembly.LoadFrom(Path.Combine(EnvironmentHelper.GetFalloutBuildOutput(), "net", "OpenTelemetry.AutoInstrumentation.dll"));
        ProfilerRuntimeConfiguration.Initialize(new PluginSettings(new YamlRoot()) { SnapshotsSamplingInterval = startupInterval });
        var reports = new List<RemoteConfigStatusReport>();
        var listener = new OpAmpRemoteConfigurationListener(
            () => { },
            report =>
            {
                reports.Add(report);
                return Task.CompletedTask;
            },
            configuration => ProfilerRuntimeConfiguration.Apply(configuration, (_, _) => { }));
        var yaml = CpuProfilerYaml + "      " + callgraphs + "\n";

        listener.HandleRemoteConfiguration([1], RemoteConfiguration.RemoteConfigContentType, Encoding.UTF8.GetBytes(yaml));

        Assert.True(reports.Last().Status == RemoteConfigStatusCode.Applied, reports.Last().ErrorMessage);
        Assert.Equal(new[] { RemoteConfigStatusCode.Applying, RemoteConfigStatusCode.Applied }, reports.Select(report => report.Status));
        var state = ProfilerRuntimeConfiguration.Current;
        Assert.True(state.CpuProfilerEnabled);
        Assert.Equal(10000u, state.CpuProfilerCallStackInterval);
        Assert.True(state.SnapshotsEnabled);
        Assert.Equal(startupInterval, state.SnapshotsSamplingInterval);
        Assert.Equal(expectedProbability, state.SnapshotsSelectionRate);
    }
}
#endif
