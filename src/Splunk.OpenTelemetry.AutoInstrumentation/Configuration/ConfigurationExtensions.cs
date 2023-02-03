// <copyright file="ConfigurationExtensions.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.Configuration
{
    internal static class ConfigurationExtensions
    {
        public static IReadOnlyCollection<KeyValuePair<string, string>> ToNameValueCollection(this string configurationValue)
        {
            if (configurationValue == null)
            {
                return Array.Empty<KeyValuePair<string, string>>();
            }

            var collection = new List<KeyValuePair<string, string>>();

            foreach (var attribute in configurationValue.Split(','))
            {
                var parts = attribute.Split('=');
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                collection.Add(new(key, value));
            }

            return collection;
        }
    }
}
