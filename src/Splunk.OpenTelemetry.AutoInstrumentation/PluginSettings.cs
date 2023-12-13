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

using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

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

        Realm = source.GetString(ConfigurationKeys.Splunk.Realm) ?? Constants.None;
        AccessToken = source.GetString(ConfigurationKeys.Splunk.AccessToken);
        ServiceName = source.GetString(ConfigurationKeys.OpenTelemetry.ServiceName);
        TraceResponseHeaderEnabled = source.GetBool(ConfigurationKeys.Splunk.TraceResponseHeaderEnabled) ?? true;
        var otlpEndpoint = source.GetString(ConfigurationKeys.OpenTelemetry.OtlpEndpoint);
        IsOtlpEndpointSet = !string.IsNullOrEmpty(otlpEndpoint);
        ResourceAttributes = source.GetString(ConfigurationKeys.OpenTelemetry.ResourceAttributes).ToNameValueCollection();

#if NET6_0_OR_GREATER
        CpuProfilerEnabled = source.GetBool(ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled) ?? false;
        MemoryProfilerEnabled = source.GetBool(ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled) ?? false;
        var callStackInterval = source.GetInt32(ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval) ?? 10000;
        CpuProfilerCallStackInterval = callStackInterval < 0 ? 10000u : (uint)callStackInterval;

        ProfilerLogsEndpoint = GetProfilerLogsEndpoints(source, otlpEndpoint);
#endif
    }

    public string Realm { get; }

    public string? AccessToken { get; }

    public string? ServiceName { get; }

    public bool TraceResponseHeaderEnabled { get; }

    public bool IsOtlpEndpointSet { get; }

    public IReadOnlyCollection<KeyValuePair<string, string>> ResourceAttributes { get; }

#if NET6_0_OR_GREATER
    public bool CpuProfilerEnabled { get; }

    public uint CpuProfilerCallStackInterval { get; }

    public bool MemoryProfilerEnabled { get; }

    public string ProfilerLogsEndpoint { get; }
#endif

    public static PluginSettings FromDefaultSources()
    {
        var configurationSource = new CompositeConfigurationSource
        {
            new EnvironmentConfigurationSource(),

#if NETFRAMEWORK
            // on .NET Framework only, also read from app.config/web.config
            new NameValueConfigurationSource(System.Configuration.ConfigurationManager.AppSettings)
#endif

        };

        return new PluginSettings(configurationSource);
    }

#if NET6_0_OR_GREATER
    private static string GetProfilerLogsEndpoints(IConfigurationSource source, string? otlpFallback)
    {
        var profilerLogsEndpoint = source.GetString(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerLogsEndpoint);

        if (string.IsNullOrEmpty(profilerLogsEndpoint))
        {
            if (string.IsNullOrEmpty(otlpFallback))
            {
                return "http://localhost:4318/v1/logs";
            }

            return otlpFallback.EndsWith("v1/logs") ? otlpFallback : new Uri(new Uri(otlpFallback), "v1/logs").ToString();
        }

        return profilerLogsEndpoint;
    }
#endif
}
