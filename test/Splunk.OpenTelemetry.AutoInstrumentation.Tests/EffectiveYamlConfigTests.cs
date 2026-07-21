// <copyright file="EffectiveYamlConfigTests.cs" company="Splunk Inc.">
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

using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Serialization;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class EffectiveYamlConfigTests
{
    [Fact]
    public void Create_CapturesConfigFileMetadata()
    {
        var config = EffectiveYamlConfig.Create(CreateSnapshot());

        Assert.Equal("stable.yaml", config.OtelConfigFile);
    }

    [Fact]
    public void Create_SerializesHttpEndpointAsOtlpHttp()
    {
        var config = EffectiveYamlConfig.Create(CreateSnapshot(
            traceEndpoints: [EffectiveOtlpEndpoint.Http("http://collector:4318/v1/traces")]));

        var processor = config.TracerProvider!.Processors.Single();
        var exporter = processor.Batch!.Exporter;
        Assert.Null(processor.Simple);
        Assert.Equal("http://collector:4318/v1/traces", exporter.OtlpHttp!.Endpoint);
        Assert.Null(exporter.OtlpGrpc);
    }

    [Fact]
    public void Create_PreservesSimpleProcessorType()
    {
        var config = EffectiveYamlConfig.Create(CreateSnapshot(
            traceEndpoints:
            [
                EffectiveOtlpEndpoint.Http(
                    "http://collector:4318/v1/traces",
                    EffectiveOtlpPipelineType.Simple)
            ]));

        var processor = config.TracerProvider!.Processors.Single();
        Assert.Null(processor.Batch);
        Assert.Equal(
            "http://collector:4318/v1/traces",
            processor.Simple!.Exporter.OtlpHttp!.Endpoint);
    }

    [Fact]
    public void Create_SerializesGrpcEndpointAsOtlpGrpc()
    {
        var config = EffectiveYamlConfig.Create(CreateSnapshot(
            metricEndpoints:
            [
                EffectiveOtlpEndpoint.Grpc(
                    "http://collector:4317/opentelemetry.proto.collector.metrics.v1.MetricsService/Export",
                    EffectiveOtlpPipelineType.Periodic)
            ]));

        var exporter = config.MeterProvider!.Readers.Single().Periodic.Exporter;
        Assert.Null(exporter.OtlpHttp);
        Assert.Equal(
            "http://collector:4317/opentelemetry.proto.collector.metrics.v1.MetricsService/Export",
            exporter.OtlpGrpc!.Endpoint);
    }

    [Fact]
    public void Create_RetainsEndpointsWithSameRedactedValueWithoutDisclosingSecrets()
    {
        var config = EffectiveYamlConfig.Create(CreateSnapshot(
            traceEndpoints:
            [
                EffectiveOtlpEndpoint.Http("https://first:secret@collector:4318/v1/traces?api_key=first"),
                EffectiveOtlpEndpoint.Http("https://second:secret@collector:4318/v1/traces?api_key=second")
            ]));

        var endpoints = config.TracerProvider!.Processors
            .Select(processor => processor.Batch!.Exporter.OtlpHttp!.Endpoint)
            .ToArray();
        var reportedValues = string.Join("\n", endpoints);

        Assert.Equal(2, endpoints.Length);
        Assert.All(endpoints, endpoint => Assert.Equal("https://collector:4318/v1/traces", endpoint));
        Assert.DoesNotContain("first:secret", reportedValues);
        Assert.DoesNotContain("second:secret", reportedValues);
        Assert.DoesNotContain("api_key", reportedValues);
    }

    [Fact]
    public void BoundedStringWriter_StopsAtCharacterAllocationGuard()
    {
        using var writer = new EffectiveYamlSerializer.BoundedStringWriter(4);
        writer.Write("abcd");

        var exception = Assert.Throws<InvalidOperationException>(() => writer.Write('e'));

        Assert.Contains("4-character", exception.Message);
    }

    [Fact]
    public void Create_UsesNullMemoryProfilerMarkerWhenEnabled()
    {
        var config = EffectiveYamlConfig.Create(CreateSnapshot(memoryProfilerEnabled: true));

        var alwaysOn = Assert.IsType<EffectiveYamlConfig.EffectiveAlwaysOnProfilingWithMemoryConfig>(
            config.Distribution!.Splunk!.Profiling!.AlwaysOn);
        Assert.Null(alwaysOn.MemoryProfiler);
    }

    [Fact]
    public void Create_OmitsMemoryProfilerMarkerWhenDisabled()
    {
        var config = EffectiveYamlConfig.Create(CreateSnapshot(cpuProfilerEnabled: true));

        Assert.IsType<EffectiveYamlConfig.EffectiveAlwaysOnProfilingConfig>(
            config.Distribution!.Splunk!.Profiling!.AlwaysOn);
    }

    private static EffectiveConfigSnapshot CreateSnapshot(
        IReadOnlyList<EffectiveOtlpEndpoint>? traceEndpoints = null,
        IReadOnlyList<EffectiveOtlpEndpoint>? metricEndpoints = null,
        bool cpuProfilerEnabled = false,
        bool memoryProfilerEnabled = false)
    {
        return new EffectiveConfigSnapshot(
            fileBasedConfigFileName: "stable.yaml",
            traceEndpoints: traceEndpoints ?? [],
            metricEndpoints: metricEndpoints ?? [],
            logEndpoints: [],
            cpuProfilerEnabled: cpuProfilerEnabled,
            memoryProfilerEnabled: memoryProfilerEnabled,
            snapshotProfilerEnabled: false,
            cpuProfilerCallStackInterval: 10000,
            snapshotSamplingInterval: 40);
    }
}
