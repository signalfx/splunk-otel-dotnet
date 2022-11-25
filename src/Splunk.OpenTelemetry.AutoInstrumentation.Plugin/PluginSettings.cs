// <copyright file="PluginSettings.cs" company="Splunk Inc.">
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
using Splunk.OpenTelemetry.AutoInstrumentation.Plugin.Configuration;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Plugin;

internal class PluginSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginSettings"/> class
    /// using the specified <see cref="IConfigurationSource"/> to initialize values.
    /// </summary>
    /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
    internal PluginSettings(IConfigurationSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        Realm = source.GetString(ConfigurationKeys.Realm);
        AccessToken = source.GetString(ConfigurationKeys.AccessToken);
        IsOtlpEndpointSet = source.GetString(ConfigurationKeys.OpenTelemetry.OtlpEndpoint) != null;
    }

    public bool IsOtlpEndpointSet { get; set; }

    public string? Realm { get; set; }

    public string? AccessToken { get; set; }

    public static PluginSettings FromDefaultSources()
    {
        var configurationSource = new CompositeConfigurationSource
        {
            new EnvironmentConfigurationSource(),

/*
* TODO: Enable ASP.NET web.config support
*
#if NETFRAMEWORK
            // on .NET Framework only, also read from app.config/web.config
            new NameValueConfigurationSource(System.Configuration.ConfigurationManager.AppSettings)
#endif
*/
        };

        return new PluginSettings(configurationSource);
    }
}
