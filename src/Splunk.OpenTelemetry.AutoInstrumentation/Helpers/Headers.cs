// <copyright file="Headers.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.Helpers;

internal static class Headers
{
    public const string AccessTokenHeader = "X-Sf-Token";

    public static string AppendAccessToken(this string headers, string accessToken)
    {
        return Append(headers, AccessTokenHeader, accessToken);
    }

    private static string Append(string headers, string appendHeader, string appendValue)
    {
        string newHeader = $"{appendHeader}={appendValue}";

        if (string.IsNullOrEmpty(headers))
        {
            return newHeader;
        }

        return $"{headers}, {newHeader}";
    }
}
