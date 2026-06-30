// <copyright file="EffectiveYamlConfig.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Serialization;

internal sealed class EffectiveYamlConfig
{
    [EffectiveYamlProperty("otel_config_file", 0)]
    public string? OtelConfigFile { get; set; }

    [EffectiveYamlProperty("otel_experimental_config_file", 1, plainStyle: true)]
    public string? OtelExperimentalConfigFile { get; set; }

    [EffectiveYamlProperty("tracer_provider", 2)]
    public EffectiveProcessorProviderConfig? TracerProvider { get; set; }

    [EffectiveYamlProperty("meter_provider", 3)]
    public EffectiveMeterProviderConfig? MeterProvider { get; set; }

    [EffectiveYamlProperty("logger_provider", 4)]
    public EffectiveProcessorProviderConfig? LoggerProvider { get; set; }

    [EffectiveYamlProperty("distribution", 5)]
    public EffectiveDistributionConfig? Distribution { get; set; }

    public static EffectiveYamlConfig Create(EffectiveConfigSnapshot snapshot)
    {
        var profiling = EffectiveProfilingConfig.Create(snapshot);

        return new EffectiveYamlConfig
        {
            OtelConfigFile = snapshot.FileBasedConfigFileName,
            OtelExperimentalConfigFile = snapshot.OtelExperimentalConfigFile ?? "null",
            TracerProvider = snapshot.TraceEndpoints.Count == 0
                ? null
                : new EffectiveProcessorProviderConfig
                {
                    Processors = snapshot.TraceEndpoints
                        .Select(endpoint => new EffectiveProcessorConfig(endpoint))
                        .ToArray()
                },
            MeterProvider = snapshot.MetricEndpoints.Count == 0
                ? null
                : new EffectiveMeterProviderConfig
                {
                    Readers = snapshot.MetricEndpoints
                        .Select(endpoint => new EffectivePeriodicReaderConfig(endpoint))
                        .ToArray()
                },
            LoggerProvider = snapshot.LogEndpoints.Count == 0
                ? null
                : new EffectiveProcessorProviderConfig
                {
                    Processors = snapshot.LogEndpoints
                        .Select(endpoint => new EffectiveProcessorConfig(endpoint))
                        .ToArray()
                },
            Distribution = profiling == null
                ? null
                : new EffectiveDistributionConfig
                {
                    Splunk = new EffectiveSplunkConfig
                    {
                        Profiling = profiling
                    }
                }
        };
    }

    internal sealed class EffectiveProcessorProviderConfig
    {
        [EffectiveYamlProperty("processors", 0)]
        public IReadOnlyList<EffectiveProcessorConfig> Processors { get; set; } = [];
    }

    internal sealed class EffectiveMeterProviderConfig
    {
        [EffectiveYamlProperty("readers", 0)]
        public IReadOnlyList<EffectivePeriodicReaderConfig> Readers { get; set; } = [];
    }

    internal sealed class EffectiveProcessorConfig
    {
        public EffectiveProcessorConfig(EffectiveOtlpEndpoint endpoint)
        {
            switch (endpoint.PipelineType)
            {
                case EffectiveOtlpPipelineType.Batch:
                    Batch = new EffectiveExporterPipelineConfig(endpoint);
                    break;
                case EffectiveOtlpPipelineType.Simple:
                    Simple = new EffectiveExporterPipelineConfig(endpoint);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint.PipelineType, "Unknown processor type.");
            }
        }

        [EffectiveYamlProperty("batch", 0)]
        public EffectiveExporterPipelineConfig? Batch { get; }

