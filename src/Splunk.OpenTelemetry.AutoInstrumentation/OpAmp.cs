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

using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
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

    private readonly Lock _lifecycleLock = new();
    private readonly Lazy<EffectiveConfigRecorder?> _effectiveConfigRecorder;
    private readonly Func<EffectiveProfilerState> _profilerStateResolver;
    private readonly OpAmpRemoteConfigurationListener _remoteConfigurationListener;
    private EffectiveConfigReporter? _effectiveConfigReporter;
    private IOpAmpClient? _client;
    private RemoteConfigStatusReport? _remoteConfigStatus;
    private bool _remoteConfigurationEnabled;
    private bool _remoteConfigurationSubscribed;
    private bool _stopped;

    public OpAmp(EffectiveConfigStaticSettings staticSettings)
        : this(
            () => new EffectiveConfigRecorder(staticSettings, OpenTelemetrySdkDisabledResolver.IsDisabled()),
            UpstreamOpAmpEnabledResolver.IsEnabled,
            UpstreamSdkSetupEnabledResolver.IsEnabled,
            UpstreamProfilerStateResolver.Resolve)
    {
    }

    internal OpAmp(
        Func<EffectiveConfigRecorder> effectiveConfigRecorderFactory,
        Func<bool> opAmpEnabledResolver,
        Func<bool> sdkSetupEnabledResolver,
        Func<EffectiveProfilerState> profilerStateResolver)
    {
        _effectiveConfigRecorder = new(() => TryCreateEffectiveConfigRecorder(
            effectiveConfigRecorderFactory,
            opAmpEnabledResolver,
            sdkSetupEnabledResolver));
        _profilerStateResolver = profilerStateResolver;
        _remoteConfigurationListener = new OpAmpRemoteConfigurationListener(NotifyEffectiveConfigChanged, ReportRemoteConfigStatusAsync);
    }

    public void ConfigureOptions(OpAmpClientSettings settings, PluginSettings pluginSettings)
    {
        lock (_lifecycleLock)
        {
            _remoteConfigurationEnabled = !_stopped && pluginSettings.OpAmpRemoteConfigEnabled;
            if (_remoteConfigurationEnabled)
            {
                settings.RemoteConfiguration.AcceptsRemoteConfig = true;
                settings.RemoteConfiguration.ReportsRemoteConfigStatus = true;
            }
        }

        ConfigureEffectiveConfigReporting(settings);
    }

    public void ConfigureEffectiveConfigReporting(OpAmpClientSettings settings)
    {
        settings.EffectiveConfigurationReporting.EnableReporting = false;
        try
        {
            var recorder = _effectiveConfigRecorder.Value;
            if (recorder == null)
            {
                return;
            }

            var reporter = EffectiveConfigReporter.CreateValidated(recorder, _profilerStateResolver);
            lock (_lifecycleLock)
            {
                if (_stopped)
                {
                    return;
                }

                _effectiveConfigReporter = reporter;
                settings.EffectiveConfigurationReporting.EnableReporting = true;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Effective configuration reporting preflight failed. The capability will not be advertised.");
        }
    }

    public void ConfigureClient(IOpAmpClient client)
    {
        bool subscribe;
        lock (_lifecycleLock)
        {
            if (_stopped || _client != null)
            {
                return;
            }

            _client = client;
            subscribe = _remoteConfigurationEnabled;
        }

        if (!subscribe)
        {
            return;
        }

        try
        {
            client.Subscribe(_remoteConfigurationListener);
        }
        catch
        {
            TryUnsubscribeRemoteConfiguration(client);
            throw;
        }

        lock (_lifecycleLock)
        {
            if (!_stopped && ReferenceEquals(_client, client))
            {
                _remoteConfigurationSubscribed = true;
                return;
            }
        }

        TryUnsubscribeRemoteConfiguration(client);
    }

    public RemoteConfigStatusReport? GetRemoteConfigStatus()
    {
        lock (_lifecycleLock)
        {
            return _remoteConfigStatus;
        }
    }

    public IReadOnlyCollection<EffectiveConfigFile> GetEffectiveConfig()
    {
        EffectiveConfigReporter reporter;
        lock (_lifecycleLock)
        {
            reporter = _effectiveConfigReporter
                ?? throw new InvalidOperationException("Effective configuration reporting is unavailable.");
        }

        // A getter may overlap cleanup. Keep its reporter alive and build outside lifecycle locks.
        return [reporter.BuildCurrentPayload()];
    }

    public void RecordLogExporterOptions(OtlpExporterOptions options)
    {
        if (_effectiveConfigRecorder.Value?.CaptureLogExporterOptions(options) == true)
        {
            NotifyEffectiveConfigChanged();
        }
    }

    public void MarkOpenTelemetryLoggerConfigured()
    {
        if (_effectiveConfigRecorder.Value?.MarkOpenTelemetryLoggerConfigured() == true)
        {
            NotifyEffectiveConfigChanged();
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

    public void StopClientReporting()
    {
        IOpAmpClient? client;
        lock (_lifecycleLock)
        {
            _stopped = true;
            client = _remoteConfigurationSubscribed ? _client : null;
            _remoteConfigurationSubscribed = false;
            _client = null;
        }

        if (client != null)
        {
            TryUnsubscribeRemoteConfiguration(client);
        }
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
                Log.Warning("Effective configuration reporting is unavailable because automatic SDK setup is disabled and application-owned providers cannot be inspected.");
                return null;
            }

            return effectiveConfigRecorderFactory();
        }
        catch (Exception ex)
        {
            Log.Warning($"Could not create effective configuration reporter: {ex.Message}");
            return null;
        }
    }

    private Task ReportRemoteConfigStatusAsync(RemoteConfigStatusReport status)
    {
        IOpAmpClient? client;
        lock (_lifecycleLock)
        {
            _remoteConfigStatus = status;
            client = _client;
        }

        try
        {
            client?.NotifyRemoteConfigStatusChanged();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to notify the OpAMP client of a remote configuration status change.");
        }

        return Task.CompletedTask;
    }

    private void TryUnsubscribeRemoteConfiguration(IOpAmpClient client)
    {
        try
        {
            client.Unsubscribe(_remoteConfigurationListener);
        }
        catch (ObjectDisposedException)
        {
            // Forced shutdown may dispose the client before plugin cleanup completes.
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to unsubscribe from OpAMP remote configuration: {ex.Message}");
        }
    }

    private void NotifyEffectiveConfigChanged()
    {
        IOpAmpClient? client;
        lock (_lifecycleLock)
        {
            client = _effectiveConfigReporter != null ? _client : null;
        }

        // Notification may synchronously wait for a getter on another thread.
        try
        {
            client?.NotifyEffectiveConfigChanged();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to notify the OpAMP client of an effective configuration change.");
        }
    }
}
