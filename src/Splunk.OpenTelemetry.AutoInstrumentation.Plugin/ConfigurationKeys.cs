// <copyright file="ConfigurationKeys.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.Plugin;

internal static class ConfigurationKeys
{
    /// <summary>
    /// Configuration key for realm.
    /// </summary>
    public const string Realm = "SPLUNK_REALM";

    /// <summary>
    /// Configuration key for access token.
    /// </summary>
    public const string AccessToken = "SPLUNK_ACCESS_TOKEN";

    public static class OpenTelemetry
    {
        public const string OtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";
    }
}