        [EffectiveYamlProperty("simple", 1)]
        public EffectiveExporterPipelineConfig? Simple { get; }
    }

    internal sealed class EffectivePeriodicReaderConfig
    {
        public EffectivePeriodicReaderConfig(EffectiveOtlpEndpoint endpoint)
        {
            if (endpoint.PipelineType != EffectiveOtlpPipelineType.Periodic)
            {
                throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint.PipelineType, "Unknown metric reader type.");
            }

            Periodic = new EffectiveExporterPipelineConfig(endpoint);
        }

        [EffectiveYamlProperty("periodic", 0)]
        public EffectiveExporterPipelineConfig Periodic { get; }
    }

    internal sealed class EffectiveExporterPipelineConfig
    {
        public EffectiveExporterPipelineConfig(EffectiveOtlpEndpoint endpoint)
        {
            Exporter = new EffectiveExporterConfig(endpoint);
        }

        [EffectiveYamlProperty("exporter", 0)]
        public EffectiveExporterConfig Exporter { get; }
    }

    internal sealed class EffectiveExporterConfig
    {
        public EffectiveExporterConfig(EffectiveOtlpEndpoint endpoint)
        {
            switch (endpoint.ExporterType)
            {
                case EffectiveOtlpExporterType.HttpProtobuf:
                    OtlpHttp = new EffectiveOtlpExporterConfig(endpoint.Endpoint);
                    break;
                case EffectiveOtlpExporterType.Grpc:
                    OtlpGrpc = new EffectiveOtlpExporterConfig(endpoint.Endpoint);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint.ExporterType, "Unknown OTLP exporter type.");
            }
        }

        [EffectiveYamlProperty("otlp_http", 0)]
        public EffectiveOtlpExporterConfig? OtlpHttp { get; }

        [EffectiveYamlProperty("otlp_grpc", 1)]
        public EffectiveOtlpExporterConfig? OtlpGrpc { get; }
    }

    internal sealed class EffectiveOtlpExporterConfig
    {
        public EffectiveOtlpExporterConfig(string endpoint)
        {
            Endpoint = endpoint;
        }

        [EffectiveYamlProperty("endpoint", 0)]
        public string Endpoint { get; }
    }

    internal sealed class EffectiveDistributionConfig
    {
        [EffectiveYamlProperty("splunk", 0)]
        public EffectiveSplunkConfig? Splunk { get; set; }
    }

    internal sealed class EffectiveSplunkConfig
    {
        [EffectiveYamlProperty("profiling", 0)]
        public EffectiveProfilingConfig? Profiling { get; set; }
    }

    internal sealed class EffectiveProfilingConfig
    {
        [EffectiveYamlProperty("always_on", 0)]
        public EffectiveAlwaysOnProfilingConfig? AlwaysOn { get; set; }

        [EffectiveYamlProperty("callgraphs", 1)]
        public EffectiveSamplingIntervalConfig? Callgraphs { get; set; }

        public static EffectiveProfilingConfig? Create(EffectiveConfigSnapshot snapshot)
        {
            if (!snapshot.CpuProfilerEnabled &&
                !snapshot.MemoryProfilerEnabled &&
                !snapshot.SnapshotProfilerEnabled)
            {
                return null;
            }

            EffectiveAlwaysOnProfilingConfig? alwaysOn = null;
            if (snapshot.CpuProfilerEnabled || snapshot.MemoryProfilerEnabled)
            {
                alwaysOn = snapshot.MemoryProfilerEnabled
                    ? new EffectiveAlwaysOnProfilingWithMemoryConfig()
                    : new EffectiveAlwaysOnProfilingConfig();
                alwaysOn.CpuProfiler = snapshot.CpuProfilerEnabled
                    ? new EffectiveSamplingIntervalConfig { SamplingInterval = snapshot.CpuProfilerCallStackInterval }
                    : null;
            }

            return new EffectiveProfilingConfig
            {
                AlwaysOn = alwaysOn,
                Callgraphs = snapshot.SnapshotProfilerEnabled
                    ? new EffectiveSamplingIntervalConfig { SamplingInterval = snapshot.SnapshotSamplingInterval }
                    : null
            };
        }
    }

    internal class EffectiveAlwaysOnProfilingConfig
    {
        [EffectiveYamlProperty("cpu_profiler", 0)]
        public EffectiveSamplingIntervalConfig? CpuProfiler { get; set; }
    }

    internal sealed class EffectiveAlwaysOnProfilingWithMemoryConfig : EffectiveAlwaysOnProfilingConfig
    {
        [EffectiveYamlProperty("memory_profiler", 1, preserveNull: true)]
        public EffectiveMemoryProfilerConfig? MemoryProfiler => null;
    }

    internal sealed class EffectiveSamplingIntervalConfig
    {
        [EffectiveYamlProperty("sampling_interval", 0)]
        public uint SamplingInterval { get; set; }
    }

    internal sealed class EffectiveMemoryProfilerConfig
    {
    }
}
