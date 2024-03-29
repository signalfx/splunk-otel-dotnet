﻿// <copyright file="StringConfigurationSource.cs" company="Splunk Inc.">
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

// <copyright file="StringConfigurationSource.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.Configuration;

/// <summary>
/// A base <see cref="IConfigurationSource"/> implementation
/// for string-only configuration sources.
/// </summary>
internal abstract class StringConfigurationSource : IConfigurationSource
{
    /// <inheritdoc />
    public virtual string? GetString(string key)
    {
        string? value = GetStringInternal(key);
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return value;
    }

    /// <inheritdoc />
    public virtual int? GetInt32(string key)
    {
        string? value = GetString(key);

        return int.TryParse(value, out int result)
            ? result
            : null;
    }

    /// <inheritdoc />
    public double? GetDouble(string key)
    {
        string? value = GetString(key);

        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result)
            ? result
            : null;
    }

    /// <inheritdoc />
    public virtual bool? GetBool(string key)
    {
        string? value = GetString(key);
        return bool.TryParse(value, out bool result) ? result : null;
    }

    protected abstract string? GetStringInternal(string key);
}
