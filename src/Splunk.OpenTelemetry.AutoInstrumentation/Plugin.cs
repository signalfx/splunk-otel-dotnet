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

using System.Runtime.InteropServices;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.Helpers;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;
using Splunk.OpenTelemetry.AutoInstrumentation.Snapshots;

#if NETFRAMEWORK
using OpenTelemetry.Instrumentation.AspNet;
#else
using OpenTelemetry.Instrumentation.AspNetCore;
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

    private static PprofInOtlpLogsExporter? _pprofInOtlpLogsExporter;
    private static int _highResTimerEnabled;
    private static int _highResTimerDisabled;

    private readonly Metrics _metrics = new(Settings);
    private readonly Traces _traces = new(Settings);
    private readonly Sdk _sdk = new();
    private readonly EffectiveConfigValueAccumulator _effectiveConfigValues = new();

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
            try
            {
                var config = EffectiveConfigReader.Read(Settings);
                foreach (var item in config)
                {
                    Log.Debug(EffectiveConfigLog.FormatEntry(item.Key, item.Value));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to read effective configuration.");
            }
        }

        if (
#if NET
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
#endif
            Settings is { SnapshotsEnabled: true, HighResolutionTimerEnabled: true })
        {
            EnableHighResTimer();
        }
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
        AccumulateEffectiveOtlpEndpoint(EffectiveConfigKeys.MetricsEndpoint, options, _effectiveConfigValues);
    }

    /// <summary>
    /// Configure Traces OTLP exporter options.
    /// </summary>
    /// <param name="options">Otlp options.</param>
    public void ConfigureTracesOptions(OtlpExporterOptions options)
    {
        _traces.ConfigureTracesOptions(options);
        AccumulateEffectiveOtlpEndpoint(EffectiveConfigKeys.TracesEndpoint, options, _effectiveConfigValues);
    }

#if NETFRAMEWORK

    /// <summary>
    /// Configures ASP.NET instrumentation options.
    /// </summary>
    /// <param name="options">ASP.NET Trace InstrumentationO options.</param>
    public void ConfigureTracesOptions(AspNetTraceInstrumentationOptions options)
    {
        _traces.ConfigureTracesOptions(options);
    }

#else

    /// <summary>
    /// Configures ASP.NET Core instrumentation options.
    /// </summary>
    /// <param name="options">ASP.NET Core Trace InstrumentationO options.</param>
    public void ConfigureTracesOptions(AspNetCoreTraceInstrumentationOptions options)
    {
        _traces.ConfigureTracesOptions(options);
    }

#endif

    /// <summary>
    /// Configure Continuous Profiler.
    /// </summary>
    /// <returns>(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, continuousProfilerExporter)</returns>
    public Tuple<bool, uint, bool, uint, TimeSpan, TimeSpan, object> GetContinuousProfilerConfiguration()
    {
        var threadSamplingEnabled = Settings.CpuProfilerEnabled;
        var threadSamplingInterval = Settings.CpuProfilerCallStackInterval;
#if NET
        var allocationSamplingEnabled = Settings.MemoryProfilerEnabled;
        var maxMemorySamplesPerMinute = Settings.MemoryProfilerMaxMemorySamplesPerMinute;
#else
        // Allocation sampling is not supported on .NET Framework
        var allocationSamplingEnabled = false;
        var maxMemorySamplesPerMinute = 0u;
#endif
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
            var frequentSamplingInterval = Settings.SnapshotsSamplingInterval;
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
            builder.AddProcessor(new SnapshotSelectingProcessor(SnapshotFilter.Instance, new TraceIdBasedSnapshotSelector(Settings.SnapshotsSelectionRate)));
        }

        return builder;
    }

    /// <summary>
    /// Called when the tracer provider has been initialized.
    /// </summary>
    /// <param name="provider">Tracer provider.</param>
    public void TracerProviderInitialized(TracerProvider provider)
    {
        LogAccumulatedEffectiveConfigValue(EffectiveConfigKeys.TracesEndpoint, _effectiveConfigValues);
    }

    /// <summary>
    /// Called when the meter provider has been initialized.
    /// </summary>
    /// <param name="provider">Meter provider.</param>
    public void MeterProviderInitialized(MeterProvider provider)
    {
        LogAccumulatedEffectiveConfigValue(EffectiveConfigKeys.MetricsEndpoint, _effectiveConfigValues);
    }

    private static void AccumulateEffectiveOtlpEndpoint(
        string configurationKey,
        OtlpExporterOptions options,
        EffectiveConfigValueAccumulator effectiveConfigValues)
    {
        if (!Log.IsDebugEnabled)
        {
            return;
        }

        try
        {
            var endpoint = OtlpEndpointResolver.ResolveFromOptions(options, configurationKey);
            if (endpoint != null)
            {
                // File-based config can create multiple OTLP exporters for the same signal.
                // Keep one env-var-shaped key and append every resolved endpoint value.
                effectiveConfigValues.Add(configurationKey, endpoint);
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to resolve {configurationKey} from OtlpExporterOptions: {ex.Message}");
        }
    }

    private static void LogAccumulatedEffectiveConfigValue(string configurationKey, EffectiveConfigValueAccumulator effectiveConfigValues)
    {
        if (!Log.IsDebugEnabled)
        {
            return;
        }

        var value = effectiveConfigValues.GetValue(configurationKey);
        if (value != null)
        {
            Log.Debug(EffectiveConfigLog.FormatEntry(configurationKey, value));
        }
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
}
