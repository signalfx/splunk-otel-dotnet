// <copyright file="EffectiveResourceConfigReader.cs" company="Splunk Inc.">
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

using OpenTelemetry.Resources;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal static class EffectiveResourceConfigReader
{
    private const string ServiceNameAttributeName = "service.name";

    internal static string? ReadServiceName(Resource resource)
    {
        foreach (var attribute in resource.Attributes)
        {
            if (string.Equals(attribute.Key, ServiceNameAttributeName, StringComparison.Ordinal) &&
                attribute.Value is string serviceName &&
                !string.IsNullOrEmpty(serviceName))
            {
                return serviceName;
            }
        }

        return null;
    }
}
