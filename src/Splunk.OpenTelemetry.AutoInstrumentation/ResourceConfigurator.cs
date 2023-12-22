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

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using OpenTelemetry.Resources;
#if NET6_0_OR_GREATER
using Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;
#endif

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal static class ResourceConfigurator
{
    /// <summary>
    /// splunk.distro.version is deprecated
    /// </summary>
    private const string ServiceNameAttribute = "service.name";
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

#if NET6_0_OR_GREATER
        var resource = resourceBuilder.Build();

        if (resource.Attributes.All(kvp => kvp.Key != ServiceNameAttribute))
        {
            var localResourceBuilder = ResourceBuilder.CreateEmpty();
            localResourceBuilder.AddAttributes(resource.Attributes);
            // service.name was not configured yet use the fallback.
            localResourceBuilder.AddAttributes(new KeyValuePair<string, object>[] { new(ServiceNameAttribute, ServiceNameConfigurator.GetFallbackServiceName()) });
            SampleExporter.SetResources(localResourceBuilder.Build());
        }
        else
        {
            SampleExporter.SetResources(resource);
        }
#endif
    }

#if NET6_0_OR_GREATER
    private static class ServiceNameConfigurator
    {
        internal static string GetFallbackServiceName()
        {
            return Assembly.GetEntryAssembly()?.GetName().Name ?? GetCurrentProcessName();
        }

        /// <summary>
        /// <para>Wrapper around <see cref="Process.GetCurrentProcess"/> and <see cref="Process.ProcessName"/></para>
        /// <para>
        /// On .NET Framework the <see cref="Process"/> class is guarded by a
        /// LinkDemand for FullTrust, so partial trust callers will throw an exception.
        /// This exception is thrown when the caller method is being JIT compiled, NOT
        /// when Process.GetCurrentProcess is called, so this wrapper method allows
        /// us to catch the exception.
        /// </para>
        /// </summary>
        /// <returns>Returns the name of the current process.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetCurrentProcessName()
        {
            using var currentProcess = Process.GetCurrentProcess();
            return currentProcess.ProcessName;
        }
    }
#endif
}
