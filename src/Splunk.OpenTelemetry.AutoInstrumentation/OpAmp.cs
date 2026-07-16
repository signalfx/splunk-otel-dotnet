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
using OpenTelemetry.OpAmp.Client.Settings;
using OpenTelemetry.Trace;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Resolvers;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal sealed class OpAmp
{
    private static readonly ILogger Log = new Logger();

    private readonly object _lifecycleLock = new();
    private readonly Lazy<EffectiveConfigRecorder?> _effectiveConfigRecorder;
    private readonly Func<EffectiveProfilerFeatures> _profilerStateResolver;
    private EffectiveConfigReporter? _effectiveConfigReporter;
    private bool _instrumentationInitialized;
    private ClientLifecycleState _clientLifecycleState;
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
    }

    private enum ClientLifecycleState
    {
        NotStarted,
        Started,
        Stopped
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

            var profilerFeatures = _profilerStateResolver();
            var effectiveConfigReporter = EffectiveConfigReporter.CreateValidated(recorder, profilerFeatures);

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
            _reportingPump = reportingPump;
            _clientLifecycleState = ClientLifecycleState.Started;
        }

        try
        {
            reportingPump.Start();
        }
        catch (Exception ex)
        {
            reportingPump.Stop();
            lock (_lifecycleLock)
            {
                if (ReferenceEquals(_reportingPump, reportingPump))
                {
                    _reportingPump = null;
                    _clientLifecycleState = ClientLifecycleState.Stopped;
                }
            }

            Log.Error(ex, "Failed to start OpAMP reporting. Automatic instrumentation will continue without reporting effective configuration or full state.");
        }
    }

    public void StopClientReporting()
    {
        OpAmpReportingPump? reportingPump;
        lock (_lifecycleLock)
        {
            if (_clientLifecycleState == ClientLifecycleState.Stopped)
            {
                return;
            }

            reportingPump = _reportingPump;
            _reportingPump = null;
            _clientLifecycleState = ClientLifecycleState.Stopped;
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

    private void NotifyILoggerEffectiveConfigChanged()
    {
        OpAmpReportingPump? reportingPump;
        lock (_lifecycleLock)
        {
            reportingPump = _reportingPump;
        }

        reportingPump?.NotifyILoggerEffectiveConfigChanged();
    }
}
