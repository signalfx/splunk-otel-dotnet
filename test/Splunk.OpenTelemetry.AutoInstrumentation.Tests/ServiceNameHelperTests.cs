// <copyright file="ServiceNameHelperTests.cs" company="Splunk Inc.">
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
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;
using Splunk.OpenTelemetry.AutoInstrumentation.Helpers;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests
{
    public class ServiceNameHelperTests
    {
        [Theory]
        [InlineData("OTEL_SERVICE_NAME", null, false)]
        [InlineData("OTEL_SERVICE_NAME", "", false)]
        [InlineData("OTEL_SERVICE_NAME", "MyServiceName", true)]
        [InlineData("OTEL_RESOURCE_ATTRIBUTES", null, false)]
        [InlineData("OTEL_RESOURCE_ATTRIBUTES", "", false)]
        [InlineData("OTEL_RESOURCE_ATTRIBUTES", "something.else=MyName", false)]
        [InlineData("OTEL_RESOURCE_ATTRIBUTES", "service.name= ", false)]
        [InlineData("OTEL_RESOURCE_ATTRIBUTES", "service.name=MyName", true)]
        [InlineData("OTEL_RESOURCE_ATTRIBUTES", "service.name=MyName,service.namespace=Temp", true)]
        [InlineData("OTEL_RESOURCE_ATTRIBUTES", "service.namespace=Temp,service.name=MyName", true)]
        [InlineData("OTEL_RESOURCE_ATTRIBUTES", "test1.thing=MyName,test2.thing=Temp", false)]
        public void ServiceNameExists(string configKey, string configValue, bool expected)
        {
            var configuration = new NameValueCollection
            {
                { configKey, configValue },
            };

            var settings = new PluginSettings(new NameValueConfigurationSource(configuration));

            var result = ServiceNameHelper.HasServiceName(settings);

            result.Should().Be(expected);
        }
    }
}
