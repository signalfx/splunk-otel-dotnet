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

        var snapshot = state.CreateSnapshot();
        Assert.False(snapshot.IsFileBasedConfig);
        Assert.Equal("config.yaml", snapshot.FileBasedConfigFileName);
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

        var snapshot = state.CreateSnapshot();
        Assert.True(snapshot.IsFileBasedConfig);
        Assert.Equal("stable.yaml", snapshot.FileBasedConfigFileName);
        Assert.Equal("stable.yaml", snapshot.OtelConfigFile);
        Assert.Equal("experimental.yaml", snapshot.OtelExperimentalConfigFile);
    }

    [Fact]
    public void CreateSnapshot_CapturesTraceEndpoints()
    {
        var state = new EffectiveConfigState();

        state.SetTraceEndpoints([EffectiveOtlpEndpoint.Http("http://collector:4318/v1/traces")]);

        Assert.Equal([EffectiveOtlpEndpoint.Http("http://collector:4318/v1/traces")], state.CreateSnapshot().TraceEndpoints);
    }

    [Fact]
    public void CreateSnapshot_CapturesMetricEndpoints()
    {
        var state = new EffectiveConfigState();

        state.SetMetricEndpoints([EffectiveOtlpEndpoint.Http("http://collector:4318/v1/metrics")]);

        Assert.Equal([EffectiveOtlpEndpoint.Http("http://collector:4318/v1/metrics")], state.CreateSnapshot().MetricEndpoints);
    }

    [Fact]
    public void CreateSnapshot_CapturesLogEndpoints()
    {
        var state = new EffectiveConfigState();

        state.SetLogEndpoints([EffectiveOtlpEndpoint.Grpc("http://collector:4317/opentelemetry.proto.collector.logs.v1.LogsService/Export")]);

        Assert.Equal(
            [EffectiveOtlpEndpoint.Grpc("http://collector:4317/opentelemetry.proto.collector.logs.v1.LogsService/Export")],
            state.CreateSnapshot().LogEndpoints);
    }

    [Fact]
    public void AddLogEndpoint_ReturnsFalse_WhenEndpointAlreadyExists()
    {
        var state = new EffectiveConfigState();

        var firstAdd = state.AddLogEndpoint(EffectiveOtlpEndpoint.Http("http://collector:4318/v1/logs"));
        var secondAdd = state.AddLogEndpoint(EffectiveOtlpEndpoint.Http("http://collector:4318/v1/logs"));

        Assert.True(firstAdd);
        Assert.False(secondAdd);
        Assert.Equal([EffectiveOtlpEndpoint.Http("http://collector:4318/v1/logs")], state.CreateSnapshot().LogEndpoints);
    }

    [Fact]
    public void AddLogEndpoint_ReturnsTrue_WhenEndpointExistsWithDifferentExporterType()
    {
        var state = new EffectiveConfigState();

        var firstAdd = state.AddLogEndpoint(EffectiveOtlpEndpoint.Http("http://collector:4318/v1/logs"));
        var secondAdd = state.AddLogEndpoint(EffectiveOtlpEndpoint.Grpc("http://collector:4318/v1/logs"));

        Assert.True(firstAdd);
        Assert.True(secondAdd);
        Assert.Equal(
            [
                EffectiveOtlpEndpoint.Http("http://collector:4318/v1/logs"),
                EffectiveOtlpEndpoint.Grpc("http://collector:4318/v1/logs")
            ],
            state.CreateSnapshot().LogEndpoints);
    }

    [Fact]
    public void ClearLogEndpoints_ReturnsFalse_WhenEndpointKeyIsMissing()
    {
        var state = new EffectiveConfigState();

        Assert.False(state.ClearLogEndpoints());

        state.AddLogEndpoint(EffectiveOtlpEndpoint.Http("http://collector:4318/v1/logs"));

        Assert.True(state.ClearLogEndpoints());
        Assert.Empty(state.CreateSnapshot().LogEndpoints);
        Assert.False(state.ClearLogEndpoints());
    }
}
