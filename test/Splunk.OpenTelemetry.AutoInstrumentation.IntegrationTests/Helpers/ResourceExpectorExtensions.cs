// <copyright file="ResourceExpectorExtensions.cs" company="Splunk Inc.">
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

using System.Reflection;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Resource.V1;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;

internal static class ResourceExpectorExtensions
{
    private static readonly string ExpectedSdkVersion = typeof(global::OpenTelemetry.Resources.Resource).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
    private static readonly string ExpectedDistributionVersion = typeof(Plugin).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];

    public static void ExpectDistributionResources(this OtlpResourceExpector resourceExpector, string serviceName)
    {
        resourceExpector.Expect("service.name", serviceName);
        resourceExpector.Matches("service.instance.id", "^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$");
        resourceExpector.Expect("telemetry.sdk.name", "opentelemetry");
        resourceExpector.Expect("telemetry.sdk.language", "dotnet");
        resourceExpector.Expect("telemetry.sdk.version", ExpectedSdkVersion);
        resourceExpector.Expect("telemetry.distro.name", "splunk-otel-dotnet");
        resourceExpector.Expect("telemetry.distro.version", ExpectedDistributionVersion);
        resourceExpector.Expect("splunk.distro.version", ExpectedDistributionVersion);
    }

    internal static void AssertProfileResources(Resource resource, string serviceName)
    {
        // asserting resource attribute with values
        var constantAttributes = new List<KeyValue>
        {
            new() { Key = "service.name", Value = new AnyValue { StringValue = serviceName } },
            new() { Key = "telemetry.sdk.name", Value = new AnyValue { StringValue = "opentelemetry" } },
            new() { Key = "telemetry.sdk.language", Value = new AnyValue { StringValue = "dotnet" } },
            new() { Key = "telemetry.distro.name", Value = new AnyValue { StringValue = "splunk-otel-dotnet" } },
            new() { Key = "telemetry.distro.version", Value = new AnyValue { StringValue = ExpectedDistributionVersion } },
            new() { Key = "splunk.distro.version", Value = new AnyValue { StringValue = ExpectedDistributionVersion } }
        };

        foreach (var constantAttribute in constantAttributes)
        {
            Assert.Contains(constantAttribute, resource.Attributes);
        }

        // asserting resource attribute without values
        Assert.Single(resource.Attributes, value => value.Key == "host.name");
        Assert.Single(resource.Attributes, value => value.Key == "process.pid");
    }
}
