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

using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Model;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Serialization;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class EffectiveYamlConfigTests
{
    [Fact]
    public void Create_CapturesConfigFileMetadata()
    {
        var config = EffectiveYamlConfig.Create(
            otelConfigFile: "stable.yaml",
            otelExperimentalConfigFile: "experimental.yaml",
            traceEndpoints: [],
            metricEndpoints: [],
            logEndpoints: [],
            cpuProfilerEnabled: false,
            memoryProfilerEnabled: false,
            snapshotProfilerEnabled: false,
            cpuProfilerCallStackInterval: 10000,
            snapshotSamplingInterval: 40);

        Assert.Equal("stable.yaml", config.OtelConfigFile);
        Assert.Equal("experimental.yaml", config.OtelExperimentalConfigFile);
    }

    [Fact]
    public void Create_UsesYamlNullForMissingExperimentalConfigFile()
    {
        var config = EffectiveYamlConfig.Create(
            otelConfigFile: "stable.yaml",
            otelExperimentalConfigFile: null,
            traceEndpoints: [],
            metricEndpoints: [],
            logEndpoints: [],
            cpuProfilerEnabled: false,
            memoryProfilerEnabled: false,
            snapshotProfilerEnabled: false,
            cpuProfilerCallStackInterval: 10000,
            snapshotSamplingInterval: 40);

        Assert.Equal("null", config.OtelExperimentalConfigFile);
    }

    [Fact]
    public void Create_SerializesHttpEndpointAsOtlpHttp()
    {
        var config = EffectiveYamlConfig.Create(
            otelConfigFile: "stable.yaml",
            otelExperimentalConfigFile: null,
            traceEndpoints: [EffectiveOtlpEndpoint.Http("http://collector:4318/v1/traces")],
            metricEndpoints: [],
            logEndpoints: [],
            cpuProfilerEnabled: false,
            memoryProfilerEnabled: false,
            snapshotProfilerEnabled: false,
            cpuProfilerCallStackInterval: 10000,
            snapshotSamplingInterval: 40);

        var exporter = config.TracerProvider!.Processors.Single().Batch.Exporter;
        Assert.Equal("http://collector:4318/v1/traces", exporter.OtlpHttp!.Endpoint);
        Assert.Null(exporter.OtlpGrpc);
    }

    [Fact]
    public void Create_SerializesGrpcEndpointAsOtlpGrpc()
    {
        var config = EffectiveYamlConfig.Create(
            otelConfigFile: "stable.yaml",
            otelExperimentalConfigFile: null,
            traceEndpoints: [],
            metricEndpoints: [EffectiveOtlpEndpoint.Grpc("http://collector:4317/opentelemetry.proto.collector.metrics.v1.MetricsService/Export")],
            logEndpoints: [],
            cpuProfilerEnabled: false,
            memoryProfilerEnabled: false,
            snapshotProfilerEnabled: false,
            cpuProfilerCallStackInterval: 10000,
            snapshotSamplingInterval: 40);

        var exporter = config.MeterProvider!.Readers.Single().Periodic.Exporter;
        Assert.Null(exporter.OtlpHttp);
        Assert.Equal(
            "http://collector:4317/opentelemetry.proto.collector.metrics.v1.MetricsService/Export",
            exporter.OtlpGrpc!.Endpoint);
    }
}
