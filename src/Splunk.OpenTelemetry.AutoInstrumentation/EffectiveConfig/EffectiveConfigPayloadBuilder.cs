// <copyright file="EffectiveConfigPayloadBuilder.cs" company="Splunk Inc.">
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

using System.Globalization;
using System.Text;
using OpenTelemetry.OpAmp.Client.Messages;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Serialization;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal static class EffectiveConfigPayloadBuilder
{
    private const string EnvironmentFileName = "environment";
    private const string EnvironmentContentType = "text/plain; format=properties; vendor=splunk; v=1.0.0";
    private const string YamlContentType = "application/yaml; vendor=splunk; v=1.0.0";

    private const string DefaultEndpoint = "none";

    public static EffectiveConfigFile Build(EffectiveConfigSnapshot snapshot)
    {
        return snapshot.IsFileBasedConfig ? BuildYamlPayload(snapshot) : BuildEnvironmentPayload(snapshot);
    }

    private static EffectiveConfigFile BuildEnvironmentPayload(EffectiveConfigSnapshot snapshot)
    {
        var entries = new (string Key, string Value)[]
        {
            ("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", GetFirstEndpointOrDefault(snapshot.TraceEndpoints, DefaultEndpoint)),
            ("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", GetFirstEndpointOrDefault(snapshot.MetricEndpoints, DefaultEndpoint)),
            ("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", GetFirstEndpointOrDefault(snapshot.LogEndpoints, DefaultEndpoint)),
            ("SPLUNK_PROFILER_ENABLED", FormatBoolean(snapshot.CpuProfilerEnabled)),
            ("SPLUNK_PROFILER_MEMORY_ENABLED", FormatBoolean(snapshot.MemoryProfilerEnabled)),
            ("SPLUNK_SNAPSHOT_PROFILER_ENABLED", FormatBoolean(snapshot.SnapshotProfilerEnabled)),
            ("SPLUNK_SNAPSHOT_PROFILER_SAMPLING_INTERVAL", FormatUInt(snapshot.SnapshotSamplingInterval)),
            ("SPLUNK_PROFILER_CALL_STACK_INTERVAL", FormatUInt(snapshot.CpuProfilerCallStackInterval)),
            ("OTEL_CONFIG_FILE", "null"),
            ("OTEL_EXPERIMENTAL_CONFIG_FILE", "null")
        };

        var body = string.Join("\n", entries.Select(entry => entry.Key + "=" + entry.Value));
        return CreateFile(EnvironmentFileName, EnvironmentContentType, body);
    }

    private static EffectiveConfigFile BuildYamlPayload(EffectiveConfigSnapshot snapshot)
    {
        var yamlConfig = EffectiveYamlConfig.Create(snapshot);

        return CreateFile(
            snapshot.FileBasedConfigFileName!,
            YamlContentType,
            EffectiveYamlSerializer.Serialize(yamlConfig));
    }

    private static EffectiveConfigFile CreateFile(string fileName, string contentType, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        return new EffectiveConfigFile(new ReadOnlyMemory<byte>(bytes), contentType, fileName);
    }

    private static string GetFirstEndpointOrDefault(IReadOnlyList<EffectiveOtlpEndpoint> endpoints, string defaultValue)
    {
        return endpoints.Count == 0 ? defaultValue : endpoints[0].Endpoint;
    }

    private static string FormatBoolean(bool value)
    {
        return value ? "true" : "false";
    }

    private static string FormatUInt(uint value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
