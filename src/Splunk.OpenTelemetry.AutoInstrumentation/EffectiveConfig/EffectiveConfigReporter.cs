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
    private volatile EffectiveProfilerFeatures _profilerFeatures;
    private OpAmpClient? _opAmpClient;

    public EffectiveConfigReporter(EffectiveConfigStaticSettings staticSettings)
        : this(staticSettings, new EffectiveLogEndpointTracker())
    {
    }

    internal EffectiveConfigReporter(
        EffectiveConfigStaticSettings staticSettings,
        Func<IReadOnlyList<EffectiveOtlpEndpoint>?> bridgeLogEndpointResolver)
        : this(staticSettings, new EffectiveLogEndpointTracker(bridgeLogEndpointResolver))
    {
    }

    private EffectiveConfigReporter(EffectiveConfigStaticSettings staticSettings, EffectiveLogEndpointTracker logEndpointTracker)
    {
        _staticSettings = staticSettings;
        _traceEndpointTracker = new EffectiveProviderEndpointTracker<TracerProvider>(
            OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints);
        _metricEndpointTracker = new EffectiveProviderEndpointTracker<MeterProvider>(
            OtlpEndpointProviderGraphResolver.ResolveMetricEndpoints);
        _logEndpointTracker = logEndpointTracker;
    }

    public void CaptureTraceEndpoints(TracerProvider provider)
    {
        if (_traceEndpointTracker.Capture(provider))
        {
            SendUpdatedPayloadIfOpAmpClientIsAvailable();
        }
    }

    public void CaptureMetricEndpoints(MeterProvider provider)
    {
        if (_metricEndpointTracker.Capture(provider))
        {
            SendUpdatedPayloadIfOpAmpClientIsAvailable();
        }
    }

    public void MarkOpenTelemetryLoggerConfigured()
    {
        if (_logEndpointTracker.MarkOpenTelemetryLoggerConfigured())
        {
            SendUpdatedPayloadIfOpAmpClientIsAvailable();
        }
    }

    public void CaptureLogExporterOptions(OtlpExporterOptions options)
    {
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

    public void SetProfilerFeatures(EffectiveProfilerFeatures profilerFeatures)
    {
        _profilerFeatures = profilerFeatures;
    }

    internal EffectiveConfigFile BuildCurrentPayload()
    {
        var traceEndpoints = _traceEndpointTracker.GetCurrentEndpoints();
        var metricEndpoints = _metricEndpointTracker.GetCurrentEndpoints();
        var logEndpoints = _logEndpointTracker.GetCurrentEndpoints();
        var snapshot = EffectiveConfigSnapshot.Create(
            _staticSettings,
            _profilerFeatures,
            traceEndpoints,
            metricEndpoints,
            logEndpoints);
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
