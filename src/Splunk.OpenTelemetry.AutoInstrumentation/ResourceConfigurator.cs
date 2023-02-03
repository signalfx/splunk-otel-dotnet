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

using System.Collections.Generic;
using System.Diagnostics;
using OpenTelemetry.Resources;

using static Splunk.OpenTelemetry.AutoInstrumentation.Helpers.ServiceNameHelper;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal static class ResourceConfigurator
{
    private const string SplunkDistroVersionName = "splunk.distro.version";
    private const string ServiceName = "service.name";

    private static readonly string Version;

    static ResourceConfigurator()
    {
        var assembly = typeof(ResourceConfigurator).Assembly;
        var version = FileVersionInfo.GetVersionInfo(assembly.Location);
        Version = version.FileVersion ?? "unknown";
    }

    public static void Configure(ResourceBuilder resourceBuilder, PluginSettings settings)
    {
        var attributes = new List<KeyValuePair<string, object>>
        {
            new(SplunkDistroVersionName, Version)
        };

        if (!HasServiceName(settings))
        {
            attributes.Add(new(ServiceName, GetGeneratedServiceName()));
        }

        resourceBuilder.AddAttributes(attributes);
    }
}
