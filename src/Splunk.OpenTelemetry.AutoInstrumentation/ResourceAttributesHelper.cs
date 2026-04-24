// <copyright file="ResourceAttributesHelper.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal static class ResourceAttributesHelper
{
    /// <summary>
    /// Parses service.name from the value of OTEL_RESOURCE_ATTRIBUTES (comma-separated key=value pairs).
    /// Returns null if not found or if the value is empty.
    /// </summary>
    // TODO: optimize
    internal static string? ParseServiceName(string resourceAttributes)
    {
        foreach (var pair in resourceAttributes.Split(','))
        {
            var idx = pair.IndexOf('=');
            if (idx <= 0)
            {
                continue;
            }

            var key = pair.Substring(0, idx).Trim();
            if (string.Equals(key, "service.name", StringComparison.OrdinalIgnoreCase))
            {
                var value = pair.Substring(idx + 1).Trim();
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
        }

        return null;
    }
}
