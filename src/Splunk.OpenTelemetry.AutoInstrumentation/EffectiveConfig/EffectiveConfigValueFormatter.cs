// <copyright file="EffectiveConfigValueFormatter.cs" company="Splunk Inc.">
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

using System.Globalization;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal static class EffectiveConfigValueFormatter
{
    public static string FormatList(IReadOnlyList<string> values)
    {
        // Endpoint values are valid URLs; literal quotes must already be percent-encoded.
        return string.Join(",", values.Select(Quote));
    }

    public static string FormatBoolean(bool value)
    {
        return value ? "true" : "false";
    }

    public static string? TryFormatString(string value)
    {
        if (value.IndexOf('"') >= 0 ||
            value.IndexOf('\r') >= 0 ||
            value.IndexOf('\n') >= 0)
        {
            return null;
        }

        return Quote(value);
    }

    public static string FormatMilliseconds(uint value)
    {
        return Quote(value.ToString(CultureInfo.InvariantCulture) + "ms");
    }

    private static string Quote(string value)
    {
        return "\"" + value + "\"";
    }
}
