// <copyright file="ProfilerRemoteConfigValidator.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

internal static class ProfilerRemoteConfigValidator
{
    public static string? GetValidationError(ProfilerConfiguration? profiling, uint startupSnapshotsSamplingInterval)
    {
        var alwaysOn = profiling?.AlwaysOn;
        if (alwaysOn?.CpuProfiler is { } cpuProfiler)
        {
            var cpuInterval = cpuProfiler.SamplingInterval;
            if (!PluginSettingsHelper.IsValidContinuousSamplingInterval(cpuInterval))
            {
                return $"always_on.cpu_profiler.sampling_interval must be between 1 and {uint.MaxValue}.";
            }

            if (profiling?.Callgraphs != null)
            {
                var snapshotInterval = PluginSettingsHelper.GetFinalSnapshotSamplingInterval(startupSnapshotsSamplingInterval);
                var alignedCpuInterval = PluginSettingsHelper.GetFinalContinuousSamplingInterval(
                    cpuInterval, snapshotsEnabled: true, snapshotsSamplingInterval: snapshotInterval, logAdjustment: false);
                if (alignedCpuInterval != cpuInterval)
                {
                    return $"always_on.cpu_profiler.sampling_interval must be a multiple of the startup Callgraph interval ({snapshotInterval} ms) and at least twice as long. "
                        + $"The aligned interval for {cpuInterval} ms is {alignedCpuInterval} ms.";
                }
            }
        }

        if (alwaysOn?.MemoryProfiler is { MaxMemorySamples: 0 or > Constants.DefaultMaxMemorySamples })
        {
            return $"always_on.memory_profiler.max_memory_samples must be between 1 and {Constants.DefaultMaxMemorySamples}.";
        }

        if (profiling?.Callgraphs is { SelectionProbability: not (> 0 and <= Constants.MaxSnapshotSelectionRate) })
        {
            return $"callgraphs.selection_probability must be greater than 0 and less than or equal to {Constants.MaxSnapshotSelectionRate}.";
        }

        return null;
    }
}
