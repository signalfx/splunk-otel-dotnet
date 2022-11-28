// <copyright file="ExporterTests.cs" company="Splunk Inc.">
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

using System;
using OpenTelemetry.Exporter;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Plugin.Tests
{
    public class ExporterTests
    {
        [Fact]
        public void ConfigureOtlpOptions_EndpointSpecified()
        {
            const string endpoint = "https://specifically-specified-endpoint.com";

            Environment.SetEnvironmentVariable("SPLUNK_REALM", "my-realm");
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", endpoint);

            var settings = PluginSettings.FromDefaultSources();

            var options = new OtlpExporterOptions();
            new Metrics(settings).ConfigureOptions(options);

            options.Endpoint.Should().Be(endpoint);

            Environment.SetEnvironmentVariable("SPLUNK_REALM", null);
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
        }
    }
}
