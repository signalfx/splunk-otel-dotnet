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

    private enum OtlpSignal
    {
        Traces,
        Metrics,
        Logs
    }

    public static EffectiveConfigFile Build(EffectiveConfigSnapshot snapshot)
    {
        return CreateFile(BuildValidatedContent(snapshot));
    }

    public static void Validate(EffectiveConfigSnapshot snapshot)
    {
        _ = BuildValidatedContent(snapshot);
    }

    public static void ValidateCompatibility(bool isFileBasedConfig)
    {
        if (isFileBasedConfig)
        {
            EffectiveYamlSerializer.ValidateCompatibility();
        }
    }

    internal static EffectiveConfigFile CreateFile(string fileName, string contentType, string body)
    {
        return CreateFile(CreateValidatedContent(fileName, contentType, body));
    }

    private static EffectiveConfigFile CreateFile(ValidatedPayloadContent content)
    {
        var bytes = new byte[content.Utf8ByteCount];
        Encoding.UTF8.GetBytes(content.Body, 0, content.Body.Length, bytes, 0);
        return new EffectiveConfigFile(new ReadOnlyMemory<byte>(bytes), content.ContentType, content.FileName);
    }

    private static ValidatedPayloadContent CreateValidatedContent(
        string fileName,
        string contentType,
        string body)
    {
        var bodySizeBytes = Encoding.UTF8.GetByteCount(body);
        EffectiveConfigLimits.ValidateFileContentSize(bodySizeBytes);

        return new ValidatedPayloadContent(fileName, contentType, body, bodySizeBytes);
    }

    private static void ValidateEnvironmentBodySize(
        IReadOnlyList<(string Key, string Value)> entries)
    {
        long bodySizeBytes = entries.Count == 0 ? 0 : entries.Count - 1;
        foreach (var entry in entries)
        {
            bodySizeBytes += Encoding.UTF8.GetByteCount(entry.Key);
            bodySizeBytes += 1; // '=' separator
            bodySizeBytes += Encoding.UTF8.GetByteCount(entry.Value);
        }

        EffectiveConfigLimits.ValidateFileContentSize(bodySizeBytes);
    }

    private static ValidatedPayloadContent BuildValidatedContent(EffectiveConfigSnapshot snapshot)
    {
        return snapshot.IsFileBasedConfig
            ? BuildValidatedYamlContent(snapshot)
            : BuildValidatedEnvironmentContent(snapshot);
    }

    private static ValidatedPayloadContent BuildValidatedEnvironmentContent(EffectiveConfigSnapshot snapshot)
    {
        var entries = new (string Key, string Value)[]
        {
            ("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", GetSingleEndpointOrDefault(snapshot.TraceEndpoints, OtlpSignal.Traces, DefaultEndpoint)),
            ("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", GetSingleEndpointOrDefault(snapshot.MetricEndpoints, OtlpSignal.Metrics, DefaultEndpoint)),
            ("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", GetSingleEndpointOrDefault(snapshot.LogEndpoints, OtlpSignal.Logs, DefaultEndpoint)),
            ("SPLUNK_PROFILER_ENABLED", FormatBoolean(snapshot.CpuProfilerEnabled)),
            ("SPLUNK_PROFILER_MEMORY_ENABLED", FormatBoolean(snapshot.MemoryProfilerEnabled)),
            ("SPLUNK_SNAPSHOT_PROFILER_ENABLED", FormatBoolean(snapshot.SnapshotProfilerEnabled)),
            ("SPLUNK_SNAPSHOT_PROFILER_SAMPLING_INTERVAL", FormatUInt(snapshot.SnapshotSamplingInterval)),
            ("SPLUNK_PROFILER_CALL_STACK_INTERVAL", FormatUInt(snapshot.CpuProfilerCallStackInterval)),
            ("OTEL_CONFIG_FILE", "null")
        };

        ValidateEnvironmentBodySize(entries);
        var body = string.Join("\n", entries.Select(entry => entry.Key + "=" + entry.Value));
        return CreateValidatedContent(EnvironmentFileName, EnvironmentContentType, body);
    }

    private static ValidatedPayloadContent BuildValidatedYamlContent(EffectiveConfigSnapshot snapshot)
    {
        var yamlConfig = EffectiveYamlConfig.Create(snapshot);

        return CreateValidatedContent(
            snapshot.FileBasedConfigFileName!,
            YamlContentType,
            EffectiveYamlSerializer.Serialize(
                yamlConfig,
                EffectiveConfigLimits.MaxFileContentSizeBytes));
    }

    private static string GetSingleEndpointOrDefault(
        IReadOnlyList<EffectiveOtlpEndpoint> endpoints,
        OtlpSignal signal,
        string defaultValue)
    {
        if (endpoints.Count > 1)
        {
            throw new InvalidOperationException(
                $"Environment effective configuration cannot represent {endpoints.Count} active {signal.ToString().ToLowerInvariant()} endpoints.");
        }

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

    private readonly struct ValidatedPayloadContent
    {
        public ValidatedPayloadContent(
            string fileName,
            string contentType,
            string body,
            int utf8ByteCount)
        {
            FileName = fileName;
            ContentType = contentType;
            Body = body;
            Utf8ByteCount = utf8ByteCount;
        }

        public string FileName { get; }

        public string ContentType { get; }

        public string Body { get; }

        public int Utf8ByteCount { get; }
    }
}
