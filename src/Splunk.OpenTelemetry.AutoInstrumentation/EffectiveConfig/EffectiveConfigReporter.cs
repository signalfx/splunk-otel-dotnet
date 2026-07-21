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

using OpenTelemetry.OpAmp.Client.Messages;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal sealed class EffectiveConfigReporter
{
    private readonly EffectiveConfigRecorder _recorder;
    private volatile EffectiveProfilerFeatures _profilerFeatures;
    private long _cpuProfilerCallStackInterval;

    private EffectiveConfigReporter(
        EffectiveConfigRecorder recorder,
        EffectiveProfilerFeatures profilerFeatures,
        uint cpuProfilerCallStackInterval)
    {
        _recorder = recorder;
        _profilerFeatures = profilerFeatures;
        _cpuProfilerCallStackInterval = cpuProfilerCallStackInterval;
    }

    public static EffectiveConfigReporter CreateValidated(
        EffectiveConfigRecorder recorder,
        EffectiveProfilerFeatures profilerFeatures,
        uint? cpuProfilerCallStackInterval = null)
    {
        recorder.ValidateCompatibility();
        var snapshot = recorder.CreateSnapshot(profilerFeatures);
        var effectiveCpuProfilerCallStackInterval =
            cpuProfilerCallStackInterval ?? snapshot.CpuProfilerCallStackInterval;
        EffectiveConfigPayloadBuilder.Validate(
            WithCpuProfilerCallStackInterval(snapshot, effectiveCpuProfilerCallStackInterval));
        return new EffectiveConfigReporter(
            recorder,
            profilerFeatures,
            effectiveCpuProfilerCallStackInterval);
    }

    public void UpdateProfilerState(EffectiveProfilerFeatures profilerFeatures, uint? cpuProfilerCallStackInterval = null)
    {
        _profilerFeatures = profilerFeatures;
        if (cpuProfilerCallStackInterval.HasValue)
        {
            Volatile.Write(ref _cpuProfilerCallStackInterval, cpuProfilerCallStackInterval.Value);
        }
    }

    internal EffectiveConfigFile BuildCurrentPayload()
    {
        var snapshot = _recorder.CreateSnapshot(_profilerFeatures);
        var cpuProfilerCallStackInterval = unchecked((uint)Volatile.Read(ref _cpuProfilerCallStackInterval));
        return EffectiveConfigPayloadBuilder.Build(
            WithCpuProfilerCallStackInterval(snapshot, cpuProfilerCallStackInterval));
    }

    private static EffectiveConfigSnapshot WithCpuProfilerCallStackInterval(
        EffectiveConfigSnapshot snapshot,
        uint cpuProfilerCallStackInterval)
    {
        if (snapshot.CpuProfilerCallStackInterval == cpuProfilerCallStackInterval)
        {
            return snapshot;
        }

        return new EffectiveConfigSnapshot(
            fileBasedConfigFileName: snapshot.FileBasedConfigFileName,
            traceEndpoints: snapshot.TraceEndpoints,
            metricEndpoints: snapshot.MetricEndpoints,
            logEndpoints: snapshot.LogEndpoints,
            cpuProfilerEnabled: snapshot.CpuProfilerEnabled,
            memoryProfilerEnabled: snapshot.MemoryProfilerEnabled,
            snapshotProfilerEnabled: snapshot.SnapshotProfilerEnabled,
            cpuProfilerCallStackInterval: cpuProfilerCallStackInterval,
            snapshotSamplingInterval: snapshot.SnapshotSamplingInterval);
    }
}
