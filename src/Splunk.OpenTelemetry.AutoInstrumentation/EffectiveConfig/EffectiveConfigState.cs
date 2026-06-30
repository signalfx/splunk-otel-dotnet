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
    private readonly object _lock = new();

    private string? _fileBasedConfigFileName;
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
            _fileBasedConfigFileName = settings.FileBasedConfigFileName;
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

    public EffectiveConfigSnapshot CreateSnapshot(
        IReadOnlyList<EffectiveOtlpEndpoint> traceEndpoints,
        IReadOnlyList<EffectiveOtlpEndpoint> metricEndpoints,
        IReadOnlyList<EffectiveOtlpEndpoint> logEndpoints)
    {
        lock (_lock)
        {
            return new EffectiveConfigSnapshot(
                fileBasedConfigFileName: _fileBasedConfigFileName,
                traceEndpoints: traceEndpoints,
                metricEndpoints: metricEndpoints,
                logEndpoints: logEndpoints,
                cpuProfilerEnabled: _cpuProfilerEnabled,
                memoryProfilerEnabled: _memoryProfilerEnabled,
                snapshotProfilerEnabled: _snapshotProfilerEnabled,
                cpuProfilerCallStackInterval: _cpuProfilerCallStackInterval,
                snapshotSamplingInterval: _snapshotSamplingInterval,
                otelExperimentalConfigFile: _otelExperimentalConfigFile);
        }
    }
}
