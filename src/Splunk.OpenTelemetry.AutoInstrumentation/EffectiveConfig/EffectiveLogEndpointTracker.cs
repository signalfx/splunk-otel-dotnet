// <copyright file="EffectiveLogEndpointTracker.cs" company="Splunk Inc.">
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
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Resolvers;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal sealed class EffectiveLogEndpointTracker
{
    private static readonly ILogger Log = new Logger();

    private readonly object _lock = new();
    private readonly List<EffectiveOtlpEndpoint> _endpoints = [];
    private readonly Func<IReadOnlyList<EffectiveOtlpEndpoint>?> _bridgeLogEndpointResolver;
    private bool _iloggerLogsConfigured;

    public EffectiveLogEndpointTracker()
        : this(ResolveBridgeLogEndpoints)
    {
    }

    internal EffectiveLogEndpointTracker(
        Func<IReadOnlyList<EffectiveOtlpEndpoint>?> bridgeLogEndpointResolver)
    {
        _bridgeLogEndpointResolver = bridgeLogEndpointResolver;
    }

    public bool MarkOpenTelemetryLoggerConfigured()
    {
        lock (_lock)
        {
            // ILogger owns its LoggerProvider and disables bridge logging, so bridge reflection would report a different logs pipeline.
            _iloggerLogsConfigured = true;
            var hadEndpoints = _endpoints.Count > 0;
            _endpoints.Clear();
            return hadEndpoints;
        }
    }

    public bool CaptureLogExporterOptions(OtlpExporterOptions options)
    {
        lock (_lock)
        {
            if (!_iloggerLogsConfigured)
            {
                // This hook can run during bridge setup too. Without the ILogger marker, only provider-graph values are known valid.
                return false;
            }

            try
            {
                // Upstream's ILogger path calls the marker before configuring OTLP exporters, but SDK export clients do not exist yet.
                var endpoint = OtlpLogEndpointOptionsResolver.ResolveEndpoint(options);
                if (endpoint == null)
                {
                    return false;
                }

                if (_endpoints.Contains(endpoint.Value))
                {
                    return false;
                }

                _endpoints.Add(endpoint.Value);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to resolve logs endpoint from OtlpExporterOptions: {ex.Message}");
                return false;
            }
        }
    }

    public IReadOnlyList<EffectiveOtlpEndpoint> GetCurrentEndpoints()
    {
        lock (_lock)
        {
            if (!_iloggerLogsConfigured)
            {
                CaptureBridgeLogEndpoints();
            }

            return _endpoints.ToArray();
        }
    }

    private static IReadOnlyList<EffectiveOtlpEndpoint>? ResolveBridgeLogEndpoints()
    {
        // NLog/log4net bridges use upstream's LoggerProvider; do not force Lazy.Value here.
        var bridgeLoggerProvider = UpstreamLoggerProviderResolver.TryGetAlreadyCreatedLoggerProvider();
        return bridgeLoggerProvider == null
            ? null
            : OtlpEndpointProviderGraphResolver.ResolveLogEndpoints(bridgeLoggerProvider);
    }

    private void CaptureBridgeLogEndpoints()
    {
        try
        {
            var bridgeLogEndpoints = _bridgeLogEndpointResolver();
            if (_iloggerLogsConfigured)
            {
                return;
            }

            _endpoints.Clear();
            if (bridgeLogEndpoints == null)
            {
                // Without provider-graph results, bridge log endpoints are not known valid.
                return;
            }

            // Provider-graph values are the known-valid bridge endpoints.
            _endpoints.AddRange(bridgeLogEndpoints);
        }
        catch (Exception ex)
        {
            // Reflection failure means bridge log endpoints are not known valid.
            if (!_iloggerLogsConfigured)
            {
                _endpoints.Clear();
            }

            Log.Warning($"Failed to resolve logs endpoints from LoggerProvider: {ex.Message}");
        }
    }
}
