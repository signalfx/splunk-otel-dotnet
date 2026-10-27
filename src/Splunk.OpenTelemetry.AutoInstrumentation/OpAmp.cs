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

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal sealed class OpAmp
{
    private static readonly ILogger Log = new Logger();

    private readonly Lock _lifecycleLock = new();
    private readonly Lazy<EffectiveConfigRecorder?> _effectiveConfigRecorder;
    private readonly Func<EffectiveProfilerState> _profilerStateResolver;
    private EffectiveConfigReporter? _effectiveConfigReporter;
    private IOpAmpClient? _client;
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
        lock (_lifecycleLock)
        {
            if (!_stopped && _client == null)
            {
                _client = client;
            }
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

    public void StopClientReporting()
    {
        lock (_lifecycleLock)
        {
            _stopped = true;
            _client = null;
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

    private void NotifyILoggerEffectiveConfigChanged()
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
