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

using System.Text;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal sealed class EffectiveConfigReporter
{
    internal const string EffectiveConfigFileName = "config";
    internal const string EffectiveConfigContentType = "text/plain+properties";

    private static readonly ILogger Log = new Logger();

    private readonly EffectiveConfigState _state = new();
    private readonly Func<IReadOnlyList<string>?> _bridgeLogEndpointResolver;
    private OpAmpClient? _opAmpClient;
    private int _iloggerLogsConfigured;

    public EffectiveConfigReporter()
        : this(ResolveBridgeLogEndpoints)
    {
    }

    internal EffectiveConfigReporter(Func<IReadOnlyList<string>?> bridgeLogEndpointResolver)
    {
        _bridgeLogEndpointResolver = bridgeLogEndpointResolver;
    }

    public void CaptureSplunkSettings(PluginSettings settings)
    {
        _state.SetSplunkSettings(settings);
    }

    public void CaptureServiceName(Resource resource)
    {
        var serviceName = EffectiveResourceConfigReader.ReadServiceName(resource);
        if (serviceName == null)
        {
            return;
        }

        _state.TrySetServiceName(serviceName);
    }

    public void CaptureTraceEndpoints(TracerProvider provider)
    {
        try
        {
            _state.SetEndpoints(EffectiveConfigKeys.TracesEndpoint, OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints(provider));
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to resolve {EffectiveConfigKeys.TracesEndpoint} from TracerProvider: {ex.Message}");
        }
    }

    public void CaptureMetricEndpoints(MeterProvider provider)
    {
        try
        {
            _state.SetEndpoints(EffectiveConfigKeys.MetricsEndpoint, OtlpEndpointProviderGraphResolver.ResolveMetricEndpoints(provider));
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to resolve {EffectiveConfigKeys.MetricsEndpoint} from MeterProvider: {ex.Message}");
        }
    }

    public void MarkOpenTelemetryLoggerConfigured()
    {
        // ILogger owns its LoggerProvider and disables bridge logging, so bridge reflection would report a different logs pipeline.
        Interlocked.Exchange(ref _iloggerLogsConfigured, 1);
        var endpointsChanged = _state.ClearEndpoints(EffectiveConfigKeys.LogsEndpoint);
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

        // Upstream's ILogger path calls the marker before configuring OTLP exporters, but SDK export clients do not exist yet.
        var endpoint = OtlpLogEndpointOptionsResolver.ResolveEndpoint(options);
        if (endpoint == null)
        {
            return;
        }

        if (_state.AddEndpoint(EffectiveConfigKeys.LogsEndpoint, endpoint))
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

    internal string BuildCurrentPayload()
    {
        CaptureBridgeLogEndpointsIfNeeded();
        return _state.BuildPayload();
    }

    private static IReadOnlyList<string>? ResolveBridgeLogEndpoints()
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
        var payload = BuildCurrentPayload();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(payload);
        var effectiveConfigFile = new EffectiveConfigFile(new ReadOnlyMemory<byte>(bytes), EffectiveConfigContentType, EffectiveConfigFileName);
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
                _state.ClearEndpoints(EffectiveConfigKeys.LogsEndpoint);
                return;
            }

            // Provider-graph values are the known-valid bridge endpoints.
            _state.SetEndpoints(EffectiveConfigKeys.LogsEndpoint, bridgeLogEndpoints);
        }
        catch (Exception ex)
        {
            // Reflection failure means bridge log endpoints are not known valid.
            _state.ClearEndpoints(EffectiveConfigKeys.LogsEndpoint);
            Log.Warning($"Failed to resolve {EffectiveConfigKeys.LogsEndpoint} from LoggerProvider: {ex.Message}");
        }
    }
}
