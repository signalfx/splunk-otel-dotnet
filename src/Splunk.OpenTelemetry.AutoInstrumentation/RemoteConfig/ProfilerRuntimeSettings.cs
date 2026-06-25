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

namespace Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

internal sealed class ProfilerRuntimeSettings
{
    public ProfilerRuntimeSettings(
        bool cpuProfilerEnabled,
        uint cpuProfilerCallStackInterval)
    {
        CpuProfilerEnabled = cpuProfilerEnabled;
        CpuProfilerCallStackInterval = cpuProfilerCallStackInterval;
    }

    public bool CpuProfilerEnabled { get; }

    public uint CpuProfilerCallStackInterval { get; }

    public static ProfilerRuntimeSettings FromPluginSettings(PluginSettings settings)
    {
        var cpuProfilerCallStackInterval = settings.CpuProfilerCallStackInterval == 0
            ? Constants.DefaultSamplingInterval
            : settings.CpuProfilerCallStackInterval;

        return new ProfilerRuntimeSettings(
            settings.CpuProfilerEnabled,
            cpuProfilerCallStackInterval);
    }
}
