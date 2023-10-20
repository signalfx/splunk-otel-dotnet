namespace MatrixHelper;

public static class MetadataData
{
    public static AllInOne GetAllInOne()
    {
        return new AllInOne
        {
            Component = "Splunk Distribution of OpenTelemetry .NET",
            Version = "1.0.2",
            Dependencies = new Dependency[]
            {
                new("OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet", null, "1.6.0", "stable"),
                new("OpenTelemetry .NET Automatic Instrumentation", "https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation", null, "1.0.2", "stable"),
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
