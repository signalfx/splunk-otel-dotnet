// <copyright file="OpAmpRequestFrameInspector.cs" company="Splunk Inc.">
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

#if NET
using System.Reflection;
using OpenTelemetry.OpAmp.Client;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

internal sealed class OpAmpRequestFrameInspector
{
    private static readonly Type AgentToServerType = typeof(OpAmpClient).Assembly.GetType("OpAmp.Proto.V1.AgentToServer", throwOnError: true)!;
    private static readonly object AgentToServerParser = AgentToServerType
        .GetProperty("Parser", BindingFlags.Static | BindingFlags.Public)!
        .GetValue(null)!;

    private static readonly MethodInfo ParseAgentToServerMethod = AgentToServerParser.GetType()
        .GetMethod("ParseFrom", [typeof(byte[])])!;

    private readonly object _frame;

    private OpAmpRequestFrameInspector(object frame)
    {
        _frame = frame;
    }

    public bool IsFullStateReport => GetPropertyValue(_frame, "AgentDescription") != null &&
        GetPropertyValue(_frame, "Health") != null;

    public bool HasEffectiveConfig => GetPropertyValue(_frame, "EffectiveConfig") != null;

    public static OpAmpRequestFrameInspector Parse(byte[] requestBody)
    {
        var frame = ParseAgentToServerMethod.Invoke(AgentToServerParser, [requestBody])!;
        return new OpAmpRequestFrameInspector(frame);
    }

    public string GetEffectiveConfigBody(string fileName)
    {
        var effectiveConfig = GetRequiredPropertyValue(_frame, "EffectiveConfig");
        var configMap = GetRequiredPropertyValue(effectiveConfig, "ConfigMap");
        var files = GetRequiredPropertyValue(configMap, "ConfigMap");
        var arguments = new object?[] { fileName, null };
        var found = (bool)files.GetType().GetMethod("TryGetValue")!.Invoke(files, arguments)!;
        Assert.True(found, $"The OpAMP request did not contain effective config file '{fileName}'.");

        var body = GetRequiredPropertyValue(arguments[1]!, "Body");
        return (string)body.GetType().GetMethod("ToStringUtf8")!.Invoke(body, null)!;
    }

    private static object? GetPropertyValue(object source, string name)
    {
        return source.GetType().GetProperty(name)?.GetValue(source);
    }

    private static object GetRequiredPropertyValue(object source, string name)
    {
        return GetPropertyValue(source, name)
            ?? throw new InvalidOperationException($"Property {source.GetType().FullName}.{name} returned null.");
    }
}
#endif
