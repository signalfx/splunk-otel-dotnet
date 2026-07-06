// <copyright file="EffectiveConfigSnapshot.cs" company="Splunk Inc.">
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

internal sealed class EffectiveConfigSnapshot
{
    internal EffectiveConfigSnapshot(
        string? fileBasedConfigFileName,
        IReadOnlyList<EffectiveOtlpEndpoint> traceEndpoints,
        IReadOnlyList<EffectiveOtlpEndpoint> metricEndpoints,
        IReadOnlyList<EffectiveOtlpEndpoint> logEndpoints,
        bool cpuProfilerEnabled,
        bool memoryProfilerEnabled,
        bool snapshotProfilerEnabled,
        uint cpuProfilerCallStackInterval,
        uint snapshotSamplingInterval,
        string? otelExperimentalConfigFile = null)
    {
        FileBasedConfigFileName = fileBasedConfigFileName;
        OtelExperimentalConfigFile = otelExperimentalConfigFile;
        TraceEndpoints = CopyEndpoints(traceEndpoints);
        MetricEndpoints = CopyEndpoints(metricEndpoints);
        LogEndpoints = CopyEndpoints(logEndpoints);
        CpuProfilerEnabled = cpuProfilerEnabled;
        MemoryProfilerEnabled = memoryProfilerEnabled;
        SnapshotProfilerEnabled = snapshotProfilerEnabled;
        CpuProfilerCallStackInterval = cpuProfilerCallStackInterval;
        SnapshotSamplingInterval = snapshotSamplingInterval;
    }

    public bool IsFileBasedConfig => FileBasedConfigFileName != null;

    public string? FileBasedConfigFileName { get; }

    public string? OtelExperimentalConfigFile { get; }

    public IReadOnlyList<EffectiveOtlpEndpoint> TraceEndpoints { get; }

    public IReadOnlyList<EffectiveOtlpEndpoint> MetricEndpoints { get; }

    public IReadOnlyList<EffectiveOtlpEndpoint> LogEndpoints { get; }

    public bool CpuProfilerEnabled { get; }

    public bool MemoryProfilerEnabled { get; }

    public bool SnapshotProfilerEnabled { get; }

    public uint CpuProfilerCallStackInterval { get; }

    public uint SnapshotSamplingInterval { get; }

    public static EffectiveConfigSnapshot Create(
        EffectiveConfigStaticSettings staticSettings,
        EffectiveProfilerFeatures profilerFeatures,
        IReadOnlyList<EffectiveOtlpEndpoint> traceEndpoints,
        IReadOnlyList<EffectiveOtlpEndpoint> metricEndpoints,
        IReadOnlyList<EffectiveOtlpEndpoint> logEndpoints)
    {
        return new EffectiveConfigSnapshot(
            fileBasedConfigFileName: staticSettings.FileBasedConfigFileName,
            traceEndpoints: traceEndpoints,
            metricEndpoints: metricEndpoints,
            logEndpoints: logEndpoints,
            cpuProfilerEnabled: (profilerFeatures & EffectiveProfilerFeatures.Cpu) != 0,
            memoryProfilerEnabled: (profilerFeatures & EffectiveProfilerFeatures.Memory) != 0,
            snapshotProfilerEnabled: (profilerFeatures & EffectiveProfilerFeatures.Snapshot) != 0,
            cpuProfilerCallStackInterval: staticSettings.CpuProfilerCallStackInterval,
            snapshotSamplingInterval: staticSettings.SnapshotSamplingInterval,
            otelExperimentalConfigFile: staticSettings.OtelExperimentalConfigFile);
    }

    private static IReadOnlyList<EffectiveOtlpEndpoint> CopyEndpoints(IReadOnlyList<EffectiveOtlpEndpoint> endpoints)
    {
        return (endpoints ?? throw new ArgumentNullException(nameof(endpoints))).ToArray();
    }
}
