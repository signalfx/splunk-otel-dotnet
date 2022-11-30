﻿// <copyright file="Plugin.cs" company="Splunk Inc.">
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

using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Splunk OTel plugin
/// </summary>
public class Plugin
{
    private readonly Metrics _metrics = new();
    private readonly Traces _traces = new();
    private readonly Logs _logs = new();

    /// <summary>
    /// Configures Metrics
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> to configure</param>
    /// <returns>Returns <see cref="MeterProviderBuilder"/> for chaining.</returns>
    public MeterProviderBuilder ConfigureMeterProvider(MeterProviderBuilder builder)
    {
        return _metrics.ConfigureMeterProvider(builder);
    }

    /// <summary>
    /// Configure metrics OTLP exporter options
    /// </summary>
    /// <param name="options">Otlp options</param>
    public void ConfigureMetricsOptions(OtlpExporterOptions options)
    {
        _metrics.ConfigureMetricsOptions(options);
    }

    /// <summary>
    /// Configures Traces
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> to configure</param>
    /// <returns>Returns <see cref="TracerProviderBuilder"/> for chaining.</returns>
    public TracerProviderBuilder ConfigureTracerProvider(TracerProviderBuilder builder)
    {
        return _traces.ConfigureTracerProvider(builder);
    }

    /// <summary>
    /// Configure Traces OTLP exporter options
    /// </summary>
    /// <param name="options">Otlp options</param>
    public void ConfigureTracesOptions(OtlpExporterOptions options)
    {
        _traces.ConfigureTracesOptions(options);
    }

    /// <summary>
    /// Configure metrics OpenTelemetryLoggerOptions options
    /// </summary>
    /// <param name="options"><see cref="OpenTelemetryLoggerOptions"/> to configure</param>
    public void ConfigureLogsOptions(OpenTelemetryLoggerOptions options)
    {
        _logs.ConfigureLogsOptions(options);
    }
}
