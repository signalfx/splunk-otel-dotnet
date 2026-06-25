// <copyright file="RemoteConfiguration.cs" company="Splunk Inc.">
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

using Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration;

namespace Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

internal static class RemoteConfiguration
{
    public const string RemoteConfigFileName = "splunk.remote.config";
    public const string RemoteConfigContentType = "application/yaml";

    public static void ApplyYaml(string yaml)
    {
        ProfilerRuntimeConfiguration.Apply(ParseYaml(yaml));
    }

    internal static YamlRoot? ParseYaml(string yaml)
    {
        var fileName = Path.Combine(Path.GetTempPath(), $"splunk-remote-config-{Guid.NewGuid():N}.yaml");
        try
        {
            File.WriteAllText(fileName, yaml);
            return PluginSettings.LoadSplunkConfig(fileName);
        }
        finally
        {
            TryDelete(fileName);
        }
    }

    private static void TryDelete(string fileName)
    {
        try
        {
            File.Delete(fileName);
        }
        catch
        {
            // Best effort cleanup for a transient parser input file.
        }
    }
}
