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

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal sealed class EffectiveConfigState
{
    private const string DefaultFileBasedConfigFileName = "config.yaml";

    private readonly object _lock = new();
    private readonly List<EffectiveOtlpEndpoint> _traceEndpoints = [];
    private readonly List<EffectiveOtlpEndpoint> _metricEndpoints = [];
    private readonly List<EffectiveOtlpEndpoint> _logEndpoints = [];

    private bool _isFileBasedConfig;
    private string _fileBasedConfigFileName = DefaultFileBasedConfigFileName;
    private string? _otelConfigFile;
    private string? _otelExperimentalConfigFile;
    private bool _cpuProfilerEnabled;
    private bool _memoryProfilerEnabled;
    private bool _snapshotProfilerEnabled;
    private uint _cpuProfilerCallStackInterval = Constants.DefaultSamplingInterval;
    private uint _snapshotSamplingInterval = Constants.DefaultSnapshotSamplingIntervalMs;

    public void SetSplunkSettings(PluginSettings settings)
    {
        lock (_lock)
        {
            _isFileBasedConfig = settings.IsFileBasedConfig;
            _fileBasedConfigFileName = settings.FileBasedConfigFileName ?? DefaultFileBasedConfigFileName;
            _otelConfigFile = settings.OtelConfigFile;
            _otelExperimentalConfigFile = settings.OtelExperimentalConfigFile;
            _cpuProfilerEnabled = settings.CpuProfilerEnabled;
#if NET
            _memoryProfilerEnabled = settings.MemoryProfilerEnabled;
#else
            _memoryProfilerEnabled = false;
#endif
            _snapshotProfilerEnabled = settings.SnapshotsEnabled;
            _cpuProfilerCallStackInterval = settings.CpuProfilerCallStackInterval;
            _snapshotSamplingInterval = settings.SnapshotsSamplingInterval;
        }
    }

    public void SetTraceEndpoints(IReadOnlyList<EffectiveOtlpEndpoint> endpoints)
    {
        SetEndpoints(_traceEndpoints, endpoints);
    }

    public void SetMetricEndpoints(IReadOnlyList<EffectiveOtlpEndpoint> endpoints)
    {
        SetEndpoints(_metricEndpoints, endpoints);
    }

    public void SetLogEndpoints(IReadOnlyList<EffectiveOtlpEndpoint> endpoints)
    {
        SetEndpoints(_logEndpoints, endpoints);
    }

    public bool ClearLogEndpoints()
    {
        return ClearEndpoints(_logEndpoints);
    }

    public bool AddLogEndpoint(EffectiveOtlpEndpoint endpoint)
    {
        return AddEndpoint(_logEndpoints, endpoint);
    }

    public EffectiveConfigSnapshot CreateSnapshot()
    {
        lock (_lock)
        {
            return new EffectiveConfigSnapshot(
                isFileBasedConfig: _isFileBasedConfig,
                fileBasedConfigFileName: _fileBasedConfigFileName,
                traceEndpoints: _traceEndpoints,
                metricEndpoints: _metricEndpoints,
                logEndpoints: _logEndpoints,
                cpuProfilerEnabled: _cpuProfilerEnabled,
                memoryProfilerEnabled: _memoryProfilerEnabled,
                snapshotProfilerEnabled: _snapshotProfilerEnabled,
                cpuProfilerCallStackInterval: _cpuProfilerCallStackInterval,
                snapshotSamplingInterval: _snapshotSamplingInterval,
                otelConfigFile: _otelConfigFile,
                otelExperimentalConfigFile: _otelExperimentalConfigFile);
        }
    }

    private void SetEndpoints(List<EffectiveOtlpEndpoint> target, IReadOnlyList<EffectiveOtlpEndpoint> endpoints)
    {
        // Provider graph results replace earlier tentative values from option hooks.
        lock (_lock)
        {
            target.Clear();
            target.AddRange(endpoints);
        }
    }

    private bool ClearEndpoints(List<EffectiveOtlpEndpoint> target)
    {
        lock (_lock)
        {
            var hadEndpoints = target.Count > 0;
            target.Clear();
            return hadEndpoints;
        }
    }

    private bool AddEndpoint(List<EffectiveOtlpEndpoint> target, EffectiveOtlpEndpoint endpoint)
    {
        // File-based config can invoke option hooks once per configured exporter.
        lock (_lock)
        {
            if (!target.Contains(endpoint))
            {
                target.Add(endpoint);
                return true;
            }

            return false;
        }
    }
}
