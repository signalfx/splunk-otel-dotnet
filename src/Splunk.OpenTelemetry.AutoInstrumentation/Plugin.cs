// <copyright file="Plugin.cs" company="Splunk Inc.">
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
using OpenTelemetry.Resources;
#if NETFRAMEWORK
using OpenTelemetry.Instrumentation.AspNet;
#else
using OpenTelemetry.Instrumentation.AspNetCore;
using Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;
#endif

namespace Splunk.OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Splunk OTel plugin
/// </summary>
public class Plugin
{
    private static readonly PluginSettings Settings = PluginSettings.FromDefaultSources();

    private readonly Metrics _metrics = new(Settings);
    private readonly Traces _traces = new(Settings);
    private readonly Sdk _sdk = new();

    /// <summary>
    /// Configures Sdk
    /// </summary>
    public void Initializing()
    {
        _sdk.Initializing();
    }

    /// <summary>
    /// Configure Resource Builder for Logs, Metrics and Traces
    /// </summary>
    /// <param name="builder"><see cref="ResourceBuilder"/> to configure</param>
    /// <returns>>Returns <see cref="ResourceBuilder"/> for chaining.</returns>
    public ResourceBuilder ConfigureResource(ResourceBuilder builder)
    {
        ResourceConfigurator.Configure(builder, Settings);
        return builder;
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
    /// Configure Traces OTLP exporter options.
    /// </summary>
    /// <param name="options">Otlp options.</param>
    public void ConfigureTracesOptions(OtlpExporterOptions options)
    {
        _traces.ConfigureTracesOptions(options);
    }

#if NETFRAMEWORK

    /// <summary>
    /// Configures ASP.NET instrumentation options.
    /// </summary>
    /// <param name="options">Otlp options.</param>
    public void ConfigureTracesOptions(AspNetInstrumentationOptions options)
    {
        _traces.ConfigureTracesOptions(options);
    }

#else

    /// <summary>
    /// Configures ASP.NET Core instrumentation options.
    /// </summary>
    /// <param name="options">Otlp options.</param>
    public void ConfigureTracesOptions(AspNetCoreInstrumentationOptions options)
    {
        _traces.ConfigureTracesOptions(options);
    }

#endif

#if NET6_0_OR_GREATER
    /// <summary>
    /// Configure Continuous Profiler.
    /// </summary>
    /// <returns>(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, continuousProfilerExporter)</returns>
    public Tuple<bool, uint, bool, uint, TimeSpan, object> GetContinuousProfilerConfiguration()
    {
        var threadSamplingEnabled = Settings.CpuProfilerEnabled;
        var threadSamplingInterval = Settings.CpuProfilerCallStackInterval;
        var allocationSamplingEnabled = Settings.MemoryProfilerEnabled;
        const uint maxMemorySamplesPerMinute = 200u;
        var exportInterval = TimeSpan.FromMilliseconds(500); // it is half of the shortest possible thread sampling interval

        var sampleProcessor = new SampleProcessor(TimeSpan.FromMilliseconds(threadSamplingInterval));

        var logSender = new OtlpHttpLogSender(Settings.ProfilerLogsEndpoint);

        var sampleExporter = new SampleExporter(logSender);

        object continuousProfilerExporter = new PprofInOtlpLogsExporter(sampleProcessor, sampleExporter);

        return Tuple.Create(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, continuousProfilerExporter);
    }
#endif
}
