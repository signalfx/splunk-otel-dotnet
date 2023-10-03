using YamlDotNet.Serialization;

public static class InstrumentationData
{
    public static Instrumentation[] GetInstrumentations()
    {
        var instrumentations = new Instrumentation[]
        {
            new(new [] {"ASPNET"}, "ASP.NET Framework (.NET Framework)", "See :ref:`dotnet-otel-versions`", "MVC / WebApi (Only integrated pipeline mode supported)", "beta", "community", new [] {new Dependency("https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNet", "1.0.0-rc9.9", "beta"), new Dependency("https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule", "1.0.0-rc9.9", "beta") }) {ToDoComment = "version with ref link"},
            new("ASPNETCORE", "ASP.NET Core", "See :ref:`dotnet-otel-versions`", null, "beta", "community", new Dependency("https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.AspNetCore", "1.5.1-beta.1", "beta")) {ToDoComment = "reflink in version"},
            new("AZURE", "Azure SDK", "`Azure.` prefixed packages, released after October 1, 2021", null, "beta", "third-party"),
            new("ELASTICSEARCH", "Elastic.Clients.Elasticsearch", "8.0.0 and higher", null, "beta", "third-party"),
            new("ENTITYFRAMEWORKCORE", "Microsoft.EntityFrameworkCore", "6.0.12 and higher", "Not supported on .NET Framework", "beta", "community", new Dependency("https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.EntityFrameworkCore", "1.0.0-beta.7", "beta")),
            new("GRAPHQL", "GraphQL", "7.5.0 and higher", "Not supported on .NET Framework", "beta", "third-party"),
            new("GRPCNETCLIENT", "Grpc.Net.Client", "2.52.0 to 3.0.0", null, "beta", "community", new Dependency("https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.GrpcNetClient", "1.5.1-beta.1", "beta")),
            new("HTTPCLIENT", "System.Net.Http.HttpClient and System.Net.HttpWebRequest", "See :ref:`dotnet-otel-versions`", null, "beta", "community", new Dependency("https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.Http", "1.5.1-beta.1", "beta") ) {ToDoComment = "how to handle two libraries? convert to array?, reflink in versions"},
            new("MASSTRANSIT", "MassTransit", "8.0.0 and higher", "Not supported on .NET Framework", "beta", "third-party"),
            new("MONGODB", "MongoDB.Driver.Core", "2.13.3 to 3.0.0", "Not supported on .NET Framework", "beta", "third-party"),
            new("MYSQLCONNECTOR", "MySqlConnector", "2.0.0 and higher", null, "beta", "third-party"),
            new("MYSQLDATA", "MySql.Data", "8.1.0 and higher", "Not supported on .NET Framework", "beta", "third-party"),
            new("NPGSQL", "Npgsql", "6.0.0 and higher", null, "beta", "third-party"),
            new("NSERVICEBUS", "NServiceBus", "8.0.0 and higher", null, "beta", "third-party"),
            new("QUARTZ", "Quartz", "3.4.0 and higher", "Not supported on .NET Framework 4.7.1 and lower", "beta", "community", new Dependency("https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Quartz", "1.0.0-alpha.3", "alpha") ),
            new ("STACKEXCHANGEREDIS", "StackExchange.Redis", "2.0.405 to 3.0.0", "Not supported on .NET Framework", "beta", "community", new Dependency("https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.StackExchangeRedis", "1.0.0-rc9.10", "beta") ),
            new(new []{"WCFCLIENT", "WCFSERVICE" }, "System.ServiceModel", "4.7.0 and higher of `System.ServiceModel.Primitives`", "Service side not supported on .NET. `WCFCLIENT for client side instrumentation and `WCFSERVICE` for service side instrumentation", "beta", "community", new []{ new Dependency("https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Wcf", "1.0.0-rc.12", "beta") }),
        };

        return instrumentations;
    }
}

public class Instrumentation
{
    public Instrumentation(string key, string library, string version, string? comment, string stability,
        string supportabilityLevel)
        : this(new[] {key}, library, version, comment, stability, supportabilityLevel, Array.Empty<Dependency>())
    {
    }

    public Instrumentation(string key, string library, string version, string? comment, string stability, string supportabilityLevel, Dependency dependency)
        : this(new[] { key }, library, version, comment, stability, supportabilityLevel, new[] { dependency })
    {
    }

    public Instrumentation(string[] keys, string library, string version, string? comment, string stability, string supportabilityLevel, Dependency[] dependencies)
    {
        Keys = keys;
        Library = library;
        Version = version;
        Comment = comment;
        Stability = stability;
        SupportabilityLevel = supportabilityLevel;
        Dependencies = dependencies;
    }

    public string[] Keys { get; set; }

    public string Library { get; set; }

    public string Version { get; set; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Comment { get; set; }

    public string Stability { get; set; }

    public string SupportabilityLevel { get; set; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Dependency[] Dependencies { get; set; }

    // TODO remove this property
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? ToDoComment { get; set; }
}
