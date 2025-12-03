// <copyright file="ProfilerConfiguration.cs" company="Splunk Inc.">
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

using Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration.Utils;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration;

[EmptyObjectOnEmptyYaml]
internal class ProfilerConfiguration
{
    public uint ScheduleDelay { get; set; } = 500;

    public uint ExportTimeout { get; set; } = 3000;

    public ExporterConfig Exporter { get; set; } = new ExporterConfig();

    public MemoryAllocationSampling? MemoryAllocationSampling { get; set; }

    public CallStackSampling? CallStackSampling { get; set; }

    public CallGraphsConfiguration? Snapshotting { get; set; }
}
