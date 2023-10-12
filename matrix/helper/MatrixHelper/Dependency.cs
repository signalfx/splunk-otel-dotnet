namespace MatrixHelper;

public class Dependency
{
    public Dependency(string link, string version, string stability)
    {
        Link = link;
        Version = version;
        Stability = stability;
    }

    public string Link { get; set; }
    public string Version { get; set; }
    public string Stability { get; set; }  
}
