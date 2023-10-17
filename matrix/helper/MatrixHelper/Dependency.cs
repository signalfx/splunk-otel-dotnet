namespace MatrixHelper;

public class Dependency
{
    public Dependency(string name, string sourceHref, string version, string stability)
    {
        Name = name;
        SourceHref = sourceHref;
        Version = version;
        Stability = stability;
    }

    public string Name { get; set; }
    public string SourceHref { get; set; }
    public string Version { get; set; }
    public string Stability { get; set; }  
}
