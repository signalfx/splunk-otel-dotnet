// <copyright file="ResourceConfigurator.cs" company="Splunk Inc.">
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
using OpenTelemetry.Resources;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal static class ResourceConfigurator
{
    /// <summary>
    /// splunk.otel.version is deprecated
    /// </summary>
    private const string SplunkDistroVersionName = "splunk.distro.version";
    private const string TelemetryDistroNameName = "telemetry.distro.name";
    private const string TelemetryDistroNameValue = "splunk-otel-dotnet";
    private const string TelemetryDistroVersionName = "telemetry.distro.version";

    private static readonly string Version;

    static ResourceConfigurator()
    {
        var assemblyInformationalVersion = typeof(ResourceConfigurator).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(assemblyInformationalVersion))
        {
            Version = "unknown";
        }
        else
        {
            var indexOfPlusSign = assemblyInformationalVersion!.IndexOf('+');
            Version = indexOfPlusSign > 0
                ? assemblyInformationalVersion.Substring(0, indexOfPlusSign)
                : assemblyInformationalVersion;
        }
    }

    public static void Configure(ResourceBuilder resourceBuilder, PluginSettings settings)
    {
        var attributes = new List<KeyValuePair<string, object>>
        {
            new(SplunkDistroVersionName, Version),
            new(TelemetryDistroNameName, TelemetryDistroNameValue),
            new(TelemetryDistroVersionName, Version)
        };

        resourceBuilder.AddAttributes(attributes);
    }
}
