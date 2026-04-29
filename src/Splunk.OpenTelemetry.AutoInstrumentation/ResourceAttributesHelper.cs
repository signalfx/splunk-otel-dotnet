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
    private const string ServiceNameAttributeName = "service.name";
    private const char AttributeListSplitter = ',';
    private const char AttributeKeyValueSplitter = '=';

    /// <summary>
    /// Parses service.name from the value of OTEL_RESOURCE_ATTRIBUTES (comma-separated key=value pairs).
    /// Returns null if not found or if the value is empty.
    /// </summary>
    internal static string? ParseServiceName(string resourceAttributes)
    {
        IDictionary<string, string> serviceNameAttribute = new Dictionary<string, string>();

        foreach (var rawKeyValuePair in resourceAttributes.Split(AttributeListSplitter))
        {
            var keyValuePair = rawKeyValuePair.Split(AttributeKeyValueSplitter);
            if (keyValuePair.Length != 2)
            {
                continue;
            }

            serviceNameAttribute[keyValuePair[0].Trim()] = keyValuePair[1].Trim();
        }

        if (!serviceNameAttribute.TryGetValue(ServiceNameAttributeName, out var serviceNameValue))
        {
            return null;
        }

        return string.IsNullOrEmpty(serviceNameValue) ? null : serviceNameValue;
    }
}
