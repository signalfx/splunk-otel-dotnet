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
    private readonly HashSet<EffectiveOtlpEndpoint> _endpointSet = [];
    private readonly Func<IReadOnlyList<EffectiveOtlpEndpoint>?> _bridgeLogEndpointResolver;
    private bool _bridgeLogEndpointsResolved;
    private bool _iloggerLogsConfigured;
    private bool _hasEndpointResolutionFailure;

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
            if (_iloggerLogsConfigured)
            {
                return false;
            }

            // ILogger owns its LoggerProvider and disables bridge logging, so bridge reflection would report a different logs pipeline.
            _iloggerLogsConfigured = true;
            var hadEndpoints = _endpoints.Count > 0;
            _endpoints.Clear();
            _endpointSet.Clear();
            return hadEndpoints;
        }
    }

    public bool CaptureLogExporterOptions(OtlpExporterOptions options)
    {
        Exception? resolutionFailure = null;
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
                if (!_endpointSet.Add(endpoint))
                {
                    return false;
                }

                _endpoints.Add(endpoint);
                return true;
            }
            catch (Exception ex)
            {
                _hasEndpointResolutionFailure = true;
                resolutionFailure = ex;
            }
        }

        Log.Warning($"Failed to resolve logs endpoint from OtlpExporterOptions: {resolutionFailure!.Message}");
        return false;
    }

    public IReadOnlyList<EffectiveOtlpEndpoint> GetEndpoints()
    {
        ResolveBridgeLogEndpointsIfNeeded();

        lock (_lock)
        {
            ThrowIfResolutionFailed();
            return _endpoints.ToArray();
        }
    }

    public void ValidateState()
    {
        ResolveBridgeLogEndpointsIfNeeded();

        lock (_lock)
        {
            ThrowIfResolutionFailed();
        }
    }

    private static IReadOnlyList<EffectiveOtlpEndpoint>? ResolveBridgeLogEndpoints()
    {
        // NLog/log4net bridges use upstream's LoggerProvider; do not force Lazy.Value here.
        var bridgeLoggerProvider = UpstreamLoggerProviderResolver.GetAlreadyCreatedLoggerProvider();
        return bridgeLoggerProvider == null
            ? null
            : OtlpEndpointProviderGraphResolver.ResolveLogEndpoints(bridgeLoggerProvider);
    }

    private void ResolveBridgeLogEndpointsIfNeeded()
    {
        lock (_lock)
        {
            if (!ShouldResolveBridgeLogEndpointsLocked())
            {
                return;
            }
        }

        IReadOnlyList<EffectiveOtlpEndpoint>? bridgeLogEndpoints;
        try
        {
            // Resolving the provider graph may initialize the SDK's lazy export client and invoke
            // an application-provided HttpClientFactory. Do not run that code while holding _lock.
            bridgeLogEndpoints = _bridgeLogEndpointResolver();
        }
        catch
        {
            lock (_lock)
            {
                if (!ShouldResolveBridgeLogEndpointsLocked())
                {
                    // ILogger configuration or another bridge resolution superseded this attempt.
                    return;
                }
            }

            throw;
        }

        lock (_lock)
        {
            if (!ShouldResolveBridgeLogEndpointsLocked())
            {
                return;
            }

            _bridgeLogEndpointsResolved = true;
            _endpoints.Clear();
            if (bridgeLogEndpoints != null)
            {
                _endpoints.AddRange(bridgeLogEndpoints);
            }
        }
    }

    private bool ShouldResolveBridgeLogEndpointsLocked()
    {
        return !_iloggerLogsConfigured && !_bridgeLogEndpointsResolved;
    }

    private void ThrowIfResolutionFailed()
    {
        if (_hasEndpointResolutionFailure)
        {
            throw new InvalidOperationException("The OpenTelemetry logs endpoint state could not be resolved.");
        }
    }
}
