using YamlDotNet.Serialization;

namespace MatrixHelper;

public static class InstrumentationData
{
    public static Instrumentation[] GetInstrumentations()
    {
        const string counter = "counter";
        const string upDownCounter = "updowncounter";
        const string histogram = "histogram";

        var netRuntimeMetrics = new MetricData[]
        {
            new("process.runtime.dotnet.gc.collections.count", counter, "Number of garbage collections since the process started."),
            new("process.runtime.dotnet.gc.heap.size", upDownCounter, "Heap size, as observed during the last garbage collection. Only available for .NET 6 or higher."),
            new("process.runtime.dotnet.gc.heap.fragmentation.size", upDownCounter, "Heap fragmentation, as observed during the last garbage collection. Only available for .NET 7 or higher."),
            new("process.runtime.dotnet.gc.objects.size", upDownCounter, "Count of bytes currently in use by live objects in the GC heap."),
            new("process.runtime.dotnet.gc.allocations.size", counter, "Count of bytes allocated on the managed GC heap since the process started. Only available for .NET 6 or higher."),
            new("process.runtime.dotnet.gc.committed_memory.size", upDownCounter, "Amount of committed virtual memory for the managed GC heap, as observed during the last garbage collection. Only available for .NET 6 and higher."),
            new("process.runtime.dotnet.gc.duration", counter, "The total amount of time paused in GC since the process start. Only available for .NET 7 and higher."),
            new("process.runtime.dotnet.monitor.lock_contention.count", counter, "Contentions count when trying to acquire a monitor lock since the process started."),
            new("process.runtime.dotnet.thread_pool.threads.count", counter, "Number of thread pool threads, as observed during the last measurement. Only available for .NET 6 or higher."),
            new("process.runtime.dotnet.thread_pool.completed_items.count", counter, "Number of work items processed by the thread pool since the process started. Only available for .NET 6 or higher."),
            new("process.runtime.dotnet.thread_pool.queue.length", upDownCounter, "Number of work items currently queued for processing by the thread pool. Only available for .NET 6 or higher."),
            new("process.runtime.dotnet.jit.il_compiled.size", counter, "Bytes of intermediate language that have been compiled since the process started. Only available for .NET 6 or higher."),
            new("process.runtime.dotnet.jit.methods_compiled.count", counter, "Number of times the JIT compiler compiled a method since the process started. Only available for .NET 6 or higher."),
            new("process.runtime.dotnet.jit.compilation_time", counter, "Amount of time the compiler spent compiling methods since the process started. Only available for .NET 6 or higher."),
            new("process.runtime.dotnet.timer.count", upDownCounter, "Number of timer instances currently active. Only available for .NET 6 or higher."),
            new("process.runtime.dotnet.assemblies.count", upDownCounter, "Number of .NET assemblies that are currently loaded."),
            new("process.runtime.dotnet.exceptions.count", counter, "Count of exceptions thrown in managed code since the observation started."),
        };

        var instrumentations = new Instrumentation[]
        {
            new(new[] {"ASPNET"}, new [] {new InstrumentedComponent("ASP.NET Framework (.NET Framework)", "See general requirements") }, "MVC / WebApi (Only integrated pipeline mode supported). Metrics requires trace instrumentation.", "beta", "community", new [] {new Dependency("ASP.NET Instrumentation for OpenTelemetry", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNet", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNet", "1.0.0-rc9.9", "beta"), new Dependency("ASP.NET Telemetry HttpModule for OpenTelemetry", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule", "1.0.0-rc9.9", "beta") }, new SignalsList[]{new TracesList(), new MetricList(new MetricData("http.server.duration_{bucket|count|sum}", histogram, "Duration of the inbound HTTP request, in the form of count, sum, and histogram buckets. This metric originates multiple metric time series, which might result in increased data ingestion costs.")) }, Array.Empty<Setting>()),
            new("ASPNETCORE", new InstrumentedComponent("ASP.NET Core", "See general requirements"), "Metrics automatically activates `Microsoft.AspNetCore.Hosting.HttpRequestIn` spans.", "beta", "community", new Dependency("ASP.NET Core Instrumentation for OpenTelemetry .NET","https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.AspNetCore", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNetCore", "1.5.1-beta.1", "beta"), new SignalsList[]{new TracesList(), new MetricList(new MetricData("http.server.duration_{bucket|count|sum}", histogram, "Duration of the inbound HTTP request, in the form of count, sum, and histogram buckets. This metric originates multiple metric time series, which might result in increased data ingestion costs.")) }),
            new("AZURE", new InstrumentedComponent("Azure SDK", "`Azure.` prefixed packages, released after October 1, 2021"), null, "beta", "third-party", new TracesList()),
            new("ELASTICSEARCH", new InstrumentedComponent("Elastic.Clients.Elasticsearch", "8.0.0 and higher"), null, "beta", "third-party", new TracesList()),
            new("ENTITYFRAMEWORKCORE", new InstrumentedComponent("Microsoft.EntityFrameworkCore", "6.0.12 and higher"), "Not supported on .NET Framework", "beta", "community", new Dependency("EntityFrameworkCore Instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.EntityFrameworkCore", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.EntityFrameworkCore", "1.0.0-beta.7", "beta"), new SignalsList[]{new TracesList()}),
            new(new [] {"GRAPHQL"}, new [] {new InstrumentedComponent("GraphQL", "7.5.0 and higher")}, "Not supported on .NET Framework", "beta", "third-party", Array.Empty<Dependency>(), new SignalsList[] {new TracesList()}, new Setting[] {new("OTEL_DOTNET_AUTO_GRAPHQL_SET_DOCUMENT", "Whether the GraphQL instrumentation can pass raw queries as a `graphql.document` attribute. As queries might contain sensitive information, the default value is `false`.", "false", "boolean", SettingsData.InstrumentationCategory)}),
            new("GRPCNETCLIENT", new InstrumentedComponent("Grpc.Net.Client", "2.52.0 to 3.0.0"), null, "beta", "community", new Dependency("Grpc.Net.Client Instrumentation for OpenTelemetry", "https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.GrpcNetClient", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.GrpcNetClient", "1.5.1-beta.1", "beta"), new SignalsList[] { new TracesList() }),
            new(new [] {"HTTPCLIENT"}, new[] {new InstrumentedComponent("System.Net.Http.HttpClient", "See general requirements"),  new InstrumentedComponent("System.Net.HttpWebRequest", "See general requirements") }, null, "beta", "community", new[] { new Dependency("HttpClient and HttpWebRequest instrumentation for OpenTelemetry", "https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.Http", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Http", "1.5.1-beta.1", "beta") }, new SignalsList[] { new TracesList(), new MetricList(new MetricData("http.client.duration_{bucket|count|sum}", histogram, "Duration of outbound HTTP requests, in the form of count, sum, and histogram buckets. This metric originates multiple metric time series, which might result in increased data ingestion costs."), new MetricData("http.server.duration_{bucket|count|sum}", histogram, "Duration of the inbound HTTP request, in the form of count, sum, and histogram buckets. This metric originates multiple metric time series, which might result in increased data ingestion costs.")) }, Array.Empty<Setting>()),
            new("MASSTRANSIT", new InstrumentedComponent("MassTransit", "8.0.0 and higher"), "Not supported on .NET Framework", "beta", "third-party", new TracesList()),
            new("MONGODB", new InstrumentedComponent("MongoDB.Driver.Core", "2.13.3 to 3.0.0"), "Not supported on .NET Framework", "beta", "third-party", new TracesList()),
            new("MYSQLCONNECTOR", new InstrumentedComponent("MySqlConnector", "2.0.0 and higher"), null, "beta", "third-party", new TracesList()),
            new("MYSQLDATA", new InstrumentedComponent("MySql.Data", "8.1.0 and higher"), "Not supported on .NET Framework", "beta", "third-party", new TracesList()),
            new("NPGSQL", new InstrumentedComponent("Npgsql", "6.0.0 and higher"), null, "beta", "third-party", new TracesList()),
            new(new[] {"NSERVICEBUS"}, new [] {new InstrumentedComponent("NServiceBus", "8.0.0 and higher")}, null, "beta", "third-party", Array.Empty<Dependency>(), new SignalsList[]{new TracesList(), new MetricList(new MetricData("nservicebus.messaging.successes", counter, "Number of messages successfully processed by the endpoint."), new MetricData("nservicebus.messaging.fetches", counter, "Number of messages retrieved from the queue by the endpoint."), new MetricData("nservicebus.messaging.failures", counter, "Number of messages unsuccessfully processed by the endpoint."))}, Array.Empty<Setting>()),
            new("QUARTZ", new InstrumentedComponent("Quartz", "3.4.0 and higher"), "Not supported on .NET Framework 4.7.1 and lower", "beta", "community", new Dependency("QuartzNET Instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Quartz", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Quartz", "1.0.0-alpha.3", "alpha"), new SignalsList[] { new TracesList() } ),
            new("STACKEXCHANGEREDIS", new InstrumentedComponent("StackExchange.Redis", "2.0.405 to 3.0.0"), "Not supported on .NET Framework", "beta", "community", new Dependency("StackExchange.Redis Instrumentation for OpenTelemetry", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.StackExchangeRedis", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.StackExchangeRedis", "1.0.0-rc9.10", "beta"), new SignalsList[] { new TracesList() } ),
            new(new []{"WCFCLIENT", "WCFSERVICE" }, new []{new InstrumentedComponent("System.ServiceModel", "4.7.0 and higher of `System.ServiceModel.Primitives`")}, "Service side not supported on .NET. `WCFCLIENT for client side instrumentation and `WCFSERVICE` for service side instrumentation", "beta", "community", new []{ new Dependency("WCF Instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Wcf", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Wcf", "1.0.0-rc.12", "beta") }, new SignalsList[] { new TracesList() }, Array.Empty<Setting>()),

            new("NETRUNTIME", new InstrumentedComponent(".NET runtime", null), null, "beta", "community", new Dependency("Runtime Instrumentation for OpenTelemetry .NET","https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Runtime", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Runtime", "1.5.1", "stable"), new SignalsList[]{new MetricList(netRuntimeMetrics) }),
            new("PROCESS", new InstrumentedComponent("Process", null), null, "beta", "community", new Dependency("Process Instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Process", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Process", "0.5.0-beta.3", "beta"), new SignalsList[]{new MetricList(new MetricData("process.memory.usage", upDownCounter, "The amount of physical memory allocated for this process."), new ("process.memory.virtual", upDownCounter, "The amount of committed virtual memory for this process."), new MetricData("process.cpu.time", counter, "Total CPU seconds broken down by different states, such as user and system."), new("process.cpu.count", upDownCounter, "Total CPU seconds broken down by different states, such as user and system."), new("process.threads", upDownCounter, "Process threads count."))}),

            new("ILOGGER", new InstrumentedComponent("Microsoft.Extensions.Logging", "6.0.0 and higher"), "Not supported on .NET Framework", "beta", "community", new LogLists())
        };

        return instrumentations;
    }
}

public class Instrumentation
{
    public Instrumentation(string key, InstrumentedComponent instrumentedComponent, string? description, string stability, string support, SignalsList signalsList)
        : this(new[] {key}, new [] { instrumentedComponent }, description, stability, support, Array.Empty<Dependency>(), new[] {signalsList}, Array.Empty<Setting>())
    {
    }

    public Instrumentation(string key, InstrumentedComponent instrumentedComponent, string? description, string stability, string support, Dependency dependency, SignalsList[] signalsList)
        : this(new[] { key }, new[] { instrumentedComponent }, description, stability, support, new[] { dependency }, signalsList, Array.Empty<Setting>())
    {
    }

    public Instrumentation(string[] keys, InstrumentedComponent[] instrumentedComponents, string? description, string stability, string support, Dependency[] dependencies, SignalsList[] signalsList, Setting[] settings)
    {
        Keys = keys;
        InstrumentedComponents = instrumentedComponents;
        Description = description;
        Stability = stability;
        Support = support;
        Dependencies = dependencies;
        Signals = signalsList;
        Settings = settings;
    }

    public string[] Keys { get; set; }

    public InstrumentedComponent[] InstrumentedComponents { get; set; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Description { get; set; }

    public string Stability { get; set; }

    public string Support { get; set; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Dependency[] Dependencies { get; set; }

    public SignalsList[] Signals { get; set; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Setting[] Settings { get; set; }
}

public class SignalsList
{
}

public class MetricData
{
    public MetricData(string metricName, string instrument, string description)
    {
        MetricName = metricName;
        Instrument = instrument;
        Description = description;
    }

    public string MetricName { get; set; }
    public string Instrument { get; set; }
    public string Description { get; set; }
}

public class MetricList : SignalsList
{
    public MetricList()
    {
    }

    public MetricList(params MetricData[] metrics)
    {
        Metrics = metrics;
    }

    public MetricData[] Metrics { get; set; }
}

public class Log
{
}

public class LogLists: SignalsList
{
    public Log[] Logs { get; set; }
}

public class Trace
{
}

public class TracesList: SignalsList
{
    public Trace[] Traces { get; set; }
}

public class InstrumentedComponent
{
    public InstrumentedComponent(string name, string? supportedVersions)
    {
        Name = name;
        SupportedVersions = supportedVersions;
    }

    public string Name { get; set; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? SupportedVersions { get; set; }
}
