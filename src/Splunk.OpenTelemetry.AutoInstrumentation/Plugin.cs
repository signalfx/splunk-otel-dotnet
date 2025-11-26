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

using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Splunk.OpenTelemetry.AutoInstrumentation.Helpers;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;
#if NET
using Splunk.OpenTelemetry.AutoInstrumentation.Snapshots;
#endif

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
#pragma warning disable SA1401
    internal static Func<PluginSettings> DefaultSettingsFactory = PluginSettings.FromDefaultSources;
#pragma warning restore SA1401
    private static readonly Lazy<PluginSettings> SettingsFactory = new(() => DefaultSettingsFactory());

    private static readonly ILogger Log = new Logger();
#if NET
    private static PprofInOtlpLogsExporter? _pprofInOtlpLogsExporter;
    private static int _highResTimerEnabled;
    private static int _highResTimerDisabled;
#endif

    private readonly Metrics _metrics = new(Settings);
    private readonly Traces _traces = new(Settings);
    private readonly Sdk _sdk = new();

    internal static PluginSettings Settings => SettingsFactory.Value;

    /// <summary>
    /// Configures Sdk
    /// </summary>
    public void Initializing()
    {
        _sdk.Initializing();

        if (Log.IsDebugEnabled)
        {
            Log.LogConfigurationSetup();
        }

#if NET
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            Settings is { SnapshotsEnabled: true, HighResolutionTimerEnabled: true })
        {
            EnableHighResTimer();
        }
#endif
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
    public void ConfigureTracesOptions(AspNetTraceInstrumentationOptions options)
    {
        _traces.ConfigureTracesOptions(options);
    }

#else

    /// <summary>
    /// Configures ASP.NET Core instrumentation options.
    /// </summary>
    /// <param name="options">Otlp options.</param>
    public void ConfigureTracesOptions(AspNetCoreTraceInstrumentationOptions options)
    {
        _traces.ConfigureTracesOptions(options);
        if (Settings.SnapshotsEnabled)
        {
            // This is needed because Baggage.Current is set by instrumentation after activity is started.
            options.EnrichWithHttpRequest += (activity, _) =>
            {
                if (!SnapshotVolumeDetector.IsLoud(Baggage.Current))
                {
                    return;
                }

                if (activity.IsEntry())
                {
                    activity.MarkLoud();
                }

                SnapshotFilter.Instance.Add(activity);
            };
        }
    }

#endif

#if NET
    /// <summary>
    /// Configure Continuous Profiler.
    /// </summary>
    /// <returns>(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, continuousProfilerExporter)</returns>
    public Tuple<bool, uint, bool, uint, TimeSpan, TimeSpan, object> GetContinuousProfilerConfiguration()
    {
        var threadSamplingEnabled = Settings.CpuProfilerEnabled;
        var threadSamplingInterval = Settings.CpuProfilerCallStackInterval;
        var allocationSamplingEnabled = Settings.MemoryProfilerEnabled;
        var maxMemorySamplesPerMinute = Settings.MemoryProfilerMaxMemorySamplesPerMinute;
        var exportInterval = TimeSpan.FromMilliseconds(Settings.ProfilerExportInterval);
        var exportTimeout = TimeSpan.FromMilliseconds(Settings.ProfilerHttpClientTimeout);

        var pprofInOtlpLogsExporter = GetPprofInOtlpLogsExporter();
        pprofInOtlpLogsExporter.SampleProcessor.ContinuousSamplingPeriod = threadSamplingInterval;

        return Tuple.Create(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, exportTimeout, (object)pprofInOtlpLogsExporter);
    }

    /// <summary>
    /// Returns selective sampling configuration.
    /// </summary>
    /// <returns>(frequentSamplingInterval, exportInterval, exportTimeout, pprofInOtlpLogsExporter) or null.</returns>
    public Tuple<uint, TimeSpan, TimeSpan, object?>? GetSelectiveSamplingConfiguration()
    {
        if (Settings.SnapshotsEnabled)
        {
            var frequentSamplingInterval = (uint)Settings.SnapshotsSamplingInterval;
            var pprofInOtlpLogsExporter = GetPprofInOtlpLogsExporter();
            pprofInOtlpLogsExporter.SampleProcessor.SelectedSamplingPeriod = frequentSamplingInterval;
            var exportInterval = GetSampleExportInterval();
            var exportTimeout = GetSampleExportTimeout();
            return Tuple.Create(frequentSamplingInterval, exportInterval, exportTimeout, (object)pprofInOtlpLogsExporter)!;
        }

        return null;
    }

    /// <summary>
    /// Modify SDK config.
    /// </summary>
    /// <param name="builder">TracerProviderBuilder instance to customize.</param>
    /// <returns>TracerProviderBuilder instance for chaining.</returns>
    public TracerProviderBuilder BeforeConfigureTracerProvider(TracerProviderBuilder builder)
    {
        if (Settings.SnapshotsEnabled)
        {
            var currentPropagator = Propagators.DefaultTextMapPropagator;
            // Ensure baggage propagator is configured.
            if (currentPropagator.Fields == null || !currentPropagator.Fields.Contains("baggage"))
            {
                // This will make SDK init fail. Native side sampling loop will already be started,
                // but as no spans will be selected, no snapshots will be collected.
                throw new NotSupportedException("Collecting snapshots requires baggage propagator usage.");
            }

            global::OpenTelemetry.Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator([currentPropagator, new SnapshotVolumePropagator(new CompositeSelector(Settings.SnapshotsSelectionRate))]));
            builder.AddProcessor(new SnapshotSelectingProcessor());
        }

        return builder;
    }

    private static void EnableHighResTimer()
    {
        if (Interlocked.Exchange(ref _highResTimerEnabled, value: 1) != 0)
        {
            // Timer already enabled
            return;
        }

        if (!WinApi.TryEnableHighResolutionTimer())
        {
            return;
        }

        AppDomain.CurrentDomain.ProcessExit += DisableHighResTimer;
        AppDomain.CurrentDomain.DomainUnload += DisableHighResTimer;
    }

    private static void DisableHighResTimer(object? o, EventArgs args)
    {
        if (Interlocked.Exchange(ref _highResTimerDisabled, value: 1) != 0)
        {
            // Timer already disabled
            return;
        }

        WinApi.TryDisableHighResolutionTimer();
    }

    private static TimeSpan GetSampleExportTimeout()
    {
        return TimeSpan.FromMilliseconds(Settings.ProfilerHttpClientTimeout);
    }

    private static TimeSpan GetSampleExportInterval()
    {
        return TimeSpan.FromMilliseconds(Settings.ProfilerExportInterval);
    }

    private static PprofInOtlpLogsExporter GetPprofInOtlpLogsExporter()
    {
        _pprofInOtlpLogsExporter ??= CreatePprofInOtlpLogsExporter();
        return _pprofInOtlpLogsExporter;
    }

    private static PprofInOtlpLogsExporter CreatePprofInOtlpLogsExporter()
    {
        return new PprofInOtlpLogsExporter(new SampleProcessor(), new SampleExporter(new OtlpHttpLogSender(Settings.ProfilerLogsEndpoint)), new NativeFormatParser(Settings.SnapshotsEnabled));
    }
#endif
}
