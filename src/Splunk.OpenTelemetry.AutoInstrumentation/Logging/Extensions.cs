// <copyright file="Extensions.cs" company="Splunk Inc.">
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

using System.Collections;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Logging;

internal static class Extensions
{
    private const string SecretPattern = "(?:^|_)(API|TOKEN|SECRET|KEY|PASSWORD|PASS|PWD|HEADER|CREDENTIALS)(?:_|$)";
    private static readonly ICollection<string> RelevantPrefixes = ["DOTNET_", "COR_", "CORECLR_", "OTEL_", "SPLUNK_"];

    private static readonly Regex SecretRegex;

    static Extensions()
    {
        SecretRegex = new Regex(SecretPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public static void LogConfigurationSetup(this ILogger logger)
    {
        LogConfiguration(
            Environment.GetEnvironmentVariables().ToKeyValuePair(),
            "Environment Variables:",
            logger);

#if NETFRAMEWORK
        LogConfiguration(
            System.Configuration.ConfigurationManager.AppSettings.ToKeyValuePair(),
            "AppSettings:",
            logger);
#endif
    }

    private static void LogConfiguration(IEnumerable<KeyValuePair<string, string?>> configuration, string header, ILogger logger)
    {
        configuration
            .FilterRelevant()
            .OutputInOrder(header, logger);
    }

    private static IEnumerable<KeyValuePair<string, string?>> ToKeyValuePair(this IDictionary dictionary)
    {
        foreach (DictionaryEntry kvp in dictionary)
        {
            var key = kvp.Key?.ToString();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var isSecret = SecretRegex.IsMatch(key);
            var value = isSecret
                ? "<hidden>"
                : kvp.Value?.ToString();

            yield return new KeyValuePair<string, string?>(key!, value);
        }
    }

#if NETFRAMEWORK
    private static IEnumerable<KeyValuePair<string, string?>> ToKeyValuePair(this NameValueCollection collection)
    {
        foreach (var key in collection.AllKeys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var isSecret = SecretRegex.IsMatch(key);
            var value = isSecret
                ? "<hidden>"
                : (collection.GetValues(key) ?? []).FirstOrDefault();

            yield return new KeyValuePair<string, string?>(key, value);
        }
    }
#endif

    private static IEnumerable<KeyValuePair<string, string?>> FilterRelevant(this IEnumerable<KeyValuePair<string, string?>> kvp)
    {
        foreach (var pair in kvp)
        {
            foreach (var prefix in RelevantPrefixes)
            {
                if (pair.Key.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return pair;
                }
            }
        }
    }

    private static void OutputInOrder(this IEnumerable<KeyValuePair<string, string?>> pairs, string header, ILogger logger)
    {
        var sb = new StringBuilder();
        sb.AppendLine(header);

        foreach (var kvp in pairs.OrderBy(x => x.Key))
        {
            sb.AppendLine($"\t{kvp.Key}={kvp.Value}");
        }

        logger.Debug(sb.ToString());
    }
}
