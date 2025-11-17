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

using static System.Net.WebRequestMethods;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration;

internal class ProfilerConfiguration
{
    [YamlMember(Alias = "memory_enabled")]
    public bool MemoryProfilerEnabled { get; set; } = false;

    [YamlMember(Alias = "logs_endpoint")]
    public string LogsEndpoint { get; set; } = "http://localhost:4318/v1/logs";

    [YamlMember(Alias = "call_stack_interval")]
    public uint CallStackInterval { get; set; } = 10000;

    [YamlMember(Alias = "export_interval")]
    public uint ExportInterval { get; set; } = 500;

    [YamlMember(Alias = "export_timeout")]
    public uint ExportTimeout { get; set; } = 3000;

    [YamlMember(Alias = "max_memory_samples")]
    public uint MaxMemorySamples { get; set; } = 200;
}
