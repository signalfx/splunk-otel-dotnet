// <copyright file="ResourceDetectorsData.cs" company="Splunk Inc.">
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

namespace MatrixHelper;

internal static class ResourceDetectorsData
{
    public static ResourceDetector[] GetResourceDetectors()
    {
        return new ResourceDetector[]
        {
            new("AZUREAPPSERVICE", "Azure App Service detector.", new Attribute[] { new("azure.app.service.stamp"), new("cloud.platform"), new("cloud.provider"), new("cloud.resource_id"), new("cloud.region"), new("deployment.environment"), new("host.id"), new("service.instance.id"), new("service.name") }, "beta", "community", new Dependency("Resource Detectors for Azure cloud environments", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.ResourceDetectors.Azure", "https://www.nuget.org/packages/OpenTelemetry.ResourceDetectors.Azure", "1.0.0-beta.4", "beta")),
            new("CONTAINER", "Container detector. For example, Docker or Podman containers.", new Attribute[] { new("container.id") }, "beta", "community", new Dependency("Container Resource Detectors", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.ResourceDetectors.Container", "https://www.nuget.org/packages/OpenTelemetry.ResourceDetectors.Container", "1.0.0-beta.5", "beta")),
            new("HOST", "Host detector.", new Attribute[] { new("host.name") }, "alpha", "community", new Dependency("Host Resource Detectors", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.ResourceDetectors.Host", "https://www.nuget.org/packages/OpenTelemetry.ResourceDetectors.Host", "0.1.0-alpha.2", "alpha")),
            new("PROCESS", "Process detector.", new Attribute[] { new("process.pid") }, "alpha", "community", new Dependency("Process Resource Detectors", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.ResourceDetectors.Process", "https://www.nuget.org/packages/OpenTelemetry.ResourceDetectors.Process", "0.1.0-alpha.2", "alpha")),
            new("PROCESSRUNTIME", "Process Runtime detector.", new Attribute[] { new("process.runtime.description"), new("process.runtime.name"), new("process.runtime.version") }, "alpha", "community", new Dependency("Host Resource Detectors", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.ResourceDetectors.ProcessRuntime", "https://www.nuget.org/packages/OpenTelemetry.ResourceDetectors.ProcessRuntime", "0.1.0-alpha.2", "alpha")),
        };
    }
}
