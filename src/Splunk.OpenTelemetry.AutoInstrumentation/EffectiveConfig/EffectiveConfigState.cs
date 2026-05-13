// <copyright file="EffectiveConfigState.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal sealed class EffectiveConfigState
{
    private static readonly string[] KeyOrder =
    [
        EffectiveConfigKeys.ServiceName,
        EffectiveConfigKeys.TracesEndpoints,
        EffectiveConfigKeys.MetricsEndpoints,
        EffectiveConfigKeys.LogsEndpoints,
        EffectiveConfigKeys.CpuProfilerEnabled,
        EffectiveConfigKeys.MemoryProfilerEnabled,
        EffectiveConfigKeys.CpuProfilerCallStackInterval,
        EffectiveConfigKeys.ProfilerLogsEndpoint,
        EffectiveConfigKeys.SnapshotProfilerEnabled,
        EffectiveConfigKeys.SnapshotSamplingInterval
    ];

    // Provider hooks and late OpAmp updates are not guaranteed to stay on one thread.
    private readonly object _lock = new();
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _endpoints = new(StringComparer.Ordinal);

    public void SetSplunkSettings(PluginSettings settings)
    {
        lock (_lock)
        {
            _values[EffectiveConfigKeys.CpuProfilerEnabled] = EffectiveConfigValueFormatter.FormatBoolean(settings.CpuProfilerEnabled);
#if NET
            _values[EffectiveConfigKeys.MemoryProfilerEnabled] = EffectiveConfigValueFormatter.FormatBoolean(settings.MemoryProfilerEnabled);
#else
            _values[EffectiveConfigKeys.MemoryProfilerEnabled] = EffectiveConfigValueFormatter.FormatBoolean(false);
#endif
            _values[EffectiveConfigKeys.CpuProfilerCallStackInterval] = EffectiveConfigValueFormatter.FormatMilliseconds(settings.CpuProfilerCallStackInterval);
            _values[EffectiveConfigKeys.ProfilerLogsEndpoint] = EffectiveConfigValueFormatter.FormatList([settings.ProfilerLogsEndpoint.ToString()]);
            _values[EffectiveConfigKeys.SnapshotProfilerEnabled] = EffectiveConfigValueFormatter.FormatBoolean(settings.SnapshotsEnabled);
            _values[EffectiveConfigKeys.SnapshotSamplingInterval] = EffectiveConfigValueFormatter.FormatMilliseconds(settings.SnapshotsSamplingInterval);
        }
    }

    public void TrySetServiceName(string serviceName)
    {
        var formattedServiceName = EffectiveConfigValueFormatter.TryFormatString(serviceName);
        if (formattedServiceName == null)
        {
            return;
        }

        lock (_lock)
        {
            if (!_values.ContainsKey(EffectiveConfigKeys.ServiceName))
            {
                _values[EffectiveConfigKeys.ServiceName] = formattedServiceName;
            }
        }
    }

    public void SetTraceEndpoints(IReadOnlyList<string> endpoints)
    {
        SetEndpoints(EffectiveConfigKeys.TracesEndpoints, endpoints);
    }

    public void SetMetricEndpoints(IReadOnlyList<string> endpoints)
    {
        SetEndpoints(EffectiveConfigKeys.MetricsEndpoints, endpoints);
    }

    public void SetLogEndpoints(IReadOnlyList<string> endpoints)
    {
        SetEndpoints(EffectiveConfigKeys.LogsEndpoints, endpoints);
    }

    public bool ClearLogEndpoints()
    {
        return ClearEndpoints(EffectiveConfigKeys.LogsEndpoints);
    }

    public bool AddLogEndpoint(string endpoint)
    {
        return AddEndpoint(EffectiveConfigKeys.LogsEndpoints, endpoint);
    }

    public string BuildPayload()
    {
        lock (_lock)
        {
            var payload = new StringBuilder();
            foreach (var key in KeyOrder)
            {
                var value = TryGetValue(key);
                if (value == null)
                {
                    continue;
                }

                if (payload.Length > 0)
                {
                    payload.AppendLine();
                }

                payload.Append(key);
                payload.Append('=');
                payload.Append(value);
            }

            return payload.ToString();
        }
    }

    private void SetEndpoints(string key, IReadOnlyList<string> endpoints)
    {
        // Provider graph results replace earlier tentative values from option hooks.
        lock (_lock)
        {
            if (endpoints.Count == 0)
            {
                _endpoints.Remove(key);
                return;
            }

            _endpoints[key] = endpoints.ToList();
        }
    }

    private bool ClearEndpoints(string key)
    {
        lock (_lock)
        {
            return _endpoints.Remove(key);
        }
    }

    private bool AddEndpoint(string key, string endpoint)
    {
        // File-based config can invoke option hooks once per configured exporter.
        lock (_lock)
        {
            if (!_endpoints.TryGetValue(key, out var endpoints))
            {
                endpoints = [];
                _endpoints[key] = endpoints;
            }

            if (!endpoints.Contains(endpoint, StringComparer.Ordinal))
            {
                endpoints.Add(endpoint);
                return true;
            }

            return false;
        }
    }

    private string? TryGetValue(string key)
    {
        if (_endpoints.TryGetValue(key, out var endpoints))
        {
            return EffectiveConfigValueFormatter.FormatList(endpoints);
        }

        return _values.TryGetValue(key, out var value) ? value : null;
    }
}
