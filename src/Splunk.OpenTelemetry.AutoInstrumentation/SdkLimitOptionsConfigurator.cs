// <copyright file="SdkLimitOptionsConfigurator.cs" company="Splunk Inc.">
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
using System.Globalization;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal class SdkLimitOptionsConfigurator
{
    public static void Configure()
    {
        // Hack, there is no possibility to configure SdkLimitOptions
        // through standard callback.
        // Setting up expected env. variables to be parsed by the SdkLimitOption constructor

        int? unlimited = int.MaxValue;
        int? takeFromFallback = null;

        ChangeEnvironmentalVariableDefault("OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT", 12000);
        ChangeEnvironmentalVariableDefault("OTEL_ATTRIBUTE_COUNT_LIMIT", unlimited);

        ChangeEnvironmentalVariableDefault("OTEL_SPAN_ATTRIBUTE_VALUE_LENGTH_LIMIT", takeFromFallback);
        ChangeEnvironmentalVariableDefault("OTEL_SPAN_ATTRIBUTE_COUNT_LIMIT", takeFromFallback);
        ChangeEnvironmentalVariableDefault("OTEL_SPAN_EVENT_COUNT_LIMIT", unlimited);
        ChangeEnvironmentalVariableDefault("OTEL_SPAN_LINK_COUNT_LIMIT", 1000);
        ChangeEnvironmentalVariableDefault("OTEL_EVENT_ATTRIBUTE_COUNT_LIMIT", takeFromFallback);
        ChangeEnvironmentalVariableDefault("OTEL_LINK_ATTRIBUTE_COUNT_LIMIT", takeFromFallback);
    }

    private static void ChangeEnvironmentalVariableDefault(string environmentalVariableName, int? value)
    {
        var environmentVariable = Environment.GetEnvironmentVariable(environmentalVariableName);

        if (string.IsNullOrEmpty(environmentVariable))
        {
            Environment.SetEnvironmentVariable(environmentalVariableName, value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);
        }
    }
}
