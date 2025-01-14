// <copyright file="TracesTests.cs" company="Splunk Inc.">
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
using OpenTelemetry.Exporter;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class TracesTests
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
        new Traces(settings).ConfigureTracesOptions(options);

        Assert.Equal("https://ingest.my-realm.signalfx.com/v2/trace/otlp", options.Endpoint.ToString());
        Assert.Equal("X-Sf-Token=MyToken", options.Headers);
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

        new Traces(settings, loggerMock).ConfigureTracesOptions(options);

        loggerMock.Received().Error(Arg.Any<string>());

        Assert.DoesNotContain("my-realm", options.Endpoint.ToString());
        Assert.Null(options.Headers);
    }
}
