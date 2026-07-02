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

using System.Threading;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Settings;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;
using Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal sealed class OpAmp
{
    private static readonly ILogger Log = new Logger();

    private readonly Lazy<EffectiveConfigReporter?> _effectiveConfigReporter;
    private readonly OpAmpRemoteConfigurationListener _remoteConfigurationListener;
    private int _instrumentationInitialized;
    private int _opAmpClientStarted;
    private int _initialEffectiveConfigReportStarted;
    private bool _remoteConfigurationEnabled;
    private Task? _initialEffectiveConfigReportTask;
    private OpAmpClient? _opAmpClient;

    public OpAmp()
    {
        _effectiveConfigReporter = new Lazy<EffectiveConfigReporter?>(TryCreateEffectiveConfigReporter);
        _remoteConfigurationListener = new OpAmpRemoteConfigurationListener(SendEffectiveConfigAfterRemoteConfiguration);
    }

    public void ConfigureOptions(OpAmpClientSettings settings, PluginSettings pluginSettings)
    {
        settings.EffectiveConfigurationReporting.EnableReporting = true;

        if (!pluginSettings.OpAmpRemoteConfigEnabled)
        {
            return;
        }

        _remoteConfigurationEnabled = pluginSettings.OpAmpRemoteConfigEnabled;
        settings.RemoteConfiguration.AcceptsRemoteConfig = pluginSettings.OpAmpRemoteConfigEnabled;
    }

    public void RecordPluginConfig(PluginSettings settings)
    {
        _effectiveConfigReporter.Value?.CaptureSplunkSettings(settings);
    }

    public void RecordServiceName(Resource resource)
    {
        _effectiveConfigReporter.Value?.CaptureServiceName(resource);
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
        if (_remoteConfigurationEnabled)
        {
            client.Subscribe(_remoteConfigurationListener);
        }

        _effectiveConfigReporter.Value?.SetOpAmpClient(client);
        Volatile.Write(ref _opAmpClientStarted, 1);
        TryReportEffectiveConfig();
    }

    public void FlushBeforeClientStops()
    {
        var client = Volatile.Read(ref _opAmpClient);
        if (_remoteConfigurationEnabled)
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

    private static EffectiveConfigReporter? TryCreateEffectiveConfigReporter()
    {
        try
        {
            return UpstreamOpAmpEnabledResolver.IsEnabled() ? new EffectiveConfigReporter() : null;
        }
        catch (Exception e)
        {
            Log.Warning($"Could not create effective configuration reporter: {e.Message}");
            return null;
        }
    }

    private void SendEffectiveConfigAfterRemoteConfiguration()
    {
        _ = ReportEffectiveConfigAsync();
    }

    private void TryReportEffectiveConfig()
    {
        if (Volatile.Read(ref _instrumentationInitialized) == 0 ||
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

            await effectiveConfigReporter.ReportToOpAmpAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to report effective configuration to OpAMP server.");
        }
    }
}
