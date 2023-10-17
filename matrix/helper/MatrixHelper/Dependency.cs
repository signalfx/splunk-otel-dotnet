using YamlDotNet.Serialization;

namespace MatrixHelper;

public class Dependency
{
    public Dependency(string name, string sourceHref, string? packageHref, string version, string stability)
    {
        Name = name;
        SourceHref = sourceHref;
        PackageHref = packageHref;
        Version = version;
        Stability = stability;
    }

    public string Name { get; set; }
    public string SourceHref { get; set; }
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? PackageHref { get; set; }
    public string Version { get; set; }
    public string Stability { get; set; }  
}
