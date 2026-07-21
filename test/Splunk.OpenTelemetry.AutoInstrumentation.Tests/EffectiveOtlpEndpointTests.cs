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

    [Theory]
    [InlineData("secret-marker")]
    [InlineData("/v1/traces?api_key=secret-marker")]
    [InlineData("ftp://user:secret-marker@collector/v1/traces")]
    [InlineData("http:///v1/traces?api_key=secret-marker")]
    public void Constructor_RejectsInvalidEndpointWithoutDisclosingIt(string invalidEndpoint)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => EffectiveOtlpEndpoint.Http(invalidEndpoint));

        Assert.Equal("endpoint", exception.ParamName);
        Assert.StartsWith(
            "The OTLP endpoint must be an absolute HTTP or HTTPS URI with a host.",
            exception.Message);
        Assert.DoesNotContain("secret-marker", exception.Message);
    }

    [Theory]
    [InlineData("https://user:password@collector:4318/v1/traces", "https://other:secret@collector:4318/v1/traces")]
    [InlineData("https://collector:4318/v1/traces?api_key=first", "https://collector:4318/v1/traces?api_key=second")]
    [InlineData("https://collector:4318/v1/traces#first", "https://collector:4318/v1/traces#second")]
    public void Equality_DistinguishesRedactedEndpointComponents(string firstEndpoint, string secondEndpoint)
    {
        var first = EffectiveOtlpEndpoint.Http(firstEndpoint);
        var second = EffectiveOtlpEndpoint.Http(secondEndpoint);

        Assert.Equal(first.Endpoint, second.Endpoint);
        Assert.NotEqual(first, second);
        Assert.Equal(2, new HashSet<EffectiveOtlpEndpoint> { first, second }.Count);
    }

    [Fact]
    public void Equality_MatchesCanonicalEndpointIdentity()
    {
        var first = EffectiveOtlpEndpoint.Http("https://user:password@COLLECTOR:4318/v1/traces?api_key=secret#fragment");
        var second = EffectiveOtlpEndpoint.Http("https://user:password@collector:4318/v1/traces?api_key=secret#fragment");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void ToString_DoesNotDiscloseRedactedEndpointComponents()
    {
        var endpoint = EffectiveOtlpEndpoint.Http(
            "https://user:password@collector:4318/v1/traces?api_key=secret#fragment-secret");

        var value = endpoint.ToString();

        Assert.Contains("https://collector:4318/v1/traces", value);
        Assert.DoesNotContain("user", value);
        Assert.DoesNotContain("password", value);
        Assert.DoesNotContain("api_key", value);
        Assert.DoesNotContain("fragment-secret", value);
    }

    [Theory]
    [InlineData("http://collector:4318/v1/traces", "https://collector:4318/v1/traces")]
    [InlineData("https://collector:4318/v1/traces", "https://other-collector:4318/v1/traces")]
    [InlineData("https://collector:4318/v1/traces", "https://collector:4319/v1/traces")]
    [InlineData("https://collector:4318/v1/traces", "https://collector:4318/other/traces")]
    public void Equality_DistinguishesDestinationComponents(string firstEndpoint, string secondEndpoint)
    {
        var first = EffectiveOtlpEndpoint.Http(firstEndpoint);
        var second = EffectiveOtlpEndpoint.Http(secondEndpoint);

        Assert.NotEqual(first, second);
    }
}
