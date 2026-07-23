// <copyright file="YamlConfigurationParser.cs" company="Splunk Inc.">
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

using System.Reflection;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration.Parser;

internal static class YamlConfigurationParser
{
    private const string ParserTypeName = "OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser, OpenTelemetry.AutoInstrumentation";

    internal static YamlRoot? ParseFile(string fileName)
    {
        return ParseYaml("ParseYaml", fileName);
    }

    internal static YamlRoot? ParseContent(string yaml)
    {
        return ParseYaml("ParseYamlContent", yaml);
    }

    private static YamlRoot? ParseYaml(string methodName, string argument)
    {
        var parserType = Type.GetType(ParserTypeName);
        if (parserType == null)
        {
            throw new Exception("Could not find Parser type for YAML configuration parsing.");
        }

        var parseYaml = parserType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(m =>
                m.Name == methodName &&
                m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(string));

        if (parseYaml == null)
        {
            throw new MissingMethodException(parserType.FullName, $"{methodName}<T>(string)");
        }

        var yamlRoot = parseYaml
            .MakeGenericMethod(typeof(YamlRoot))
            .Invoke(null, new object[] { argument });

        if (yamlRoot == null)
        {
            return null;
        }

        return (YamlRoot)yamlRoot;
    }
}
