// <copyright file="MetricsTests.cs" company="Splunk Inc.">
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

using System.Collections.Specialized;
using FluentAssertions.Execution;
using OpenTelemetry.Exporter;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class MetricsTests
{
    [Fact]
    public void ConfigureOtlpOptions()
    {
        var configuration = new NameValueCollection
        {
            { "SPLUNK_REALM", "my-realm" },
            { "SPLUNK_ACCESS_TOKEN", "MyToken" }
        };

        var settings = new PluginSettings(new NameValueConfigurationSource(configuration));

        var options = new OtlpExporterOptions();
        new Metrics(settings).ConfigureMetricsOptions(options);

        using (new AssertionScope())
        {
            options.Endpoint.Should().Be("https://ingest.my-realm.signalfx.com/v2/datapoint/otlp");
            options.Headers.Should().Be("X-Sf-Token=MyToken");
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WhenRealmIsSetRequireAccessToken(string? accessToken)
    {
        var configuration = new NameValueCollection
        {
            { "SPLUNK_REALM", "my-realm" },
            { "SPLUNK_ACCESS_TOKEN", accessToken }
        };

        var settings = new PluginSettings(new NameValueConfigurationSource(configuration));
        var options = new OtlpExporterOptions();
        var loggerMock = Substitute.For<ILogger>();

        new Metrics(settings, loggerMock).ConfigureMetricsOptions(options);

        using (new AssertionScope())
        {
            loggerMock.Received(1).Error(Arg.Any<string>());

            options.Endpoint.ToString().Should().NotContain("my-realm");
            options.Headers.Should().BeNull();
        }
    }
}
