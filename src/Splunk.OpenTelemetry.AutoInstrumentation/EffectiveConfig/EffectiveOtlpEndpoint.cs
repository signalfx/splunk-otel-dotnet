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
    public EffectiveOtlpEndpoint(string endpoint, EffectiveOtlpExporterType exporterType)
    {
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        ExporterType = exporterType;
    }

    public string Endpoint { get; }

    public EffectiveOtlpExporterType ExporterType { get; }

    public static EffectiveOtlpEndpoint Http(string endpoint)
    {
        return new EffectiveOtlpEndpoint(endpoint, EffectiveOtlpExporterType.HttpProtobuf);
    }

    public static EffectiveOtlpEndpoint Grpc(string endpoint)
    {
        return new EffectiveOtlpEndpoint(endpoint, EffectiveOtlpExporterType.Grpc);
    }

    public bool Equals(EffectiveOtlpEndpoint other)
    {
        return string.Equals(Endpoint, other.Endpoint, StringComparison.Ordinal)
            && ExporterType == other.ExporterType;
    }

    public override bool Equals(object? obj)
    {
        return obj is EffectiveOtlpEndpoint other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Endpoint == null ? 0 : StringComparer.Ordinal.GetHashCode(Endpoint)) * 397) ^ (int)ExporterType;
        }
    }

    public override string ToString()
    {
        return $"{ExporterType}:{Endpoint}";
    }
}
