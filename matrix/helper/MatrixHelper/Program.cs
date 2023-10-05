using MatrixHelper;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


var serializer = new SerializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .WithIndentedSequences()
    .Build();


File.WriteAllText("..\\..\\..\\..\\..\\settings.yaml", serializer.Serialize(SettingsData.GetSettings()));

File.WriteAllText("..\\..\\..\\..\\..\\instrumentations.yaml", serializer.Serialize(InstrumentationData.GetInstrumentations()));

File.WriteAllText("..\\..\\..\\..\\..\\resource-detectors.yaml", serializer.Serialize(ResourceDetectorsData.GetResourceDetectors()));

File.WriteAllText("..\\..\\..\\..\\..\\metadata.yaml", serializer.Serialize(MetaDataData.GetMetaData()));

File.WriteAllText("..\\..\\..\\..\\..\\all-in-one.yaml", serializer.Serialize(MetaDataData.GetAllInOne()));
