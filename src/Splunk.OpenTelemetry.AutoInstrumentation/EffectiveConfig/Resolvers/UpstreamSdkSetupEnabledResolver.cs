// <copyright file="UpstreamSdkSetupEnabledResolver.cs" company="Splunk Inc.">
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

internal static class UpstreamSdkSetupEnabledResolver
{
    private static readonly Lazy<bool> IsEnabledCache = new(Resolve);

    public static bool IsEnabled()
    {
        return IsEnabledCache.Value;
    }

    private static bool Resolve()
    {
        var instrumentationType = UpstreamInstrumentationResolver.GetInstrumentationType();

        var generalSettingsProperty = instrumentationType
            .GetProperty("GeneralSettings", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(instrumentationType.FullName, "GeneralSettings");
        var generalSettingsLazy = generalSettingsProperty.GetValue(null)
            ?? throw new InvalidOperationException(
                $"Property {instrumentationType.FullName}.GeneralSettings returned null.");

        var valueProperty = generalSettingsLazy
            .GetType()
            .GetProperty("Value", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new MissingMemberException(generalSettingsLazy.GetType().FullName, "Value");
        var generalSettings = valueProperty.GetValue(generalSettingsLazy)
            ?? throw new InvalidOperationException(
                $"Property {generalSettingsLazy.GetType().FullName}.Value returned null.");

        var setupSdkProperty = generalSettings
            .GetType()
            .GetProperty("SetupSdk", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(generalSettings.GetType().FullName, "SetupSdk");

        if (setupSdkProperty.GetValue(generalSettings) is not bool setupSdk)
        {
            throw new InvalidOperationException(
                $"The pinned upstream {generalSettings.GetType().FullName}.SetupSdk has an unexpected value.");
        }

        return setupSdk;
    }
}
