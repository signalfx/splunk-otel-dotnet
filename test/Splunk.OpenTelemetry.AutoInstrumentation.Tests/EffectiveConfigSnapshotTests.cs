// <copyright file="EffectiveConfigSnapshotTests.cs" company="Splunk Inc.">
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
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class EffectiveConfigSnapshotTests
{
    [Fact]
    public void Create_CopiesResolvedProfilerState()
    {
        const EffectiveProfilerFeatures profilerFeatures = EffectiveProfilerFeatures.Cpu | EffectiveProfilerFeatures.Snapshot;
        var snapshot = EffectiveConfigSnapshot.Create(
            CreateStaticSettings(),
            profilerFeatures,
            [],
            [],
            []);

        Assert.True(snapshot.CpuProfilerEnabled);
        Assert.False(snapshot.MemoryProfilerEnabled);
        Assert.True(snapshot.SnapshotProfilerEnabled);
    }

    [Fact]
    public void Create_CopiesFileBasedStaticSettings()
    {
        var staticSettings = new EffectiveConfigStaticSettings(
            new PluginSettings(new YamlRoot(), "stable.yaml"));

        var snapshot = EffectiveConfigSnapshot.Create(staticSettings, EffectiveProfilerFeatures.None, [], [], []);

        Assert.True(snapshot.IsFileBasedConfig);
        Assert.Equal("stable.yaml", snapshot.FileBasedConfigFileName);
    }

    [Fact]
    public void Create_CopiesProvidedEndpointCollections()
    {
        var traceEndpoints = new List<EffectiveOtlpEndpoint>
        {
            EffectiveOtlpEndpoint.Http("http://collector:4318/v1/traces")
        };
        var metricEndpoints = new List<EffectiveOtlpEndpoint>
        {
            EffectiveOtlpEndpoint.Http("http://collector:4318/v1/metrics", EffectiveOtlpPipelineType.Periodic)
        };
        var logEndpoints = new List<EffectiveOtlpEndpoint>
        {
            EffectiveOtlpEndpoint.Grpc("http://collector:4317/opentelemetry.proto.collector.logs.v1.LogsService/Export")
        };
        var expectedTraceEndpoints = traceEndpoints.ToArray();
        var expectedMetricEndpoints = metricEndpoints.ToArray();
        var expectedLogEndpoints = logEndpoints.ToArray();
        var snapshot = EffectiveConfigSnapshot.Create(
            CreateStaticSettings(),
            EffectiveProfilerFeatures.None,
            traceEndpoints,
            metricEndpoints,
            logEndpoints);

        traceEndpoints.Clear();
        metricEndpoints.Clear();
        logEndpoints.Clear();

        Assert.Equal(expectedTraceEndpoints, snapshot.TraceEndpoints);
        Assert.Equal(expectedMetricEndpoints, snapshot.MetricEndpoints);
        Assert.Equal(expectedLogEndpoints, snapshot.LogEndpoints);
    }

    [Fact]
    public void Constructor_RejectsFileNameOverLengthLimit()
    {
        var fileName = new string('a', EffectiveConfigLimits.MaxFileNameLength + 1);

        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateSnapshot(fileBasedConfigFileName: fileName));

        Assert.Contains(EffectiveConfigLimits.MaxFileNameLength.ToString(), exception.Message);
    }

    [Fact]
    public void Constructor_RejectsEndpointCountOverLimit()
    {
        var endpoints = Enumerable.Repeat(
                EffectiveOtlpEndpoint.Http("http://collector:4318/v1/traces"),
                EffectiveConfigLimits.MaxEndpointCount + 1)
            .ToArray();

        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateSnapshot(traceEndpoints: endpoints));

        Assert.Contains(EffectiveConfigLimits.MaxEndpointCount.ToString(), exception.Message);
    }

    private static EffectiveConfigSnapshot CreateSnapshot(
        string? fileBasedConfigFileName = null,
        IReadOnlyList<EffectiveOtlpEndpoint>? traceEndpoints = null)
    {
        return new EffectiveConfigSnapshot(
            fileBasedConfigFileName,
            traceEndpoints ?? [],
            [],
            [],
            cpuProfilerEnabled: false,
            memoryProfilerEnabled: false,
            snapshotProfilerEnabled: false,
            cpuProfilerCallStackInterval: 10000,
            snapshotSamplingInterval: 40);
    }

    private static EffectiveConfigStaticSettings CreateStaticSettings()
    {
        return new EffectiveConfigStaticSettings(
            new PluginSettings(new NameValueConfigurationSource(new NameValueCollection())));
    }
}
