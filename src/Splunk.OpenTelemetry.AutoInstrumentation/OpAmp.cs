// <copyright file="OpAmp.cs" company="Splunk Inc.">
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
using OpenTelemetry.Metrics;
using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;
using OpenTelemetry.Trace;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Resolvers;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;
using Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal sealed class OpAmp
{
    private static readonly ILogger Log = new Logger();

    private readonly Lazy<EffectiveConfigReporter?> _effectiveConfigReporter;
    private readonly OpAmpRemoteConfigurationListener _remoteConfigurationListener;
    private readonly Func<EffectiveProfilerFeatures> _profilerStateResolver;
    private int _effectiveConfigReportingEnabled;
    private int _remoteConfigurationEnabled;
    private int _instrumentationInitialized;
    private int _opAmpClientStarted;
    private int _initialEffectiveConfigReportStarted;
    private Task? _initialEffectiveConfigReportTask;
    private OpAmpClient? _opAmpClient;

    public OpAmp(EffectiveConfigStaticSettings staticSettings)
        : this(
            CreateEffectiveConfigReporterFactory(staticSettings),
            UpstreamOpAmpEnabledResolver.IsEnabled,
            UpstreamSdkSetupEnabledResolver.IsEnabled,
            UpstreamProfilerStateResolver.Resolve)
    {
    }

    internal OpAmp(
        Func<EffectiveConfigReporter> effectiveConfigReporterFactory,
        Func<bool> opAmpEnabledResolver,
        Func<bool> sdkSetupEnabledResolver,
        Func<EffectiveProfilerFeatures> profilerStateResolver)
    {
        _effectiveConfigReporter = new(() => TryCreateEffectiveConfigReporter(
            effectiveConfigReporterFactory,
            opAmpEnabledResolver,
            sdkSetupEnabledResolver));
        _profilerStateResolver = profilerStateResolver;
        _remoteConfigurationListener = new OpAmpRemoteConfigurationListener(SendEffectiveConfigAfterRemoteConfiguration, SendRemoteConfigStatusAsync);
    }

    public void ConfigureOptions(OpAmpClientSettings settings, PluginSettings pluginSettings)
    {
        ConfigureRemoteConfiguration(settings, pluginSettings);
        ConfigureEffectiveConfigReporting(settings);
    }

    public void ConfigureEffectiveConfigReporting(OpAmpClientSettings settings)
    {
        try
        {
            var effectiveConfigReporter = _effectiveConfigReporter.Value;
            if (effectiveConfigReporter == null)
            {
                return;
            }

            var profilerState = ResolveProfilerState();
            effectiveConfigReporter.RunPreflight(
                profilerState.Features,
                profilerState.CpuProfilerCallStackInterval);

            settings.EffectiveConfigurationReporting.EnableReporting = true;
            Volatile.Write(ref _effectiveConfigReportingEnabled, 1);
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Effective configuration reporting preflight failed. The capability will not be advertised.");
        }
    }

    public void ConfigureRemoteConfiguration(OpAmpClientSettings settings, PluginSettings pluginSettings)
    {
        if (!pluginSettings.OpAmpRemoteConfigEnabled)
        {
            return;
        }

        Volatile.Write(ref _remoteConfigurationEnabled, 1);
        settings.RemoteConfiguration.AcceptsRemoteConfig = true;
        settings.RemoteConfiguration.ReportsRemoteConfigStatus = true;
    }

    public void RecordLogExporterOptions(OtlpExporterOptions options)
    {
        _effectiveConfigReporter.Value?.CaptureLogExporterOptions(options);
    }

    public void MarkOpenTelemetryLoggerConfigured()
    {
        _effectiveConfigReporter.Value?.MarkOpenTelemetryLoggerConfigured();
    }

    public void RecordTraceProviderEndpoints(TracerProvider provider)
    {
        _effectiveConfigReporter.Value?.CaptureTraceEndpoints(provider);
    }

    public void RecordMetricProviderEndpoints(MeterProvider provider)
    {
        _effectiveConfigReporter.Value?.CaptureMetricEndpoints(provider);
    }

    public void OnClientStarted(OpAmpClient client)
    {
        Volatile.Write(ref _opAmpClient, client);
        if (Volatile.Read(ref _remoteConfigurationEnabled) != 0)
        {
            client.Subscribe(_remoteConfigurationListener);
        }

        if (Volatile.Read(ref _effectiveConfigReportingEnabled) == 0)
        {
            return;
        }

        _effectiveConfigReporter.Value?.SetOpAmpClient(client);
        Volatile.Write(ref _opAmpClientStarted, 1);
        TryReportEffectiveConfig();
    }

    public void FlushBeforeClientStops()
    {
        var client = Volatile.Read(ref _opAmpClient);
        if (Volatile.Read(ref _remoteConfigurationEnabled) != 0)
        {
            client?.Unsubscribe(_remoteConfigurationListener);
        }

        TryReportEffectiveConfig();
        var initialReportTask = Volatile.Read(ref _initialEffectiveConfigReportTask);
        if (initialReportTask == null)
        {
            return;
        }

        if (!initialReportTask.Wait(TimeSpan.FromSeconds(5)))
        {
            Log.Warning("Timed out waiting for effective configuration report before OpAMP client stopped.");
        }
    }

    public void MarkInstrumentationInitialized()
    {
        Volatile.Write(ref _instrumentationInitialized, 1);
        TryReportEffectiveConfig();
    }

    private static EffectiveConfigReporter? TryCreateEffectiveConfigReporter(
        Func<EffectiveConfigReporter> effectiveConfigReporterFactory,
        Func<bool> opAmpEnabledResolver,
        Func<bool> sdkSetupEnabledResolver)
    {
        try
        {
            if (!opAmpEnabledResolver())
            {
                return null;
            }

            if (!sdkSetupEnabledResolver())
            {
                Log.Warning(
                    "Effective configuration reporting is unavailable because automatic SDK setup is disabled and application-owned providers cannot be inspected.");
                return null;
            }

            return effectiveConfigReporterFactory();
        }
        catch (Exception e)
        {
            Log.Warning($"Could not create effective configuration reporter: {e.Message}");
            return null;
        }
    }

    private static Func<EffectiveConfigReporter> CreateEffectiveConfigReporterFactory(
        EffectiveConfigStaticSettings staticSettings)
    {
        return () => new EffectiveConfigReporter(
            staticSettings,
            OpenTelemetrySdkDisabledResolver.IsDisabled());
    }

    private void SendEffectiveConfigAfterRemoteConfiguration()
    {
        _ = ReportEffectiveConfigAsync();
    }

    private async Task SendRemoteConfigStatusAsync(RemoteConfigStatusReport statusReport)
    {
        try
        {
            var client = Volatile.Read(ref _opAmpClient);
            if (client == null)
            {
                return;
            }

            await client.SendRemoteConfigStatusAsync(statusReport).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to report remote configuration status to OpAMP server: {e.Message}");
        }
    }

    private void TryReportEffectiveConfig()
    {
        if (Volatile.Read(ref _effectiveConfigReportingEnabled) == 0 ||
            Volatile.Read(ref _instrumentationInitialized) == 0 ||
            Volatile.Read(ref _opAmpClientStarted) == 0)
        {
            return;
        }

        if (Interlocked.Exchange(ref _initialEffectiveConfigReportStarted, 1) != 0)
        {
            return;
        }

        Volatile.Write(ref _initialEffectiveConfigReportTask, ReportEffectiveConfigAsync());
    }

    private async Task ReportEffectiveConfigAsync()
    {
        try
        {
            var effectiveConfigReporter = _effectiveConfigReporter.Value;
            if (effectiveConfigReporter == null)
            {
                return;
            }

            RefreshProfilerState(effectiveConfigReporter);
            await effectiveConfigReporter.ReportToOpAmpAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to report effective configuration to OpAMP server.");
        }
    }

    private void RefreshProfilerState(EffectiveConfigReporter effectiveConfigReporter)
    {
        var profilerState = ResolveProfilerState();
        effectiveConfigReporter.UpdateProfilerState(
            profilerState.Features,
            profilerState.CpuProfilerCallStackInterval);
    }

    private (EffectiveProfilerFeatures Features, uint? CpuProfilerCallStackInterval) ResolveProfilerState()
    {
        var profilerFeatures = _profilerStateResolver();
        if (Volatile.Read(ref _remoteConfigurationEnabled) == 0)
        {
            return (profilerFeatures, null);
        }

        try
        {
            var runtimeSettings = ProfilerRuntimeConfiguration.Current;
            profilerFeatures = runtimeSettings.CpuProfilerEnabled
                ? profilerFeatures | EffectiveProfilerFeatures.Cpu
                : profilerFeatures & ~EffectiveProfilerFeatures.Cpu;

            return (profilerFeatures, runtimeSettings.CpuProfilerCallStackInterval);
        }
        catch (InvalidOperationException)
        {
            return (profilerFeatures, null);
        }
    }
}
