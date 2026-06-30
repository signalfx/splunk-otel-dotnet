// <copyright file="EffectiveConfigStateTests.cs" company="Splunk Inc.">
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

public class EffectiveConfigStateTests
{
    [Fact]
    public void CreateSnapshot_CapturesSplunkSettings()
    {
        var configuration = new NameValueCollection
        {
            { ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled, "true" },
            { ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled, "true" },
            { ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval, "10000" },
            { ConfigurationKeys.Splunk.Snapshots.Enabled, "true" },
            { ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs, "5000" }
        };
        var settings = new PluginSettings(new NameValueConfigurationSource(configuration));
        var state = new EffectiveConfigState();

        state.SetSplunkSettings(settings);

        var snapshot = state.CreateSnapshot([], [], []);
        Assert.False(snapshot.IsFileBasedConfig);
        Assert.Null(snapshot.FileBasedConfigFileName);
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
    public void CreateSnapshot_CapturesFileBasedConfigFileMetadata()
    {
        var settings = new PluginSettings(new YamlRoot(), "stable.yaml", "experimental.yaml");
        var state = new EffectiveConfigState();

        state.SetSplunkSettings(settings);

        var snapshot = state.CreateSnapshot([], [], []);
        Assert.True(snapshot.IsFileBasedConfig);
        Assert.Equal("stable.yaml", snapshot.FileBasedConfigFileName);
        Assert.Equal("experimental.yaml", snapshot.OtelExperimentalConfigFile);
    }

    [Fact]
    public void CreateSnapshot_UsesProvidedEndpoints()
    {
        var state = new EffectiveConfigState();
        var traceEndpoints = new[] { EffectiveOtlpEndpoint.Http("http://collector:4318/v1/traces") };
        var metricEndpoints = new[] { EffectiveOtlpEndpoint.Http("http://collector:4318/v1/metrics", EffectiveOtlpPipelineType.Periodic) };
        var logEndpoints =
            new[] { EffectiveOtlpEndpoint.Grpc("http://collector:4317/opentelemetry.proto.collector.logs.v1.LogsService/Export") };
        var snapshot = state.CreateSnapshot(traceEndpoints, metricEndpoints, logEndpoints);

        Assert.Equal(traceEndpoints, snapshot.TraceEndpoints);
        Assert.Equal(metricEndpoints, snapshot.MetricEndpoints);
        Assert.Equal(logEndpoints, snapshot.LogEndpoints);
    }
}
