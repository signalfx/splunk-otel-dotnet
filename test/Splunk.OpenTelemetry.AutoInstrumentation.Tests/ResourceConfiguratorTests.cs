// <copyright file="ResourceConfiguratorTests.cs" company="Splunk Inc.">
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
using System.Reflection;
using OpenTelemetry.Resources;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class ResourceConfiguratorTests
{
    [Fact]
    public void ConfigureSplunkDistributionVersion()
    {
        var resourceBuilder = ResourceBuilder.CreateEmpty();

        var configuration = new NameValueCollection
        {
            { "OTEL_SERVICE_NAME", "MyServiceName" },
        };

        var settings = new PluginSettings(new NameValueConfigurationSource(configuration));

        ResourceConfigurator.Configure(resourceBuilder, settings);

        var resource = resourceBuilder.Build();
        var version = typeof(Plugin).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];

        var expected = new Dictionary<string, object>
        {
            { "splunk.distro.version", version },
            { "telemetry.distro.name", "splunk-otel-dotnet" },
            { "telemetry.distro.version", version }
        };

        Assert.Equivalent(expected, resource.Attributes);
    }
}
