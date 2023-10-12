namespace MatrixHelper;

public static class ResourceDetectorsData
{
    public static ResourceDetector[] GetResourceDetectors()
    {
        return new ResourceDetector[]
        {
            new("AZUREAPPSERVICE", "Azure App Service detector.", new Attribute[]{new("azure.app.service.stamp"), new("cloud.platform"), new("cloud.provider"), new("cloud.resource_id"), new("cloud.region"), new("deployment.environment"), new("host.id"), new("service.instance.id"), new("service.name")}, "beta", "community", new Dependency("https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.ResourceDetectors.Azure", "1.0.0-beta.3", "beta")),
            new("CONTAINER", "Container detector. For example, Docker or Podman containers.", new Attribute[]{ new("container.id")}, "beta", "community", new Dependency("https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.ResourceDetectors.Container", "1.0.0-beta.4", "beta")),
        };
    }
}
public class ResourceDetector(string key, string description, Attribute[] attributes, string stability, string supportabilityLevel, Dependency dependency)
{
    public string Key { get; set; } = key;

    public string Description { get; set; } = description;

    public Attribute[] Attributes { get; set; } = attributes;

    public string Stability { get; set; } = stability;

    public string SupportabilityLevel { get; set; } = supportabilityLevel;

    public Dependency[] Dependencies { get; set; } = new Dependency[] { dependency };
}

public class Attribute(string id)
{
    public string Id { get; set; } = id;
}
