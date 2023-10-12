namespace MatrixHelper;

public class Dependency
{
    public Dependency(string name, string link, string version, string stability)
    {
        Name = name;
        Link = link;
        Version = version;
        Stability = stability;
    }

    public string Name { get; set; }
    public string Link { get; set; }
    public string Version { get; set; }
    public string Stability { get; set; }  
}
