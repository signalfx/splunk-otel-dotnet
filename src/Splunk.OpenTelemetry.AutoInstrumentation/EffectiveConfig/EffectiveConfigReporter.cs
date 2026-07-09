// <copyright file="EffectiveConfigReporter.cs" company="Splunk Inc.">
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
using OpenTelemetry.Trace;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Resolvers;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal sealed class EffectiveConfigReporter
{
    private static readonly ILogger Log = new Logger();

    private readonly EffectiveConfigStaticSettings _staticSettings;
    private readonly EffectiveProviderEndpointTracker<TracerProvider> _traceEndpointTracker;
    private readonly EffectiveProviderEndpointTracker<MeterProvider> _metricEndpointTracker;
    private readonly EffectiveLogEndpointTracker _logEndpointTracker;
    private readonly bool _openTelemetrySdkDisabled;
    private volatile EffectiveProfilerFeatures _profilerFeatures;
    private long _cpuProfilerCallStackInterval;
    private OpAmpClient? _opAmpClient;

    public EffectiveConfigReporter(
        EffectiveConfigStaticSettings staticSettings,
        bool openTelemetrySdkDisabled)
        : this(staticSettings, openTelemetrySdkDisabled, new EffectiveLogEndpointTracker())
    {
    }

    internal EffectiveConfigReporter(
        EffectiveConfigStaticSettings staticSettings,
        bool openTelemetrySdkDisabled,
        Func<IReadOnlyList<EffectiveOtlpEndpoint>?> bridgeLogEndpointResolver)
        : this(
            staticSettings,
            openTelemetrySdkDisabled,
            new EffectiveLogEndpointTracker(bridgeLogEndpointResolver))
    {
    }

    private EffectiveConfigReporter(
        EffectiveConfigStaticSettings staticSettings,
        bool openTelemetrySdkDisabled,
        EffectiveLogEndpointTracker logEndpointTracker)
    {
        _staticSettings = staticSettings;
        _openTelemetrySdkDisabled = openTelemetrySdkDisabled;
        _traceEndpointTracker = new EffectiveProviderEndpointTracker<TracerProvider>(
            OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints);
        _metricEndpointTracker = new EffectiveProviderEndpointTracker<MeterProvider>(
            OtlpEndpointProviderGraphResolver.ResolveMetricEndpoints);
        _logEndpointTracker = logEndpointTracker;
        _cpuProfilerCallStackInterval = staticSettings.CpuProfilerCallStackInterval;
    }

    public void CaptureTraceEndpoints(TracerProvider provider)
    {
        if (_openTelemetrySdkDisabled)
        {
            return;
        }

        if (_traceEndpointTracker.Capture(provider))
        {
            SendUpdatedPayloadIfOpAmpClientIsAvailable();
        }
    }

    public void CaptureMetricEndpoints(MeterProvider provider)
    {
        if (_openTelemetrySdkDisabled)
        {
            return;
        }

        if (_metricEndpointTracker.Capture(provider))
        {
            SendUpdatedPayloadIfOpAmpClientIsAvailable();
        }
    }

    public void MarkOpenTelemetryLoggerConfigured()
    {
        if (_openTelemetrySdkDisabled)
        {
            return;
        }

        if (_logEndpointTracker.MarkOpenTelemetryLoggerConfigured())
        {
            SendUpdatedPayloadIfOpAmpClientIsAvailable();
        }
    }

    public void CaptureLogExporterOptions(OtlpExporterOptions options)
    {
        if (_openTelemetrySdkDisabled)
        {
            return;
        }

        if (_logEndpointTracker.CaptureLogExporterOptions(options))
        {
            SendUpdatedPayloadIfOpAmpClientIsAvailable();
        }
    }

    public async Task ReportToOpAmpAsync()
    {
        var client = Volatile.Read(ref _opAmpClient);
        if (client == null)
        {
            return;
        }

        await SendCurrentPayloadAsync(client).ConfigureAwait(false);
    }

    public void SetOpAmpClient(OpAmpClient client)
    {
        Volatile.Write(ref _opAmpClient, client);
    }

    public void RunPreflight(EffectiveProfilerFeatures profilerFeatures, uint? cpuProfilerCallStackInterval = null)
    {
        UpdateProfilerState(profilerFeatures, cpuProfilerCallStackInterval);
#if NET
        if (!_openTelemetrySdkDisabled)
        {
            OtlpLogEndpointOptionsResolver.ValidateCompatibility();
        }
#endif

        // Build and serialize the payload now so failures prevent capability advertisement.
        // The initial report rebuilds it to include endpoints captured after preflight.
        _ = BuildCurrentPayload();
    }

    public void UpdateProfilerState(EffectiveProfilerFeatures profilerFeatures, uint? cpuProfilerCallStackInterval = null)
    {
        _profilerFeatures = profilerFeatures;
        if (cpuProfilerCallStackInterval.HasValue)
        {
            Volatile.Write(ref _cpuProfilerCallStackInterval, cpuProfilerCallStackInterval.Value);
        }
    }

    internal EffectiveConfigFile BuildCurrentPayload()
    {
        IReadOnlyList<EffectiveOtlpEndpoint> traceEndpoints = [];
        IReadOnlyList<EffectiveOtlpEndpoint> metricEndpoints = [];
        IReadOnlyList<EffectiveOtlpEndpoint> logEndpoints = [];

        if (!_openTelemetrySdkDisabled)
        {
            traceEndpoints = _traceEndpointTracker.GetEndpoints();
            metricEndpoints = _metricEndpointTracker.GetEndpoints();
            logEndpoints = _logEndpointTracker.GetEndpoints();
        }

        var snapshot = EffectiveConfigSnapshot.Create(
            _staticSettings,
            _profilerFeatures,
            traceEndpoints,
            metricEndpoints,
            logEndpoints,
            unchecked((uint)Volatile.Read(ref _cpuProfilerCallStackInterval)));
        return EffectiveConfigPayloadBuilder.Build(snapshot);
    }

    private void SendUpdatedPayloadIfOpAmpClientIsAvailable()
    {
        var client = Volatile.Read(ref _opAmpClient);
        if (client == null)
        {
            return;
        }

        _ = SendUpdatedPayloadAsync(client);
    }

    private async Task SendUpdatedPayloadAsync(OpAmpClient client)
    {
        try
        {
            await SendCurrentPayloadAsync(client).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to send updated effective configuration to OpAmp server: {ex.Message}");
        }
    }

    private async Task SendCurrentPayloadAsync(OpAmpClient client)
    {
        var effectiveConfigFile = BuildCurrentPayload();
        await client.SendEffectiveConfigAsync([effectiveConfigFile]).ConfigureAwait(false);
    }
}
