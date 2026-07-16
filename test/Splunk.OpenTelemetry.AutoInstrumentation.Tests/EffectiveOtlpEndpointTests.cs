// <copyright file="EffectiveOtlpEndpointTests.cs" company="Splunk Inc.">
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

using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class EffectiveOtlpEndpointTests
{
    [Fact]
    public void Constructor_RedactsEndpointCredentials()
    {
        var endpoint = new EffectiveOtlpEndpoint(
            "https://user:password@collector:4318/v1/traces?api_key=secret#fragment-secret",
            EffectiveOtlpExporterType.HttpProtobuf,
            EffectiveOtlpPipelineType.Batch);

        Assert.Equal("https://collector:4318/v1/traces", endpoint.Endpoint);
    }

    [Fact]
    public void Constructor_HidesMalformedEndpoint()
    {
        var endpoint = EffectiveOtlpEndpoint.Http("password=secret");

        Assert.Equal("<hidden>", endpoint.Endpoint);
    }

    [Fact]
    public void Constructor_RejectsEndpointOverLengthLimit()
    {
        const string prefix = "http://collector/";
        var oversizedEndpoint = prefix + new string(
            'a',
            EffectiveConfigLimits.MaxEndpointLength - prefix.Length + 1);

        var exception = Assert.Throws<InvalidOperationException>(
            () => EffectiveOtlpEndpoint.Http(oversizedEndpoint));

        Assert.Contains(EffectiveConfigLimits.MaxEndpointLength.ToString(), exception.Message);
    }

    [Fact]
    public void Equality_UsesSanitizedEndpoint()
    {
        var first = EffectiveOtlpEndpoint.Http("https://user:first@collector:4318/v1/traces?api_key=first");
        var second = EffectiveOtlpEndpoint.Http("https://user:second@collector:4318/v1/traces?api_key=second");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
