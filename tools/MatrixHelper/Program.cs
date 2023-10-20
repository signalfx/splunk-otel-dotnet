using MatrixHelper;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


var serializer = new SerializerBuilder()
    .WithNamingConvention(UnderscoredNamingConvention.Instance)
    .WithIndentedSequences()
    .Build();


File.WriteAllText("..\\..\\..\\..\\..\\splunk-otel-dotnet-metadata.yaml", serializer.Serialize(MetadataData.GetAllInOne()));
