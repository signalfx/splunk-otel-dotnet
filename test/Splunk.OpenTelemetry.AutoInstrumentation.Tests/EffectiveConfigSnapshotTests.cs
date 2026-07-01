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
    public void Create_CopiesEnvironmentStaticSettings()
    {
        var configuration = new NameValueCollection
        {
            { ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled, "true" },
            { ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled, "true" },
            { ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval, "10000" },
            { ConfigurationKeys.Splunk.Snapshots.Enabled, "true" },
            { ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs, "5000" }
        };
        var staticSettings = new EffectiveConfigStaticSettings(
            new PluginSettings(new NameValueConfigurationSource(configuration)));
        var snapshot = EffectiveConfigSnapshot.Create(staticSettings, [], [], []);

        Assert.False(snapshot.IsFileBasedConfig);
        Assert.Null(snapshot.FileBasedConfigFileName);
        Assert.Null(snapshot.OtelExperimentalConfigFile);
        Assert.True(snapshot.CpuProfilerEnabled);
#if NET
        Assert.True(snapshot.MemoryProfilerEnabled);
#else
        Assert.False(snapshot.MemoryProfilerEnabled);
#endif
        Assert.True(snapshot.SnapshotProfilerEnabled);
        Assert.Equal(10000U, snapshot.CpuProfilerCallStackInterval);
        Assert.Equal(5000U, snapshot.SnapshotSamplingInterval);
    }

    [Fact]
    public void Create_CopiesFileBasedStaticSettings()
    {
        var staticSettings = new EffectiveConfigStaticSettings(
            new PluginSettings(new YamlRoot(), "stable.yaml", "experimental.yaml"));

        var snapshot = EffectiveConfigSnapshot.Create(staticSettings, [], [], []);

        Assert.True(snapshot.IsFileBasedConfig);
        Assert.Equal("stable.yaml", snapshot.FileBasedConfigFileName);
        Assert.Equal("experimental.yaml", snapshot.OtelExperimentalConfigFile);
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

    private static EffectiveConfigStaticSettings CreateStaticSettings()
    {
        return new EffectiveConfigStaticSettings(
            new PluginSettings(new NameValueConfigurationSource(new NameValueCollection())));
    }
}
