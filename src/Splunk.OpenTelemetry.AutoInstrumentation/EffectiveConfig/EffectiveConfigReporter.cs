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

    private readonly EffectiveConfigState _state = new();
    private readonly EffectiveLogEndpointTracker _logEndpointTracker;
    private OpAmpClient? _opAmpClient;

    public EffectiveConfigReporter()
    {
        _logEndpointTracker = new EffectiveLogEndpointTracker(_state);
    }

    internal EffectiveConfigReporter(Func<IReadOnlyList<EffectiveOtlpEndpoint>?> bridgeLogEndpointResolver)
    {
        _logEndpointTracker = new EffectiveLogEndpointTracker(_state, bridgeLogEndpointResolver);
    }

    public void CaptureSplunkSettings(PluginSettings settings)
    {
        _state.SetSplunkSettings(settings);
    }

    public void CaptureTraceEndpoints(TracerProvider provider)
    {
        try
        {
            _state.SetTraceEndpoints(OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints(provider));
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to resolve traces endpoints from TracerProvider: {ex.Message}");
        }
    }

    public void CaptureMetricEndpoints(MeterProvider provider)
    {
        try
        {
            _state.SetMetricEndpoints(OtlpEndpointProviderGraphResolver.ResolveMetricEndpoints(provider));
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to resolve metrics endpoints from MeterProvider: {ex.Message}");
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

    internal EffectiveConfigFile BuildCurrentPayload()
    {
        _logEndpointTracker.CaptureBridgeLogEndpointsIfNeeded();
        return EffectiveConfigPayloadBuilder.Build(_state.CreateSnapshot());
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
        if (effectiveConfigFile.Content.Length == 0)
        {
            return;
        }

        await client.SendEffectiveConfigAsync([effectiveConfigFile]).ConfigureAwait(false);
    }
}
