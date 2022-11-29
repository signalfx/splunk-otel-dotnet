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
using OpenTelemetry.Trace;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class TracesTests
{
    [Fact]
    public void ConfigureTracerProvider()
    {
        var builder = Mock.Of<TracerProviderBuilder>();
        var returnedBuilder = new Traces().ConfigureTracerProvider(builder);
        returnedBuilder.Should().Be(builder);
    }

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

        options.Endpoint.Should().Be("https://ingest.my-realm.signalfx.com/v2/trace");
        options.Headers.Should().Be("X-Sf-Token=MyToken");
    }
}
