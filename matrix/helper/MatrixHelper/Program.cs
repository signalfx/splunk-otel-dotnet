using MatrixHelper;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


var serializer = new SerializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();


File.WriteAllText("..\\..\\..\\..\\..\\settings.yaml", serializer.Serialize(SettingsData.GetSettings()));
