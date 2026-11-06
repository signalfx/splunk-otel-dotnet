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
    [Theory]
    [InlineData("distribution: [")]
    [InlineData("distribution:\n  splunk:\n    profiling:\n      always_on:\n        cpu_profiler:\n          sampling_interval: invalid\n")]
    [InlineData("distribution:\n  splunk:\n    profiling:\n      callgraphs:\n        sampling_interval: invalid\n")]
    [InlineData("distribution:\n  splunk:\n    profiling:\n      callgraphs:\n        selection_probability: invalid\n")]
    [InlineData("distribution:\n  splunk:\n    profiling:\n      callgraphs:\n        sampling_interval: null\n")]
    [InlineData("distribution:\n  splunk:\n    profiling:\n      callgraphs:\n        selection_probability: null\n")]
    public void InvalidYamlIsNotRetriedAndNewConfigurationCanBeApplied(string invalidYaml)
    {
        var assemblyPath = Path.Combine(
            EnvironmentHelper.GetNukeBuildOutput(),
            "net",
            "OpenTelemetry.AutoInstrumentation.dll");
        _ = Assembly.LoadFrom(assemblyPath);

        var configHash = new byte[] { 1, 2, 3 };
        var reports = new List<RemoteConfigStatusReport>();
        var parseAttempts = 0;
        var appliedCount = 0;
        YamlRoot? appliedConfiguration = null;
        var timestamp = 0L;
        var listener = new OpAmpRemoteConfigurationListener(
            () => appliedCount++,
            report =>
            {
                reports.Add(report);
                return Task.CompletedTask;
            },
            configuration => appliedConfiguration = configuration,
            () => timestamp,
            yaml =>
            {
                parseAttempts++;
                return RemoteConfiguration.ParseYaml(yaml);
            });

        foreach (var nextTimestamp in new[] { 0L, 30 * Stopwatch.Frequency, 60 * Stopwatch.Frequency, 3600 * Stopwatch.Frequency })
        {
            timestamp = nextTimestamp;
            listener.HandleRemoteConfiguration(configHash.ToArray(), RemoteConfiguration.RemoteConfigContentType, Encoding.UTF8.GetBytes(invalidYaml));
        }

        Assert.Equal(1, parseAttempts);
        Assert.Equal(0, appliedCount);
        Assert.Null(appliedConfiguration);
        Assert.Equal(new[] { RemoteConfigStatusCode.Applying, RemoteConfigStatusCode.Failed }, reports.Select(report => report.Status));
        Assert.All(reports, report => Assert.Equal(configHash, report.LastRemoteConfigHash.ToArray()));
        Assert.False(string.IsNullOrEmpty(reports[1].ErrorMessage));

        var nextConfigHash = new byte[] { 4, 5, 6 };
        const string ValidYaml = "distribution:\n  splunk:\n    profiling:\n      always_on:\n        cpu_profiler:\n          sampling_interval: 1234\n";
        listener.HandleRemoteConfiguration(nextConfigHash, RemoteConfiguration.RemoteConfigContentType, Encoding.UTF8.GetBytes(ValidYaml));

        Assert.Equal(2, parseAttempts);
        Assert.Equal(1, appliedCount);
        Assert.Equal(1234u, appliedConfiguration?.Distribution?.Splunk?.Profiling?.AlwaysOn?.CpuProfiler?.SamplingInterval);
        Assert.Equal(new[] { RemoteConfigStatusCode.Applying, RemoteConfigStatusCode.Applied }, reports.Skip(2).Select(report => report.Status));
        Assert.All(reports.Skip(2), report => Assert.Equal(nextConfigHash, report.LastRemoteConfigHash.ToArray()));
    }

    [Theory]
    [InlineData("callgraphs:", 40u, 0.01)]
    [InlineData("callgraphs: {}", 40u, 0.01)]
    [InlineData("callgraphs:\n        sampling_interval: 80\n        selection_probability: 0.75", 40u, 0.75)]
    public void CallgraphsUseFileBasedYamlParserAndKeepStartupSamplingInterval(string callgraphs, uint expectedInterval, double expectedProbability)
    {
        _ = Assembly.LoadFrom(Path.Combine(EnvironmentHelper.GetNukeBuildOutput(), "net", "OpenTelemetry.AutoInstrumentation.dll"));
        ProfilerRuntimeConfiguration.Initialize(new PluginSettings(new YamlRoot()));
        var reports = new List<RemoteConfigStatusReport>();
        var listener = new OpAmpRemoteConfigurationListener(
            () => { },
            report =>
            {
                reports.Add(report);
                return Task.CompletedTask;
            },
            configuration => ProfilerRuntimeConfiguration.Apply(configuration, (_, _) => { }));
        var yaml = "distribution:\n  splunk:\n    profiling:\n      always_on:\n        cpu_profiler:\n          sampling_interval: 10000\n      " + callgraphs + "\n";

        listener.HandleRemoteConfiguration([1], RemoteConfiguration.RemoteConfigContentType, Encoding.UTF8.GetBytes(yaml));

        Assert.True(reports.Last().Status == RemoteConfigStatusCode.Applied, reports.Last().ErrorMessage);
        Assert.Equal(new[] { RemoteConfigStatusCode.Applying, RemoteConfigStatusCode.Applied }, reports.Select(report => report.Status));
        var state = ProfilerRuntimeConfiguration.Current;
        Assert.True(state.CpuProfilerEnabled);
        Assert.Equal(10000u, state.CpuProfilerCallStackInterval);
        Assert.True(state.SnapshotsEnabled);
        Assert.Equal(expectedInterval, state.SnapshotsSamplingInterval);
        Assert.Equal(expectedProbability, state.SnapshotsSelectionRate);
    }
}
#endif
