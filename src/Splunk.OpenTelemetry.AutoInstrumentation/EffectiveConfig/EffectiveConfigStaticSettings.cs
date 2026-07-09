// <copyright file="EffectiveConfigStaticSettings.cs" company="Splunk Inc.">
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

internal sealed class EffectiveConfigStaticSettings
{
    public EffectiveConfigStaticSettings(PluginSettings settings)
    {
        FileBasedConfigFileName = settings.FileBasedConfigFileName;
        CpuProfilerCallStackInterval = settings.CpuProfilerCallStackInterval;
        SnapshotSamplingInterval = settings.SnapshotsSamplingInterval;
    }

    public string? FileBasedConfigFileName { get; }

    public uint CpuProfilerCallStackInterval { get; }

    public uint SnapshotSamplingInterval { get; }
}
