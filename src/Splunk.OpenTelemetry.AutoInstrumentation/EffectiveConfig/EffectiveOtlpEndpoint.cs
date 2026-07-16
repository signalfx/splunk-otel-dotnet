// <copyright file="EffectiveOtlpEndpoint.cs" company="Splunk Inc.">
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

internal readonly struct EffectiveOtlpEndpoint : IEquatable<EffectiveOtlpEndpoint>
{
    private const string HiddenEndpoint = "<hidden>";

    public EffectiveOtlpEndpoint(
        string endpoint,
        EffectiveOtlpExporterType exporterType,
        EffectiveOtlpPipelineType pipelineType)
    {
        endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        EffectiveConfigLimits.ValidateEndpointLength(endpoint);
        Endpoint = SanitizeEndpoint(endpoint);
        ExporterType = exporterType;
        PipelineType = pipelineType;
    }

    public string Endpoint { get; }

    public EffectiveOtlpExporterType ExporterType { get; }

    public EffectiveOtlpPipelineType PipelineType { get; }

    public static EffectiveOtlpEndpoint Http(
        string endpoint,
        EffectiveOtlpPipelineType pipelineType = EffectiveOtlpPipelineType.Batch)
    {
        return new EffectiveOtlpEndpoint(endpoint, EffectiveOtlpExporterType.HttpProtobuf, pipelineType);
    }

    public static EffectiveOtlpEndpoint Grpc(
        string endpoint,
        EffectiveOtlpPipelineType pipelineType = EffectiveOtlpPipelineType.Batch)
    {
        return new EffectiveOtlpEndpoint(endpoint, EffectiveOtlpExporterType.Grpc, pipelineType);
    }

    public bool Equals(EffectiveOtlpEndpoint other)
    {
        return string.Equals(Endpoint, other.Endpoint, StringComparison.Ordinal)
            && ExporterType == other.ExporterType
            && PipelineType == other.PipelineType;
    }

    public override bool Equals(object? obj)
    {
        return obj is EffectiveOtlpEndpoint other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Endpoint == null ? 0 : StringComparer.Ordinal.GetHashCode(Endpoint);
            hashCode = (hashCode * 397) ^ (int)ExporterType;
            return (hashCode * 397) ^ (int)PipelineType;
        }
    }

    public override string ToString()
    {
        return $"{PipelineType}:{ExporterType}:{Endpoint}";
    }

    private static string SanitizeEndpoint(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            return HiddenEndpoint;
        }

        if (string.IsNullOrEmpty(endpointUri.UserInfo) &&
            string.IsNullOrEmpty(endpointUri.Query) &&
            string.IsNullOrEmpty(endpointUri.Fragment))
        {
            return endpoint;
        }

        var redactedEndpoint = new UriBuilder(endpointUri)
        {
            UserName = string.Empty,
            Password = string.Empty,
            Query = string.Empty,
            Fragment = string.Empty,
        };
        return redactedEndpoint.Uri.AbsoluteUri;
    }
}
