// <copyright file="MetadataData.cs" company="Splunk Inc.">
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

namespace MatrixHelper;

internal static class MetadataData
{
    public static AllInOne GetAllInOne()
    {
        return new AllInOne(
            component: "Splunk Distribution of OpenTelemetry .NET",
            version: typeof(MetadataData).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0],
            dependencies: new Dependency[]
            {
                new("OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet", null, "1.6.0", "stable"),
                new("OpenTelemetry .NET Automatic Instrumentation", "https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation", null, "1.0.2", "stable"),
            },
            settings: SettingsData.GetSettings(),
            instrumentations: InstrumentationData.GetInstrumentations(),
            resourceDetectors: ResourceDetectorsData.GetResourceDetectors());
    }
}
