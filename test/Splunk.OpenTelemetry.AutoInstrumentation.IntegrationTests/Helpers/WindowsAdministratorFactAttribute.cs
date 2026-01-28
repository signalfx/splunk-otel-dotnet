// <copyright file="WindowsAdministratorFactAttribute.cs" company="Splunk Inc.">
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

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;

[AttributeUsage(AttributeTargets.Method)]
public sealed class WindowsAdministratorFactAttribute : FactAttribute
{
    public WindowsAdministratorFactAttribute()
    {
        Skip = GetSkipReason();
    }

    internal static string? GetSkipReason() =>
        !EnvironmentTools.IsWindows() || EnvironmentTools.IsWindowsAdministrator() ? null : "This test requires administrative privileges on Windows.";
}
