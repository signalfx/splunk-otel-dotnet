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

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;

internal static class ResourceExpectorExtensions
{
    private static readonly string ExpectedSdkVersion = typeof(global::OpenTelemetry.Resources.Resource).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
    private static readonly string ExpectedDistributionVersion = typeof(Plugin).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version;

    public static void ExpectDistributionResources(this OtlpResourceExpector resourceExpector, string serviceName)
    {
        resourceExpector.Expect("service.name", serviceName);
        resourceExpector.Expect("telemetry.sdk.name", "opentelemetry");
        resourceExpector.Expect("telemetry.sdk.language", "dotnet");
        resourceExpector.Expect("telemetry.sdk.version", ExpectedSdkVersion);
        resourceExpector.Expect("telemetry.auto.version", "1.0.1");
        resourceExpector.Expect("splunk.distro.version", ExpectedDistributionVersion);
    }
}
