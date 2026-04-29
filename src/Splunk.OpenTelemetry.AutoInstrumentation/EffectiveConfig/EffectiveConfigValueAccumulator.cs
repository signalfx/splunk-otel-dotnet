// <copyright file="EffectiveConfigValueAccumulator.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal sealed class EffectiveConfigValueAccumulator
{
    private readonly object _lock = new();
    private readonly Dictionary<string, List<string>> _values = new();

    public void Add(string key, string value)
    {
        lock (_lock)
        {
            if (!_values.TryGetValue(key, out var values))
            {
                values = [];
                _values[key] = values;
            }

            values.Add(value);
        }
    }

    public string? GetValue(string key)
    {
        lock (_lock)
        {
            if (!_values.TryGetValue(key, out var values))
            {
                return null;
            }

            return string.Join(",", values.Select(EncodeListItem));
        }
    }

    // TODO: Temporary encoding
    private static string EncodeListItem(string value)
    {
        // Consumers split on comma first, then percent-decode each endpoint once.
        return value
            .Replace("%", "%25")
            .Replace(",", "%2C");
    }
}
