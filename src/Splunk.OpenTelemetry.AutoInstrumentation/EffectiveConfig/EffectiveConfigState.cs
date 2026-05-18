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
    private const string ServiceName = "OTEL_SERVICE_NAME";
    private const string TracesEndpoints = "OTEL_EXPORTER_OTLP_TRACES_ENDPOINTS";
    private const string MetricsEndpoints = "OTEL_EXPORTER_OTLP_METRICS_ENDPOINTS";
    private const string LogsEndpoints = "OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS";
    private const string CpuProfilerEnabled = "SPLUNK_PROFILER_ENABLED";
    private const string MemoryProfilerEnabled = "SPLUNK_PROFILER_MEMORY_ENABLED";
    private const string CpuProfilerCallStackInterval = "SPLUNK_PROFILER_CALL_STACK_INTERVAL";
    private const string ProfilerLogsEndpoint = "SPLUNK_PROFILER_LOGS_ENDPOINT";
    private const string SnapshotProfilerEnabled = "SPLUNK_SNAPSHOT_PROFILER_ENABLED";
    private const string SnapshotSamplingInterval = "SPLUNK_SNAPSHOT_PROFILER_SAMPLING_INTERVAL";

    private static readonly string[] KeyOrder =
    [
        ServiceName,
        TracesEndpoints,
        MetricsEndpoints,
        LogsEndpoints,
        CpuProfilerEnabled,
        MemoryProfilerEnabled,
        CpuProfilerCallStackInterval,
        ProfilerLogsEndpoint,
        SnapshotProfilerEnabled,
        SnapshotSamplingInterval
    ];

    // Provider hooks and late OpAmp updates are not guaranteed to stay on one thread.
    private readonly object _lock = new();
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _endpoints = new(StringComparer.Ordinal);

    public void SetSplunkSettings(PluginSettings settings)
    {
        lock (_lock)
        {
            _values[CpuProfilerEnabled] = EffectiveConfigValueFormatter.FormatBoolean(settings.CpuProfilerEnabled);
#if NET
            var memoryProfilerEnabled = settings.MemoryProfilerEnabled;
#else
            var memoryProfilerEnabled = false;
#endif
            _values[MemoryProfilerEnabled] = EffectiveConfigValueFormatter.FormatBoolean(memoryProfilerEnabled);
            _values[SnapshotProfilerEnabled] = EffectiveConfigValueFormatter.FormatBoolean(settings.SnapshotsEnabled);

            if (settings.CpuProfilerEnabled)
            {
                _values[CpuProfilerCallStackInterval] = EffectiveConfigValueFormatter.FormatMilliseconds(settings.CpuProfilerCallStackInterval);
            }
            else
            {
                _values.Remove(CpuProfilerCallStackInterval);
            }

            if (settings.CpuProfilerEnabled || memoryProfilerEnabled || settings.SnapshotsEnabled)
            {
                _values[ProfilerLogsEndpoint] = EffectiveConfigValueFormatter.FormatList([settings.ProfilerLogsEndpoint.ToString()]);
            }
            else
            {
                _values.Remove(ProfilerLogsEndpoint);
            }

            if (settings.SnapshotsEnabled)
            {
                _values[SnapshotSamplingInterval] = EffectiveConfigValueFormatter.FormatMilliseconds(settings.SnapshotsSamplingInterval);
            }
            else
            {
                _values.Remove(SnapshotSamplingInterval);
            }
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
            if (!_values.ContainsKey(ServiceName))
            {
                _values[ServiceName] = formattedServiceName;
            }
        }
    }

    public void SetTraceEndpoints(IReadOnlyList<string> endpoints)
    {
        SetEndpoints(TracesEndpoints, endpoints);
    }

    public void SetMetricEndpoints(IReadOnlyList<string> endpoints)
    {
        SetEndpoints(MetricsEndpoints, endpoints);
    }

    public void SetLogEndpoints(IReadOnlyList<string> endpoints)
    {
        SetEndpoints(LogsEndpoints, endpoints);
    }

    public bool ClearLogEndpoints()
    {
        return ClearEndpoints(LogsEndpoints);
    }

    public bool AddLogEndpoint(string endpoint)
    {
        return AddEndpoint(LogsEndpoints, endpoint);
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
                    payload.Append('\n');
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
