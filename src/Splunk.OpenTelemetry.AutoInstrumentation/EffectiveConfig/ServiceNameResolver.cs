// <copyright file="ServiceNameResolver.cs" company="Splunk Inc.">
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
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal static class ServiceNameResolver
{
    internal const string InstrumentationTypeName = "OpenTelemetry.AutoInstrumentation.Instrumentation, OpenTelemetry.AutoInstrumentation";

    private static readonly ILogger Log = new Logger();

    // ResourceSettings tells us which configuration path upstream selected.
    // In file-based mode upstream disables the env-var detector, so direct env reads could be stale.
    internal static string? Resolve(Type? instrumentationType)
    {
        if (instrumentationType != null)
        {
            try
            {
                var resourceSettings = ReadResourceSettings(instrumentationType);
                if (resourceSettings == null)
                {
                    Log.Warning("Failed to read upstream ResourceSettings. OTEL_SERVICE_NAME will be omitted from effective configuration.");
                    return null;
                }

                return Resolve(resourceSettings, instrumentationType);
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to read upstream ResourceSettings. OTEL_SERVICE_NAME will be omitted from effective configuration: {ex.Message}");
                return null;
            }
        }

        return ResolveFromEnvironment();
    }

    internal static string? ReadFromResources(IEnumerable<KeyValuePair<string, object>> resources)
    {
        foreach (var kvp in resources)
        {
            if (string.Equals(kvp.Key, "service.name", StringComparison.Ordinal))
            {
                return kvp.Value?.ToString();
            }
        }

        return null;
    }

    private static string? Resolve(object resourceSettings, Type? instrumentationType)
    {
        var environmentalVariablesDetectorEnabled = ReadEnvironmentalVariablesDetectorEnabled(resourceSettings);
        if (environmentalVariablesDetectorEnabled == null)
        {
            Log.Warning("Failed to read upstream ResourceSettings.EnvironmentalVariablesDetectorEnabled. OTEL_SERVICE_NAME will be omitted to avoid reporting a stale environment value.");
            return null;
        }

        if (!environmentalVariablesDetectorEnabled.Value)
        {
            return ReadFromResourceSettings(resourceSettings) ?? GetFallbackServiceName(instrumentationType);
        }

        return ResolveFromEnvironment() ?? GetFallbackServiceName(instrumentationType);
    }

    private static string? ResolveFromEnvironment()
    {
        var serviceName = Environment.GetEnvironmentVariable(EffectiveConfigKeys.ServiceName);
        if (!string.IsNullOrEmpty(serviceName))
        {
            return serviceName;
        }

        var resourceAttributes = Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES");
        if (!string.IsNullOrEmpty(resourceAttributes))
        {
            var fromEnv = ResourceAttributesHelper.ParseServiceName(resourceAttributes);
            if (fromEnv != null)
            {
                return fromEnv;
            }
        }

        return null;
    }

    private static object? GetLazySettingsValue(Type instrumentationType, string settingsPropertyName)
    {
        var lazyProp = instrumentationType.GetProperty(settingsPropertyName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        var lazyObj = lazyProp?.GetValue(null);
        var valueProp = lazyObj?.GetType().GetProperty("Value");
        return valueProp?.GetValue(lazyObj);
    }

    private static object? ReadResourceSettings(Type instrumentationType)
    {
        return GetLazySettingsValue(instrumentationType, "ResourceSettings");
    }

    private static bool? ReadEnvironmentalVariablesDetectorEnabled(object resourceSettings)
    {
        var property = resourceSettings.GetType().GetProperty("EnvironmentalVariablesDetectorEnabled", BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(resourceSettings) is bool value
            ? value
            : null;
    }

    private static string? ReadFromResourceSettings(object resourceSettings)
    {
        var resourcesProp = resourceSettings.GetType().GetProperty("Resources", BindingFlags.Public | BindingFlags.Instance);
        if (resourcesProp?.GetValue(resourceSettings) is not IEnumerable<KeyValuePair<string, object>> resources)
        {
            return null;
        }

        return ReadFromResources(resources);
    }

    private static string? GetFallbackServiceName(Type? instrumentationType)
    {
        if (instrumentationType == null)
        {
            return null;
        }

        try
        {
            var configuratorType = instrumentationType.Assembly.GetType("OpenTelemetry.AutoInstrumentation.Configurations.ServiceNameConfigurator");
            var method = configuratorType?.GetMethod("GetFallbackServiceName", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            return method?.Invoke(null, null) as string;
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to read ServiceNameConfigurator.GetFallbackServiceName (OTEL_SERVICE_NAME): {ex.Message}");
            return null;
        }
    }
}
