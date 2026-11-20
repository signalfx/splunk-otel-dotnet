// <copyright file="ProfilerRemoteConfigState.cs" company="Splunk Inc.">
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

using Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration;
using Splunk.OpenTelemetry.AutoInstrumentation.Snapshots;

namespace Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

internal sealed class ProfilerRemoteConfigState
{
    public ProfilerRemoteConfigState(PluginSettings settings)
        : this(
            settings.CpuProfilerEnabled,
            settings.CpuProfilerCallStackInterval,
            settings.SnapshotsEnabled,
            settings.SnapshotsSamplingInterval,
            settings.SnapshotsSelectionRate,
#if NET
            settings.MemoryProfilerEnabled,
            PluginSettingsHelper.GetFinalMaxMemorySamples(settings))
#else
            memoryProfilerEnabled: false,
            memoryProfilerMaxMemorySamplesPerMinute: 0)
#endif
    {
    }

    private ProfilerRemoteConfigState(
        bool cpuProfilerEnabled,
        long cpuProfilerCallStackInterval,
        bool snapshotsEnabled,
        uint snapshotsSamplingInterval,
        double snapshotsSelectionRate,
        bool memoryProfilerEnabled,
        uint memoryProfilerMaxMemorySamplesPerMinute)
    {
        CpuProfilerEnabled = cpuProfilerEnabled;
        SnapshotsEnabled = snapshotsEnabled;
        SnapshotsSamplingInterval = PluginSettingsHelper.GetFinalSnapshotSamplingInterval(snapshotsSamplingInterval);
        CpuProfilerCallStackInterval = PluginSettingsHelper.GetFinalContinuousSamplingInterval(
            cpuProfilerCallStackInterval, snapshotsEnabled, SnapshotsSamplingInterval);
        SnapshotsSelectionRate = PluginSettingsHelper.GetFinalSnapshotSelectionProbability(snapshotsSelectionRate);
        SnapshotSelector = new TraceIdBasedSnapshotSelector(SnapshotsSelectionRate);
        MemoryProfilerEnabled = memoryProfilerEnabled;
        MemoryProfilerMaxMemorySamplesPerMinute = memoryProfilerMaxMemorySamplesPerMinute;
    }

    public bool CpuProfilerEnabled { get; }

    public uint CpuProfilerCallStackInterval { get; }

    public bool MemoryProfilerEnabled { get; }

    public uint MemoryProfilerMaxMemorySamplesPerMinute { get; }

    public bool SnapshotsEnabled { get; }

    public uint SnapshotsSamplingInterval { get; }

    public double SnapshotsSelectionRate { get; }

    public ISnapshotSelector SnapshotSelector { get; }

    public static ProfilerRemoteConfigState FromRemoteConfiguration(YamlRoot? configuration, PluginSettings startupSettings)
    {
        var profiling = configuration?.Distribution?.Splunk?.Profiling;
        var snapshotsSamplingInterval = startupSettings.SnapshotsSamplingInterval;
        if (ProfilerRemoteConfigValidator.GetValidationError(profiling, snapshotsSamplingInterval) is { } error)
        {
            throw new ValidationException(error);
        }

        var cpuProfiler = profiling?.AlwaysOn?.CpuProfiler;
        var callgraphs = profiling?.Callgraphs;
#if NET
        var memoryProfiler = profiling?.AlwaysOn?.MemoryProfiler;
#endif
        return new ProfilerRemoteConfigState(
            cpuProfiler != null,
            cpuProfiler?.SamplingInterval ?? Constants.DefaultSamplingInterval,
            callgraphs != null,
            snapshotsSamplingInterval,
            callgraphs?.SelectionProbability ?? Constants.DefaultSnapshotSelectionRate,
#if NET
            memoryProfiler != null,
            memoryProfiler != null ? PluginSettingsHelper.GetFinalMaxMemorySamples(memoryProfiler.MaxMemorySamples) : 0);
#else
            memoryProfilerEnabled: false,
            memoryProfilerMaxMemorySamplesPerMinute: 0);
#endif
    }

    internal sealed class ValidationException(string message) : FormatException(message);
}
