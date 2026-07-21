// <copyright file="OpAmp.cs" company="Splunk Inc.">
// Copyright Splunk Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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

    private readonly object _lifecycleLock = new();
    private readonly Lazy<EffectiveConfigRecorder?> _effectiveConfigRecorder;
    private readonly OpAmpRemoteConfigurationListener _remoteConfigurationListener;
    private readonly Func<EffectiveProfilerFeatures> _profilerStateResolver;
    private EffectiveConfigReporter? _effectiveConfigReporter;
    private bool _instrumentationInitialized;
    private int _remoteConfigurationEnabled;
    private bool _remoteConfigurationSubscribed;
    private ClientLifecycleState _clientLifecycleState;
    private OpAmpClient? _opAmpClient;
    private OpAmpReportingPump? _reportingPump;

    public OpAmp(EffectiveConfigStaticSettings staticSettings)
        : this(
            CreateEffectiveConfigRecorderFactory(staticSettings),
            UpstreamOpAmpEnabledResolver.IsEnabled,
            UpstreamSdkSetupEnabledResolver.IsEnabled,
            UpstreamProfilerStateResolver.Resolve)
    {
    }

    internal OpAmp(
        Func<EffectiveConfigRecorder> effectiveConfigRecorderFactory,
        Func<bool> opAmpEnabledResolver,
        Func<bool> sdkSetupEnabledResolver,
        Func<EffectiveProfilerFeatures> profilerStateResolver)
    {
        _effectiveConfigRecorder = new(() => TryCreateEffectiveConfigRecorder(
            effectiveConfigRecorderFactory,
            opAmpEnabledResolver,
            sdkSetupEnabledResolver));
        _profilerStateResolver = profilerStateResolver;
        _remoteConfigurationListener = new OpAmpRemoteConfigurationListener(SendEffectiveConfigAfterRemoteConfiguration, SendRemoteConfigStatusAsync);
    }

    private enum ClientLifecycleState
    {
        NotStarted,
        Started,
        Stopped
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
            var recorder = _effectiveConfigRecorder.Value;
            if (recorder == null)
            {
                return;
            }

            var profilerState = ResolveProfilerState();
            var effectiveConfigReporter = EffectiveConfigReporter.CreateValidated(
                recorder,
                profilerState.Features,
                profilerState.CpuProfilerCallStackInterval);

            lock (_lifecycleLock)
            {
                _effectiveConfigReporter = effectiveConfigReporter;
            }

            settings.EffectiveConfigurationReporting.EnableReporting = true;
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
        if (_effectiveConfigRecorder.Value?.CaptureLogExporterOptions(options) == true)
        {
            NotifyILoggerEffectiveConfigChanged();
        }
    }

    public void MarkOpenTelemetryLoggerConfigured()
    {
        if (_effectiveConfigRecorder.Value?.MarkOpenTelemetryLoggerConfigured() == true)
        {
            NotifyILoggerEffectiveConfigChanged();
        }
    }

    public void RecordTraceProviderEndpoints(TracerProvider provider)
    {
        _effectiveConfigRecorder.Value?.CaptureTraceEndpoints(provider);
    }

    public void RecordMetricProviderEndpoints(MeterProvider provider)
    {
        _effectiveConfigRecorder.Value?.CaptureMetricEndpoints(provider);
    }

    public void OnClientStarted(OpAmpClient client)
    {
        OpAmpReportingPump reportingPump;
        lock (_lifecycleLock)
        {
            if (_clientLifecycleState != ClientLifecycleState.NotStarted)
            {
                if (_clientLifecycleState == ClientLifecycleState.Started &&
                    _reportingPump?.IsForClient(client) == true)
                {
                    Log.Warning("Ignoring a duplicate OpAMP client-started callback for the active client.");
                }
                else
                {
                    Log.Warning("Ignoring an unexpected replacement OpAMP client.");
                }

                return;
            }

            reportingPump = new OpAmpReportingPump(
                client,
                _effectiveConfigReporter,
                _instrumentationInitialized);
            _opAmpClient = client;
            _reportingPump = reportingPump;
            _clientLifecycleState = ClientLifecycleState.Started;
        }

        var remoteConfigurationSubscribed = false;
        var clientStoppedBeforeSubscriptionCompleted = false;
        try
        {
            if (Volatile.Read(ref _remoteConfigurationEnabled) != 0)
            {
                client.Subscribe(_remoteConfigurationListener);
                remoteConfigurationSubscribed = true;

                lock (_lifecycleLock)
                {
                    if (_clientLifecycleState == ClientLifecycleState.Started &&
                        ReferenceEquals(_opAmpClient, client) &&
                        ReferenceEquals(_reportingPump, reportingPump))
                    {
                        _remoteConfigurationSubscribed = true;
                    }
                    else
                    {
                        clientStoppedBeforeSubscriptionCompleted = true;
                    }
                }

                if (clientStoppedBeforeSubscriptionCompleted)
                {
                    TryUnsubscribeRemoteConfiguration(client);
                    return;
                }
            }

            reportingPump.Start();
        }
        catch (Exception ex)
        {
            if (remoteConfigurationSubscribed)
            {
                TryUnsubscribeRemoteConfiguration(client);
            }

            reportingPump.Stop();
            lock (_lifecycleLock)
            {
                if (ReferenceEquals(_reportingPump, reportingPump))
                {
                    _opAmpClient = null;
                    _reportingPump = null;
                    _remoteConfigurationSubscribed = false;
                    _clientLifecycleState = ClientLifecycleState.Stopped;
                }
            }

            Log.Error(ex, "Failed to start OpAMP reporting. Automatic instrumentation will continue without reporting effective configuration or full state.");
        }
    }

    public void StopClientReporting()
    {
        OpAmpClient? client;
        OpAmpReportingPump? reportingPump;
        bool remoteConfigurationSubscribed;
        lock (_lifecycleLock)
        {
            if (_clientLifecycleState == ClientLifecycleState.Stopped)
            {
                return;
            }

            client = _opAmpClient;
            _opAmpClient = null;
            reportingPump = _reportingPump;
            _reportingPump = null;
            remoteConfigurationSubscribed = _remoteConfigurationSubscribed;
            _remoteConfigurationSubscribed = false;
            _clientLifecycleState = ClientLifecycleState.Stopped;
        }

        if (client != null && remoteConfigurationSubscribed)
        {
            TryUnsubscribeRemoteConfiguration(client);
        }

        reportingPump?.Stop();
    }

    public void MarkInstrumentationInitialized()
    {
        OpAmpReportingPump? reportingPump;
        lock (_lifecycleLock)
        {
            _instrumentationInitialized = true;
            reportingPump = _reportingPump;
        }

        reportingPump?.MarkInstrumentationInitialized();
    }

    private static EffectiveConfigRecorder? TryCreateEffectiveConfigRecorder(
        Func<EffectiveConfigRecorder> effectiveConfigRecorderFactory,
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

            return effectiveConfigRecorderFactory();
        }
        catch (Exception e)
        {
            Log.Warning($"Could not create effective configuration reporter: {e.Message}");
            return null;
        }
    }

    private static Func<EffectiveConfigRecorder> CreateEffectiveConfigRecorderFactory(
        EffectiveConfigStaticSettings staticSettings)
    {
        return () => new EffectiveConfigRecorder(
            staticSettings,
            OpenTelemetrySdkDisabledResolver.IsDisabled());
    }

    private void SendEffectiveConfigAfterRemoteConfiguration()
    {
        try
        {
            EffectiveConfigReporter? effectiveConfigReporter;
            OpAmpReportingPump? reportingPump;
            lock (_lifecycleLock)
            {
                effectiveConfigReporter = _effectiveConfigReporter;
                reportingPump = _reportingPump;
            }

            if (effectiveConfigReporter == null)
            {
                return;
            }

            RefreshProfilerState(effectiveConfigReporter);
            reportingPump?.NotifyEffectiveConfigChanged();
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to report effective configuration to OpAMP server.");
        }
    }

    private async Task SendRemoteConfigStatusAsync(RemoteConfigStatusReport statusReport)
    {
        try
        {
            OpAmpClient? client;
            lock (_lifecycleLock)
            {
                client = _clientLifecycleState == ClientLifecycleState.Started
                    ? _opAmpClient
                    : null;
            }

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

    private void NotifyILoggerEffectiveConfigChanged()
    {
        OpAmpReportingPump? reportingPump;
        lock (_lifecycleLock)
        {
            reportingPump = _reportingPump;
        }

        reportingPump?.NotifyILoggerEffectiveConfigChanged();
    }

    private void TryUnsubscribeRemoteConfiguration(OpAmpClient client)
    {
        try
        {
            client.Unsubscribe(_remoteConfigurationListener);
        }
        catch (ObjectDisposedException)
        {
            // The owner may dispose the client before the shutdown callback stops reporting work.
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to unsubscribe from OpAMP remote configuration: {ex.Message}");
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
