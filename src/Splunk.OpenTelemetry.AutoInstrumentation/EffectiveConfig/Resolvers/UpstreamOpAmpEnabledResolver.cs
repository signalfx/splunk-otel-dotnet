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

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Resolvers;

internal static class UpstreamOpAmpEnabledResolver
{
    private static readonly Lazy<bool> IsEnabledCache = new(Resolve);

    public static bool IsEnabled()
    {
        return IsEnabledCache.Value;
    }

    private static bool Resolve()
    {
        var instrumentationType = UpstreamInstrumentationResolver.GetInstrumentationType();
        var opAmpSettingsProperty = instrumentationType
            .GetProperty("OpAmpSettings", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(instrumentationType.FullName, "OpAmpSettings");
        var opAmpSettingsLazy = opAmpSettingsProperty.GetValue(null)
            ?? throw new InvalidOperationException(
                $"Property {instrumentationType.FullName}.OpAmpSettings returned null.");

        var valueProperty = opAmpSettingsLazy
            .GetType()
            .GetProperty("Value", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new MissingMemberException(opAmpSettingsLazy.GetType().FullName, "Value");
        var opAmpSettings = valueProperty.GetValue(opAmpSettingsLazy)
            ?? throw new InvalidOperationException(
                $"Property {opAmpSettingsLazy.GetType().FullName}.Value returned null.");

        var enabledProperty = opAmpSettings
            .GetType()
            .GetProperty("OpAmpClientEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(opAmpSettings.GetType().FullName, "OpAmpClientEnabled");

        if (enabledProperty.GetValue(opAmpSettings) is not bool enabled)
        {
            throw new InvalidOperationException(
                $"The pinned upstream {opAmpSettings.GetType().FullName}.OpAmpClientEnabled has an unexpected value.");
        }

        return enabled;
    }
}
