// <copyright file="ProfilerRuntimeSettings.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal sealed class ProfilerRuntimeSettings
{
    public ProfilerRuntimeSettings(
        bool cpuProfilerEnabled,
        uint cpuProfilerCallStackInterval,
        bool memoryProfilerEnabled,
        uint memoryProfilerMaxMemorySamplesPerMinute,
        bool snapshotsEnabled,
        uint snapshotsSamplingInterval,
        double snapshotsSelectionRate,
        uint profilerExportInterval)
    {
        CpuProfilerEnabled = cpuProfilerEnabled;
        CpuProfilerCallStackInterval = cpuProfilerCallStackInterval;
        MemoryProfilerEnabled = memoryProfilerEnabled;
        MemoryProfilerMaxMemorySamplesPerMinute = memoryProfilerMaxMemorySamplesPerMinute;
        SnapshotsEnabled = snapshotsEnabled;
        SnapshotsSamplingInterval = snapshotsSamplingInterval;
        SnapshotsSelectionRate = snapshotsSelectionRate;
        ProfilerExportInterval = profilerExportInterval;
    }

    public bool CpuProfilerEnabled { get; }

    public uint CpuProfilerCallStackInterval { get; }

    public bool MemoryProfilerEnabled { get; }

    public uint MemoryProfilerMaxMemorySamplesPerMinute { get; }

    public bool SnapshotsEnabled { get; }

    public uint SnapshotsSamplingInterval { get; }

    public double SnapshotsSelectionRate { get; }

    public uint ProfilerExportInterval { get; }

    public static ProfilerRuntimeSettings FromPluginSettings(PluginSettings settings)
    {
        return new ProfilerRuntimeSettings(
            settings.CpuProfilerEnabled,
            settings.CpuProfilerCallStackInterval,
#if NET
            settings.MemoryProfilerEnabled,
            settings.MemoryProfilerMaxMemorySamplesPerMinute,
#else
            false,
            0,
#endif
            settings.SnapshotsEnabled,
            settings.SnapshotsSamplingInterval,
            settings.SnapshotsSelectionRate,
            settings.ProfilerExportInterval);
    }
}
