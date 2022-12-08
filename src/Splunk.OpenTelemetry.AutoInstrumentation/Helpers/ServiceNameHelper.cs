// <copyright file="ServiceNameHelper.cs" company="Splunk Inc.">
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Helpers
{
    internal class ServiceNameHelper
    {
        private static readonly ILogger Log = new Logger();

        public static bool HasServiceName(PluginSettings settings)
        {
            return !string.IsNullOrEmpty(settings.ServiceName) || ResourceHasServiceName(settings.ResourceAttributes);

            bool ResourceHasServiceName(IEnumerable<KeyValuePair<string, string>> resources)
            {
                const string key = "service.name";

                var serviceName = settings.ResourceAttributes
                    .FirstOrDefault(x => x.Key == key)
                    .Value;

                return !string.IsNullOrEmpty(serviceName);
            }
        }

        public static string GetGeneratedServiceName()
        {
            try
            {
#if NETFRAMEWORK
                try
                {
                    if (TryLoadAspNetSiteName(out var siteName))
                    {
                        return siteName!;
                    }
                }
                catch (Exception ex)
                {
                    // Unable to call into System.Web.dll
                    Log.Error(ex, "Unable to get application name through ASP.NET settings");
                }
#endif

                var serviceName = Assembly.GetEntryAssembly()?.GetName().Name ??
                       Process.GetCurrentProcess()?.ProcessName;

                if (!string.IsNullOrEmpty(serviceName))
                {
                    return serviceName!;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating default service name.");
            }

            return "Unkown";
        }

#if NETFRAMEWORK
        private static bool TryLoadAspNetSiteName(out string? siteName)
        {
            // System.Web.dll is only available on .NET Framework
            if (System.Web.Hosting.HostingEnvironment.IsHosted)
            {
                // if this app is an ASP.NET application, return "SiteName/ApplicationVirtualPath".
                // note that ApplicationVirtualPath includes a leading slash.
                siteName = (System.Web.Hosting.HostingEnvironment.SiteName + System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath).TrimEnd('/');
                return true;
            }

            siteName = default;
            return false;
        }
#endif
    }
}
