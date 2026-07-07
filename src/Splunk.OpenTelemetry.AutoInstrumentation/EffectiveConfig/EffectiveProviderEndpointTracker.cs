// <copyright file="EffectiveProviderEndpointTracker.cs" company="Splunk Inc.">
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

using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal sealed class EffectiveProviderEndpointTracker<TProvider>
{
    private static readonly ILogger Log = new Logger();

    private readonly object _lock = new();
    private readonly List<EffectiveOtlpEndpoint> _endpoints = [];
    private readonly Func<TProvider, IReadOnlyList<EffectiveOtlpEndpoint>> _endpointResolver;
    private bool _resolutionFailed;

    public EffectiveProviderEndpointTracker(Func<TProvider, IReadOnlyList<EffectiveOtlpEndpoint>> endpointResolver)
    {
        _endpointResolver = endpointResolver;
    }

    public bool Capture(TProvider provider)
    {
        lock (_lock)
        {
            IReadOnlyList<EffectiveOtlpEndpoint> endpoints;
            try
            {
                endpoints = _endpointResolver(provider)
                    ?? throw new InvalidOperationException($"The {typeof(TProvider).Name} endpoint resolver returned null.");
            }
            catch (Exception ex)
            {
                _resolutionFailed = true;
                Log.Warning($"Failed to resolve endpoints from {typeof(TProvider).Name}: {ex.Message}");
                return false;
            }

            _resolutionFailed = false;
            if (_endpoints.SequenceEqual(endpoints))
            {
                return false;
            }

            _endpoints.Clear();
            _endpoints.AddRange(endpoints);
            return true;
        }
    }

    public IReadOnlyList<EffectiveOtlpEndpoint> GetEndpoints()
    {
        lock (_lock)
        {
            if (_resolutionFailed)
            {
                throw new InvalidOperationException(
                    $"The OpenTelemetry SDK {typeof(TProvider).Name} graph could not be inspected.");
            }

            return _endpoints.ToArray();
        }
    }
}
