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
            new("AZUREAPPSERVICE", "Azure App Service detector.", new Attribute[] { new("azure.app.service.stamp"), new("cloud.platform"), new("cloud.provider"), new("cloud.resource_id"), new("cloud.region"), new("deployment.environment"), new("host.id"), new("service.instance.id"), new("service.name"), new("service.version") }, "beta", "community", new Dependency("Resource Detectors for Azure cloud environments", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Resources.Azure", "https://www.nuget.org/packages/OpenTelemetry.Resources.Azure", "1.10.0-beta.1", "beta")),
            new("CONTAINER", "Container detector. For example, Docker or Podman containers. Not supported on .NET Framework.", new Attribute[] { new("container.id") }, "beta", "community", new Dependency("Container Resource Detectors", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Resources.Container", "https://www.nuget.org/packages/OpenTelemetry.Resources.Container", "1.10.0-beta.1", "beta")),
            new("HOST", "Host detector.", new Attribute[] { new("host.id"), new("host.name") }, "alpha", "community", new Dependency("Host Resource Detectors", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Resources.Host", "https://www.nuget.org/packages/OpenTelemetry.Resources.Host", "1.10.0-beta.1", "beta")),
            new("OPERATINGSYSTEM", "Operating System detector.", new Attribute[] { new("os.type"), new("os.build_id"), new("os.description"), new("os.name"), new("os.version") }, "beta", "community", new Dependency("Operating System Detectors", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.OperatingSystem", "https://www.nuget.org/packages/OpenTelemetry.Resources.OperatingSystem", "1.10.0-beta.1", "beta")),
            new("PROCESS", "Process detector.", new Attribute[] { new("process.owner"), new("process.pid") }, "alpha", "community", new Dependency("Process Resource Detectors", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Resources.Process", "https://www.nuget.org/packages/OpenTelemetry.Resources.Process", "1.10.0-beta.1", "beta")),
            new("PROCESSRUNTIME", "Process Runtime detector.", new Attribute[] { new("process.runtime.description"), new("process.runtime.name"), new("process.runtime.version") }, "alpha", "community", new Dependency("Process Runtime Resource Detectors", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Resources.ProcessRuntime", "https://www.nuget.org/packages/OpenTelemetry.Resources.ProcessRuntime", "1.10.0-beta.1", "beta")),
        };
    }
}
