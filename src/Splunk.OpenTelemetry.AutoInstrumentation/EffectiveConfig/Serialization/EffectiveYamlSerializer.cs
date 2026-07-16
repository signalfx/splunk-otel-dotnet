// <copyright file="EffectiveYamlSerializer.cs" company="Splunk Inc.">
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

using System.Globalization;
using System.Reflection;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Serialization;

internal static class EffectiveYamlSerializer
{
    private const string UpstreamAssemblyName = "OpenTelemetry.AutoInstrumentation";

    private static readonly Lazy<object> Serializer = new(CreateSerializer);

    private static readonly Lazy<MethodInfo> SerializeMethod = new(() =>
        GetInstanceMethod(Serializer.Value.GetType(), "Serialize", [typeof(object)]));

    public static void ValidateCompatibility()
    {
        _ = SerializeMethod.Value;
    }

    public static string Serialize(EffectiveYamlConfig value)
    {
        var result = SerializeMethod.Value.Invoke(Serializer.Value, [value]);
        if (result is not string serialized)
        {
            throw new InvalidOperationException("The upstream YAML serializer returned a non-string result.");
        }

        return serialized.TrimEnd('\r', '\n');
    }

    private static object CreateSerializer()
    {
        var serializerBuilderType = GetUpstreamType("Vendors.YamlDotNet.Serialization.SerializerBuilder");
        var builder = CreateInstance(serializerBuilderType);

        ConfigureDefaultValueHandling(builder);
        ConfigureDefaultScalarStyle(builder);
        ConfigureYamlPropertyOverrides(builder);
        InvokeRequired(builder, "WithIndentedSequences", []);

        return InvokeRequired(builder, "Build", []);
    }

    private static void ConfigureDefaultValueHandling(object builder)
    {
        var defaultValuesHandlingType = GetUpstreamType("Vendors.YamlDotNet.Serialization.DefaultValuesHandling");
        // Absence is meaningful in effective-config YAML; null/empty inactive branches should not be reported as configured.
        var defaultValuesHandling = CreateEnumFlags(defaultValuesHandlingType, "OmitNull", "OmitEmptyCollections");

        InvokeRequired(builder, "ConfigureDefaultValuesHandling", [defaultValuesHandlingType], defaultValuesHandling);
    }

    private static void ConfigureDefaultScalarStyle(object builder)
    {
        var scalarStyleType = GetUpstreamType("Vendors.YamlDotNet.Core.ScalarStyle");
        var doubleQuotedStyle = Enum.Parse(scalarStyleType, "DoubleQuoted");

        InvokeRequired(builder, "WithDefaultScalarStyle", [scalarStyleType], doubleQuotedStyle);
    }

    private static void ConfigureYamlPropertyOverrides(object builder)
    {
        var yamlMemberAttributeType = GetUpstreamType("Vendors.YamlDotNet.Serialization.YamlMemberAttribute");
        var defaultValuesHandlingType = GetUpstreamType("Vendors.YamlDotNet.Serialization.DefaultValuesHandling");
        var preserveDefaultValues = Enum.Parse(defaultValuesHandlingType, "Preserve");

        foreach (var property in GetYamlProperties())
        {
            var effectiveYamlProperty = property.GetCustomAttribute<EffectiveYamlPropertyAttribute>()!;
            var yamlMemberAttribute = (Attribute)CreateInstance(yamlMemberAttributeType);

            SetProperty(yamlMemberAttribute, "Alias", effectiveYamlProperty.Name);
            SetProperty(yamlMemberAttribute, "Order", effectiveYamlProperty.Order);
            SetProperty(yamlMemberAttribute, "ApplyNamingConventions", false);
            if (effectiveYamlProperty.PreserveNull)
            {
                SetProperty(yamlMemberAttribute, "DefaultValuesHandling", preserveDefaultValues);
            }

            InvokeRequired(
                builder,
                "WithAttributeOverride",
                [typeof(Type), typeof(string), typeof(Attribute)],
                property.DeclaringType!,
                property.Name,
                yamlMemberAttribute);
        }
    }

    private static IEnumerable<PropertyInfo> GetYamlProperties()
    {
        var properties = new[] { typeof(EffectiveYamlConfig) }
            .Concat(typeof(EffectiveYamlConfig).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .ToArray();

        var unconfiguredProperties = properties
            .Where(property => property.GetCustomAttribute<EffectiveYamlPropertyAttribute>() == null)
            .Select(property => $"{property.DeclaringType!.FullName}.{property.Name}")
            .ToArray();
        if (unconfiguredProperties.Length != 0)
        {
            throw new InvalidOperationException(
                $"Effective YAML properties must declare {nameof(EffectiveYamlPropertyAttribute)}: " +
                string.Join(", ", unconfiguredProperties));
        }

        return properties;
    }

    private static object CreateEnumFlags(Type enumType, params string[] names)
    {
        var value = names
            .Select(name => Enum.Parse(enumType, name))
            .Select(enumValue => Convert.ToInt32(enumValue, CultureInfo.InvariantCulture))
            .Aggregate(0, (current, enumValue) => current | enumValue);

        return Enum.ToObject(enumType, value);
    }

    private static object InvokeRequired(object target, string methodName, Type[] parameterTypes, params object?[] arguments)
    {
        var method = GetInstanceMethod(target.GetType(), methodName, parameterTypes);
        return method.Invoke(target, arguments)
            ?? throw new InvalidOperationException($"Method {target.GetType().FullName}.{methodName} returned null.");
    }

    private static MethodInfo GetInstanceMethod(Type type, string methodName, Type[] parameterTypes)
    {
        return type.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: parameterTypes,
            modifiers: null) ?? throw new MissingMethodException(type.FullName, methodName);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new MissingMemberException(target.GetType().FullName, propertyName);

        property.SetValue(target, value);
    }

    private static object CreateInstance(Type type)
    {
        return Activator.CreateInstance(type, nonPublic: true)
            ?? throw new MissingMethodException(type.FullName, ".ctor()");
    }

    private static Type GetUpstreamType(string fullName)
    {
        return Type.GetType($"{fullName}, {UpstreamAssemblyName}", throwOnError: false)
            ?? throw new TypeLoadException($"Could not load upstream type '{fullName}' from '{UpstreamAssemblyName}'.");
    }
}
