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
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Model;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Resolvers;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal sealed class EffectiveConfigReporter
{
    private static readonly ILogger Log = new Logger();

    private readonly EffectiveConfigState _state = new();
    private readonly Func<IReadOnlyList<EffectiveOtlpEndpoint>?> _bridgeLogEndpointResolver;
    private OpAmpClient? _opAmpClient;
    private int _iloggerLogsConfigured;

    public EffectiveConfigReporter()
        : this(ResolveBridgeLogEndpoints)
    {
    }

    internal EffectiveConfigReporter(Func<IReadOnlyList<EffectiveOtlpEndpoint>?> bridgeLogEndpointResolver)
    {
        _bridgeLogEndpointResolver = bridgeLogEndpointResolver;
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
        // ILogger owns its LoggerProvider and disables bridge logging, so bridge reflection would report a different logs pipeline.
        Interlocked.Exchange(ref _iloggerLogsConfigured, 1);
        var endpointsChanged = _state.ClearLogEndpoints();
        if (endpointsChanged)
        {
            SendUpdatedPayloadIfOpAmpClientIsAvailable();
        }
    }

    public void CaptureLogExporterOptions(OtlpExporterOptions options)
    {
        if (Volatile.Read(ref _iloggerLogsConfigured) == 0)
        {
            // This hook can run during bridge setup too. Without the ILogger marker, only provider-graph values are known valid.
            return;
        }

        try
        {
            // Upstream's ILogger path calls the marker before configuring OTLP exporters, but SDK export clients do not exist yet.
            var endpoint = OtlpLogEndpointOptionsResolver.ResolveEndpoint(options);
            if (endpoint == null)
            {
                return;
            }

            if (_state.AddLogEndpoint(endpoint.Value))
            {
                SendUpdatedPayloadIfOpAmpClientIsAvailable();
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to resolve logs endpoint from OtlpExporterOptions: {ex.Message}");
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
        CaptureBridgeLogEndpointsIfNeeded();
        return EffectiveConfigPayloadBuilder.Build(_state.CreateSnapshot());
    }

    private static IReadOnlyList<EffectiveOtlpEndpoint>? ResolveBridgeLogEndpoints()
    {
        // NLog/log4net bridges use upstream's LoggerProvider; do not force Lazy.Value here.
        var bridgeLoggerProvider = UpstreamLoggerProviderResolver.TryGetAlreadyCreatedLoggerProvider();
        return bridgeLoggerProvider == null
            ? null
            : OtlpEndpointProviderGraphResolver.ResolveLogEndpoints(bridgeLoggerProvider);
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

    private void CaptureBridgeLogEndpointsIfNeeded()
    {
        if (Volatile.Read(ref _iloggerLogsConfigured) != 0)
        {
            return;
        }

        try
        {
            var bridgeLogEndpoints = _bridgeLogEndpointResolver();
            if (Volatile.Read(ref _iloggerLogsConfigured) != 0)
            {
                return;
            }

            if (bridgeLogEndpoints == null)
            {
                // Without provider-graph results, bridge log endpoints are not known valid.
                _state.ClearLogEndpoints();
                return;
            }

            // Provider-graph values are the known-valid bridge endpoints.
            _state.SetLogEndpoints(bridgeLogEndpoints);
        }
        catch (Exception ex)
        {
            // Reflection failure means bridge log endpoints are not known valid.
            _state.ClearLogEndpoints();
            Log.Warning($"Failed to resolve logs endpoints from LoggerProvider: {ex.Message}");
        }
    }
}
