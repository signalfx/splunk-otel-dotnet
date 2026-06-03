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

using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Model;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Serialization;

internal sealed class EffectiveYamlConfig
{
    [EffectiveYamlProperty("otel_config_file", 0)]
    public string? OtelConfigFile { get; set; }

    [EffectiveYamlProperty("otel_experimental_config_file", 1, plainStyle: true)]
    public string? OtelExperimentalConfigFile { get; set; }

    [EffectiveYamlProperty("tracer_provider", 2)]
    public EffectiveTracerProviderConfig? TracerProvider { get; set; }

    [EffectiveYamlProperty("meter_provider", 3)]
    public EffectiveMeterProviderConfig? MeterProvider { get; set; }

    [EffectiveYamlProperty("logger_provider", 4)]
    public EffectiveLoggerProviderConfig? LoggerProvider { get; set; }

    [EffectiveYamlProperty("distribution", 5)]
    public EffectiveDistributionConfig? Distribution { get; set; }

    public static EffectiveYamlConfig Create(
        string otelConfigFile,
        string? otelExperimentalConfigFile,
        IReadOnlyList<EffectiveOtlpEndpoint> traceEndpoints,
        IReadOnlyList<EffectiveOtlpEndpoint> metricEndpoints,
        IReadOnlyList<EffectiveOtlpEndpoint> logEndpoints,
        bool cpuProfilerEnabled,
        bool memoryProfilerEnabled,
        bool snapshotProfilerEnabled,
        uint cpuProfilerCallStackInterval,
        uint snapshotSamplingInterval)
    {
        var profiling = EffectiveProfilingConfig.Create(
            cpuProfilerEnabled,
            memoryProfilerEnabled,
            snapshotProfilerEnabled,
            cpuProfilerCallStackInterval,
            snapshotSamplingInterval);

        return new EffectiveYamlConfig
        {
            OtelConfigFile = otelConfigFile,
            OtelExperimentalConfigFile = otelExperimentalConfigFile ?? "null",
            TracerProvider = traceEndpoints.Count == 0
                ? null
                : new EffectiveTracerProviderConfig
                {
                    Processors = traceEndpoints
                        .Select(endpoint => new EffectiveBatchProcessorConfig(endpoint))
                        .ToArray()
                },
            MeterProvider = metricEndpoints.Count == 0
                ? null
                : new EffectiveMeterProviderConfig
                {
                    Readers = metricEndpoints
                        .Select(endpoint => new EffectivePeriodicReaderConfig(endpoint))
                        .ToArray()
                },
            LoggerProvider = logEndpoints.Count == 0
                ? null
                : new EffectiveLoggerProviderConfig
                {
                    Processors = logEndpoints
                        .Select(endpoint => new EffectiveBatchProcessorConfig(endpoint))
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

    internal sealed class EffectiveTracerProviderConfig
    {
        [EffectiveYamlProperty("processors", 0)]
        public IReadOnlyList<EffectiveBatchProcessorConfig> Processors { get; set; } = [];
    }

    internal sealed class EffectiveMeterProviderConfig
    {
        [EffectiveYamlProperty("readers", 0)]
        public IReadOnlyList<EffectivePeriodicReaderConfig> Readers { get; set; } = [];
    }

    internal sealed class EffectiveLoggerProviderConfig
    {
        [EffectiveYamlProperty("processors", 0)]
        public IReadOnlyList<EffectiveBatchProcessorConfig> Processors { get; set; } = [];
    }

    internal sealed class EffectiveBatchProcessorConfig
    {
        public EffectiveBatchProcessorConfig(EffectiveOtlpEndpoint endpoint)
        {
            Batch = new EffectiveBatchConfig(endpoint);
        }

        [EffectiveYamlProperty("batch", 0)]
        public EffectiveBatchConfig Batch { get; }
    }

    internal sealed class EffectivePeriodicReaderConfig
    {
        public EffectivePeriodicReaderConfig(EffectiveOtlpEndpoint endpoint)
        {
            Periodic = new EffectivePeriodicConfig(endpoint);
        }

        [EffectiveYamlProperty("periodic", 0)]
        public EffectivePeriodicConfig Periodic { get; }
    }

    internal sealed class EffectiveBatchConfig
    {
        public EffectiveBatchConfig(EffectiveOtlpEndpoint endpoint)
        {
            Exporter = new EffectiveExporterConfig(endpoint);
        }

        [EffectiveYamlProperty("exporter", 0)]
        public EffectiveExporterConfig Exporter { get; }
    }

    internal sealed class EffectivePeriodicConfig
    {
        public EffectivePeriodicConfig(EffectiveOtlpEndpoint endpoint)
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
        public EffectiveCallGraphsConfig? Callgraphs { get; set; }

        public static EffectiveProfilingConfig? Create(
            bool cpuProfilerEnabled,
            bool memoryProfilerEnabled,
            bool snapshotProfilerEnabled,
            uint cpuProfilerCallStackInterval,
            uint snapshotSamplingInterval)
        {
            if (!cpuProfilerEnabled && !memoryProfilerEnabled && !snapshotProfilerEnabled)
            {
                return null;
            }

            return new EffectiveProfilingConfig
            {
                AlwaysOn = !cpuProfilerEnabled && !memoryProfilerEnabled
                    ? null
                    : new EffectiveAlwaysOnProfilingConfig
                    {
                        CpuProfiler = cpuProfilerEnabled
                            ? new EffectiveCpuProfilerConfig { SamplingInterval = cpuProfilerCallStackInterval }
                            : null,
                        MemoryProfiler = memoryProfilerEnabled ? new EffectiveMemoryProfilerConfig() : null
                    },
                Callgraphs = snapshotProfilerEnabled
                    ? new EffectiveCallGraphsConfig { SamplingInterval = snapshotSamplingInterval }
                    : null
            };
        }
    }

    internal sealed class EffectiveAlwaysOnProfilingConfig
    {
        [EffectiveYamlProperty("cpu_profiler", 0)]
        public EffectiveCpuProfilerConfig? CpuProfiler { get; set; }

        [EffectiveYamlProperty("memory_profiler", 1)]
        public EffectiveMemoryProfilerConfig? MemoryProfiler { get; set; }
    }

    internal sealed class EffectiveCpuProfilerConfig
    {
        [EffectiveYamlProperty("sampling_interval", 0)]
        public uint SamplingInterval { get; set; }
    }

    internal sealed class EffectiveMemoryProfilerConfig
    {
    }

    internal sealed class EffectiveCallGraphsConfig
    {
        [EffectiveYamlProperty("sampling_interval", 0)]
        public uint SamplingInterval { get; set; }
    }
}
