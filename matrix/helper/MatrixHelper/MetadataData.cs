namespace MatrixHelper;

public static class MetadataData
{
    public static Metadata GetMetaData()
    {
        return new Metadata
        {
            Component = "Splunk Distribution of OpenTelemetry .NET",
            Version = "1.0.2",
            Dependencies = new Dependency[]
            {
                new("https://github.com/open-telemetry/opentelemetry-dotnet", "1.6.0", "stable"),
                new("https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation", "1.0.2", "stable"),
            },
            SettingsFiles = new[] { "settings.yaml" },
            InstrumentationFiles = new[] { "instrumentations.yaml" },
            ResourceDetectorFiles = new[] { "resource-detectors.yaml" }
        };
    }

    public static AllInOne GetAllInOne()
    {
        return new AllInOne
        {
            Component = "Splunk Distribution of OpenTelemetry .NET",
            Version = "1.0.2",
            Dependencies = new Dependency[]
            {
                new("https://github.com/open-telemetry/opentelemetry-dotnet", "1.6.0", "stable"),
                new("https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation", "1.0.2", "stable"),
            },
            Settings = SettingsData.GetSettings(),
            Instrumentations = InstrumentationData.GetInstrumentations(),
            ResourceDetectors = ResourceDetectorsData.GetResourceDetectors()
        };
    }
}
public class AllInOne
{
    public string Component { get; set; }
    public string Version { get; set; }

    public Dependency[] Dependencies { get; set; }

    public Setting[] Settings { get; set; }
    public Instrumentation[] Instrumentations { get; set; }
    public ResourceDetector[] ResourceDetectors { get; set; }
}

public class Metadata
{
    public string Component { get; set; }
    public string Version { get; set; }

    public Dependency[] Dependencies { get; set; }

    public string[] SettingsFiles { get; set; }

    public string[] InstrumentationFiles { get; set; }
    public string[] ResourceDetectorFiles { get; set; }
}
