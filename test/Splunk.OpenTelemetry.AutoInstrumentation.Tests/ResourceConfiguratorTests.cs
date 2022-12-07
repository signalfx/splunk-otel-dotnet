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
using System.Linq;
using System.Reflection;
using FluentAssertions.Execution;
using OpenTelemetry.Resources;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class ResourceConfiguratorTests
{
    [Fact]
    public void ResourceConfiguratorConfigure()
    {
        var configuration = new NameValueCollection
        {
            { "SIGNALFX_ENV", "test" },
        };

        var settings = new PluginSettings(new NameValueConfigurationSource(configuration));
        var resourceBuilder = ResourceBuilder.CreateEmpty();

        ResourceConfigurator.Configure(resourceBuilder, settings);

        var resource = resourceBuilder.Build();

        using (new AssertionScope())
        {
            resource.Attributes.Count().Should().Be(2);

            var versionAttribute = resource.Attributes.First();
            versionAttribute.Key.Should().Be("splunk.distro.version");
            (versionAttribute.Value as string).Should().Be(typeof(Plugin).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version);

            var envAttribute = resource.Attributes.Last();
            envAttribute.Key.Should().Be("deployment.environment");
            (envAttribute.Value as string).Should().Be("test");
        }
    }
}
