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
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class EffectiveConfigStateTests
{
    [Fact]
    public void BuildPayload_IncludesSelectedSplunkSettings()
    {
        var configuration = new NameValueCollection
        {
            { ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled, "true" },
            { ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled, "true" },
            { ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval, "10000" },
            { ConfigurationKeys.Splunk.Snapshots.Enabled, "true" },
            { ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs, "5000" },
            { ConfigurationKeys.Splunk.AccessToken, "secret-token" },
            { ConfigurationKeys.Splunk.Realm, "us0" }
        };
        var settings = new PluginSettings(new NameValueConfigurationSource(configuration));
        var state = new EffectiveConfigState();

        state.SetSplunkSettings(settings);

        var payload = state.BuildPayload();
        Assert.Contains("SPLUNK_PROFILER_ENABLED=true", payload);
#if NET
        Assert.Contains("SPLUNK_PROFILER_MEMORY_ENABLED=true", payload);
#else
        Assert.Contains("SPLUNK_PROFILER_MEMORY_ENABLED=false", payload);
#endif
        Assert.Contains("SPLUNK_PROFILER_CALL_STACK_INTERVAL=\"10000ms\"", payload);
        Assert.Contains("SPLUNK_SNAPSHOT_PROFILER_ENABLED=true", payload);
        Assert.Contains("SPLUNK_SNAPSHOT_SAMPLING_INTERVAL=\"5000ms\"", payload);
        Assert.DoesNotContain("SPLUNK_ACCESS_TOKEN", payload);
        Assert.DoesNotContain("SPLUNK_REALM", payload);
    }

    [Fact]
    public void BuildPayload_FormatsMultipleEndpointValues()
    {
        var state = new EffectiveConfigState();

        state.SetEndpoints(
            EffectiveConfigKeys.TracesEndpoint,
            [
                "http://collector-1:4318/v1/traces",
                "http://collector-2:4318/v1/traces"
            ]);

        Assert.Contains(
            "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT=\"http://collector-1:4318/v1/traces\",\"http://collector-2:4318/v1/traces\"",
            state.BuildPayload());
    }

    [Fact]
    public void BuildPayload_QuotesEndpointValuesContainingCommas()
    {
        var state = new EffectiveConfigState();

        state.SetEndpoints(
            EffectiveConfigKeys.TracesEndpoint,
            [
                "http://collector/path,with%2Ccomma",
                "http://collector/second"
            ]);

        Assert.Contains(
            "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT=\"http://collector/path,with%2Ccomma\",\"http://collector/second\"",
            state.BuildPayload());
    }

    [Fact]
    public void AddEndpoint_ReturnsFalse_WhenEndpointAlreadyExists()
    {
        var state = new EffectiveConfigState();

        var firstAdd = state.AddEndpoint(EffectiveConfigKeys.LogsEndpoint, "http://collector:4318/v1/logs");
        var secondAdd = state.AddEndpoint(EffectiveConfigKeys.LogsEndpoint, "http://collector:4318/v1/logs");

        Assert.True(firstAdd);
        Assert.False(secondAdd);
        Assert.Contains(
            "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=\"http://collector:4318/v1/logs\"",
            state.BuildPayload());
    }

    [Fact]
    public void ClearEndpoints_ReturnsFalse_WhenEndpointKeyIsMissing()
    {
        var state = new EffectiveConfigState();

        Assert.False(state.ClearEndpoints(EffectiveConfigKeys.LogsEndpoint));

        state.AddEndpoint(EffectiveConfigKeys.LogsEndpoint, "http://collector:4318/v1/logs");

        Assert.True(state.ClearEndpoints(EffectiveConfigKeys.LogsEndpoint));
        Assert.False(state.ClearEndpoints(EffectiveConfigKeys.LogsEndpoint));
    }

    [Fact]
    public void TrySetServiceName_KeepsFirstValue()
    {
        var state = new EffectiveConfigState();

        state.TrySetServiceName("first-service");
        state.TrySetServiceName("second-service");

        var payload = state.BuildPayload();
        Assert.Contains("OTEL_SERVICE_NAME=\"first-service\"", payload);
        Assert.DoesNotContain("OTEL_SERVICE_NAME=second-service", payload);
    }

    [Fact]
    public void TrySetServiceName_OmitsValue_WhenValueCannotBeRepresented()
    {
        var state = new EffectiveConfigState();

        state.TrySetServiceName("service\"name");

        Assert.DoesNotContain("OTEL_SERVICE_NAME=", state.BuildPayload());
    }
}
