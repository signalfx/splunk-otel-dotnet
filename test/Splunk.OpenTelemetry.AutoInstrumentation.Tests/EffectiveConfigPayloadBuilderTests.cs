// <copyright file="EffectiveConfigPayloadBuilderTests.cs" company="Splunk Inc.">
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

using System.Text;
using OpenTelemetry.OpAmp.Client.Messages;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class EffectiveConfigPayloadBuilderTests
{
    [Fact]
    public void Build_EnvironmentConfig_UsesDataModelProperties()
    {
        var snapshot = CreateEnvironmentSnapshot(
            traceEndpoints: [EffectiveOtlpEndpoint.Http("http://collector:4318/v1/traces")],
            metricEndpoints: [EffectiveOtlpEndpoint.Http("http://collector:4318/v1/metrics")],
            logEndpoints: [EffectiveOtlpEndpoint.Http("http://collector:4318/v1/logs")],
            cpuProfilerEnabled: true,
#if NET
            memoryProfilerEnabled: true,
#else
            memoryProfilerEnabled: false,
#endif
            snapshotProfilerEnabled: true,
            cpuProfilerCallStackInterval: 10000,
            snapshotSamplingInterval: 5000);

        var payload = EffectiveConfigPayloadBuilder.Build(snapshot);

        Assert.Equal("environment", payload.FileName);
        Assert.Equal("text/plain; format=properties; vendor=splunk; v=1.0.0", payload.ContentType);
        AssertEnvironmentConfigPayload(
            payload,
            new Dictionary<string, string>
            {
                ["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"] = "http://collector:4318/v1/traces",
                ["OTEL_EXPORTER_OTLP_METRICS_ENDPOINT"] = "http://collector:4318/v1/metrics",
                ["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"] = "http://collector:4318/v1/logs",
                ["SPLUNK_PROFILER_ENABLED"] = "true",
#if NET
                ["SPLUNK_PROFILER_MEMORY_ENABLED"] = "true",
#else
                ["SPLUNK_PROFILER_MEMORY_ENABLED"] = "false",
#endif
                ["SPLUNK_SNAPSHOT_PROFILER_ENABLED"] = "true",
                ["SPLUNK_SNAPSHOT_PROFILER_SAMPLING_INTERVAL"] = "5000",
                ["SPLUNK_PROFILER_CALL_STACK_INTERVAL"] = "10000",
                ["OTEL_CONFIG_FILE"] = "null"
            });
    }

    [Fact]
    public void Build_EnvironmentConfig_IncludesDefaultValues()
    {
        var payload = EffectiveConfigPayloadBuilder.Build(CreateEnvironmentSnapshot());

        AssertEnvironmentConfigPayload(payload, new Dictionary<string, string>
        {
            ["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"] = "none",
            ["OTEL_EXPORTER_OTLP_METRICS_ENDPOINT"] = "none",
            ["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"] = "none",
            ["SPLUNK_PROFILER_ENABLED"] = "false",
            ["SPLUNK_PROFILER_MEMORY_ENABLED"] = "false",
            ["SPLUNK_SNAPSHOT_PROFILER_ENABLED"] = "false",
            ["SPLUNK_SNAPSHOT_PROFILER_SAMPLING_INTERVAL"] = "40",
            ["SPLUNK_PROFILER_CALL_STACK_INTERVAL"] = "10000",
            ["OTEL_CONFIG_FILE"] = "null"
        });
    }

    [Fact]
    public void Build_EnvironmentConfig_RedactsEndpointCredentials()
    {
        var payload = EffectiveConfigPayloadBuilder.Build(CreateEnvironmentSnapshot(
            traceEndpoints: [EffectiveOtlpEndpoint.Http("https://user:password@collector:4318/v1/traces")],
            metricEndpoints: [EffectiveOtlpEndpoint.Http("https://collector:4318/v1/metrics?api_key=metric-secret")],
            logEndpoints: [EffectiveOtlpEndpoint.Http("https://collector:4318/v1/logs#log-secret")]));

        var actual = ParseEnvironmentConfigPayload(GetBody(payload));

        Assert.Equal("https://collector:4318/v1/traces", actual["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"]);
        Assert.Equal("https://collector:4318/v1/metrics", actual["OTEL_EXPORTER_OTLP_METRICS_ENDPOINT"]);
        Assert.Equal("https://collector:4318/v1/logs", actual["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"]);
    }

    [Fact]
    public void Build_EnvironmentConfig_RejectsMultipleEndpointsForAnySignal()
    {
        var endpoints = new[]
        {
            EffectiveOtlpEndpoint.Http("http://collector-1:4318"),
            EffectiveOtlpEndpoint.Http("http://collector-2:4318")
        };
        var snapshots = new[]
        {
            CreateEnvironmentSnapshot(traceEndpoints: endpoints),
            CreateEnvironmentSnapshot(metricEndpoints: endpoints),
            CreateEnvironmentSnapshot(logEndpoints: endpoints)
        };

        foreach (var snapshot in snapshots)
        {
            var exception = Assert.Throws<InvalidOperationException>(
                () => EffectiveConfigPayloadBuilder.Build(snapshot));

            Assert.Contains("cannot represent 2 active", exception.Message);
        }
    }

    [Fact]
    public void CreateFile_RejectsPayloadOverSizeLimit()
    {
        var oversizedBody = new string('a', EffectiveConfigLimits.MaxPayloadSizeBytes + 1);

        var exception = Assert.Throws<InvalidOperationException>(
            () => EffectiveConfigPayloadBuilder.CreateFile("config.yaml", "application/yaml", oversizedBody));

        Assert.Contains(EffectiveConfigLimits.MaxPayloadSizeBytes.ToString(), exception.Message);
    }

    private static EffectiveConfigSnapshot CreateEnvironmentSnapshot(
        IReadOnlyList<EffectiveOtlpEndpoint>? traceEndpoints = null,
        IReadOnlyList<EffectiveOtlpEndpoint>? metricEndpoints = null,
        IReadOnlyList<EffectiveOtlpEndpoint>? logEndpoints = null,
        bool cpuProfilerEnabled = false,
        bool memoryProfilerEnabled = false,
        bool snapshotProfilerEnabled = false,
        uint cpuProfilerCallStackInterval = 10000,
        uint snapshotSamplingInterval = 40)
    {
        return new EffectiveConfigSnapshot(
            fileBasedConfigFileName: null,
            traceEndpoints: traceEndpoints ?? [],
            metricEndpoints: metricEndpoints ?? [],
            logEndpoints: logEndpoints ?? [],
            cpuProfilerEnabled: cpuProfilerEnabled,
            memoryProfilerEnabled: memoryProfilerEnabled,
            snapshotProfilerEnabled: snapshotProfilerEnabled,
            cpuProfilerCallStackInterval: cpuProfilerCallStackInterval,
            snapshotSamplingInterval: snapshotSamplingInterval);
    }

    private static string GetBody(EffectiveConfigFile file)
    {
        return Encoding.UTF8.GetString(file.Content.ToArray());
    }

    private static void AssertEnvironmentConfigPayload(
        EffectiveConfigFile file,
        IReadOnlyDictionary<string, string> expected)
    {
        var actual = ParseEnvironmentConfigPayload(GetBody(file));

        Assert.Equal(expected.Keys.OrderBy(key => key, StringComparer.Ordinal), actual.Keys.OrderBy(key => key, StringComparer.Ordinal));
        foreach (var entry in expected)
        {
            Assert.Equal(entry.Value, actual[entry.Key]);
        }
    }

    private static IReadOnlyDictionary<string, string> ParseEnvironmentConfigPayload(string body)
    {
        if (body.EndsWith("\n", StringComparison.Ordinal))
        {
            body = body.Substring(0, body.Length - 1);
        }

        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in body.Split('\n'))
        {
            Assert.False(string.IsNullOrEmpty(line), "Environment effective config payload must not contain blank lines.");

            var separatorIndex = line.IndexOf('=');
            Assert.True(separatorIndex > 0, $"Environment effective config line must be in key=value form: '{line}'.");

            var key = line.Substring(0, separatorIndex);
            var value = line.Substring(separatorIndex + 1);
            Assert.False(entries.ContainsKey(key), $"Environment effective config contains duplicate key '{key}'.");
            entries.Add(key, value);
        }

        return entries;
    }
}
