// <copyright file="UpstreamOpAmpEnabledResolver.cs" company="Splunk Inc.">
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

internal static class UpstreamOpAmpEnabledResolver
{
    private static readonly ILogger Log = new Logger();
    private static readonly Lazy<bool> IsEnabledCache = new(ResolveDefault);

    public static bool IsEnabled()
    {
        return IsEnabledCache.Value;
    }

    private static bool ResolveDefault()
    {
        try
        {
            var instrumentationType = UpstreamInstrumentationResolver.TryGetInstrumentationType();
            if (instrumentationType == null)
            {
                return WarnAndReturnFalse("Instrumentation type was not found.");
            }

            var opAmpSettingsLazy = instrumentationType
                .GetProperty("OpAmpSettings", BindingFlags.Static | BindingFlags.NonPublic)
                ?.GetValue(null);
            if (opAmpSettingsLazy == null)
            {
                return WarnAndReturnFalse("OpAmpSettings property was not found.");
            }

            var opAmpSettings = opAmpSettingsLazy
                .GetType()
                .GetProperty("Value", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(opAmpSettingsLazy);
            if (opAmpSettings == null)
            {
                return WarnAndReturnFalse("OpAmpSettings value was not found.");
            }

            var enabledValue = opAmpSettings
                .GetType()
                .GetProperty("OpAmpClientEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(opAmpSettings);

            if (enabledValue is bool enabled)
            {
                return enabled;
            }

            return WarnAndReturnFalse("OpAmpClientEnabled property was not found.");
        }
        catch (Exception ex)
        {
            return WarnAndReturnFalse(ex.Message);
        }
    }

    private static bool WarnAndReturnFalse(string reason)
    {
        Log.Warning($"Could not resolve upstream OpAMP enabled setting: {reason}");
        return false;
    }
}
