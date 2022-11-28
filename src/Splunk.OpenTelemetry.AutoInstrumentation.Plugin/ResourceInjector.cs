// <copyright file="ResourceNameInjector.cs" company="Splunk Inc.">
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Plugin
{
    /// <summary>
    /// Class providing injection of splunk resources.
    /// </summary>
    public static class ResourceInjector
    {
        private const string ResourceEnvVarKey = "OTEL_RESOURCE_ATTRIBUTES";
        private const string SplunkDistroVersionKey = "splunk.distro.version";
        private const char AttributeListSplitter = ',';
        private const char AttributeKeyValueSplitter = '=';

        /// <summary>
        /// Adds splunk plunk distribution version to the resources.
        /// </summary>
        public static void InjectSplunkDistributionVersion()
        {
            const string splunkDistroVersion = "v0.0.1-alpha.2";

            var resourcesEnvVarValue = Environment.GetEnvironmentVariable(ResourceEnvVarKey);
            if (resourcesEnvVarValue == null)
            {
                Environment.SetEnvironmentVariable(ResourceEnvVarKey, $"{SplunkDistroVersionKey}{AttributeKeyValueSplitter}{splunkDistroVersion}");
                return;
            }

            Dictionary<string, string> values;

            try
            {
                values = resourcesEnvVarValue.Split(AttributeListSplitter)
                    .ToDictionary(
                        s => s.Split(AttributeKeyValueSplitter)[0],
                        s => s.Split(AttributeKeyValueSplitter)[1]);
            }
            catch
            {
                // TODO: LOG ERROR
                return;
            }

            if (values.ContainsKey(SplunkDistroVersionKey))
            {
                return;
            }

            values[SplunkDistroVersionKey] = splunkDistroVersion;

            resourcesEnvVarValue = string.Join(
                AttributeListSplitter.ToString(),
                values.Select(keyValuePair => $"{keyValuePair.Key}{AttributeKeyValueSplitter}{keyValuePair.Value}"));

            Environment.SetEnvironmentVariable(ResourceEnvVarKey, resourcesEnvVarValue);
        }
    }
}
