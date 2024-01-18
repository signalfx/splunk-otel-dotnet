// <copyright file="InstrumentationData.cs" company="Splunk Inc.">
// Copyright Splunk Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace MatrixHelper;

internal static class InstrumentationData
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

        var aspNetCoreMetrics = new MetricData[]
        {
            new("http.server.request.duration", histogram, "Duration of the inbound HTTP request."),
            // following metrics .NET8+ only
            new("http.server.active_requests", upDownCounter, "Number of active HTTP server requests. Supported only on .NET8+"),
            new("kestrel.active_connections", upDownCounter, "Number of connections that are currently active on the server. Supported only on .NET8+"),
            new("kestrel.connection.duration", histogram, "The duration of connections on the server. Supported only on .NET8+"),
            new("kestrel.rejected_connections", counter, "Number of connections rejected by the server. Connections are rejected when the currently active count exceeds the value configured with MaxConcurrentConnections. Supported only on .NET8+"),
            new("kestrel.queued_connections", upDownCounter, "Number of connections that are currently queued and are waiting to start. Supported only on .NET8+"),
            new("kestrel.queued_requests", upDownCounter, "Number of HTTP requests on multiplexed connections (HTTP/2 and HTTP/3) that are currently queued and are waiting to start. Supported only on .NET8+"),
            new("kestrel.upgraded_connections", upDownCounter, "Number of HTTP connections that are currently upgraded (WebSockets). The number only tracks HTTP/1.1 connections. Supported only on .NET8+"),
            new("kestrel.tls_handshake.duration", histogram, "The duration of TLS handshakes on the server. Supported only on .NET8+"),
            new("kestrel.active_tls_handshakes", upDownCounter, "Number of TLS handshakes that are currently in progress on the server. Supported only on .NET8+"),
            new("signalr.server.connection.duration", histogram, "The duration of connections on the server. Supported only on .NET8+"),
            new("signalr.server.active_connections", upDownCounter, "Number of connections that are currently active on the server. Supported only on .NET8+"),
            new("aspnetcore.routing.match_attempts", counter, "Number of requests that were attempted to be matched to an endpoint. Supported only on .NET8+"),
            new("aspnetcore.diagnostics.exceptions", counter, "Number of exceptions caught by exception handling middleware. Supported only on .NET8+"),
            new("aspnetcore.rate_limiting.active_request_leases", upDownCounter, "Number of HTTP requests that are currently active on the server that hold a rate limiting lease. Supported only on .NET8+"),
            new("aspnetcore.rate_limiting.request_lease.duration", histogram, "The duration of rate limiting leases held by HTTP requests on the server. Supported only on .NET8+"),
            new("aspnetcore.rate_limiting.queued_requests", upDownCounter, "Number of HTTP requests that are currently queued, waiting to acquire a rate limiting lease. Supported only on .NET8+"),
            new("aspnetcore.rate_limiting.request.time_in_queue", histogram, "The duration of HTTP requests in a queue, waiting to acquire a rate limiting lease. Supported only on .NET8+"),
            new("aspnetcore.rate_limiting.requests", counter,  "Number of requests that tried to acquire a rate limiting lease. Requests could be rejected by global or endpoint rate limiting policies. Or the request could be canceled while waiting for the lease. Supported only on .NET8+")
        };

        // TODO
        var httpClientMetrics = new MetricData[]
        {
            new("http.client.request.duration", histogram, "Duration of HTTP client requests."),
            // following metrics .NET8+ only
            new("http.client.active_requests", upDownCounter, "Number of outbound HTTP requests that are currently active on the client. Supported only on .NET8+"),
            new("http.client.open_connections", upDownCounter, "Number of outbound HTTP connections that are currently active or idle on the client. Supported only on .NET8+"),
            new("http.client.connection.duration", histogram, "The duration of successfully established outbound HTTP connections. Supported only on .NET8+"),
            new("http.client.request.time_in_queue", histogram, "The amount of time requests spent on a queue waiting for an available connection. Supported only on .NET8+"),
            new("dns.lookup.duration", histogram, "Measures the time taken to perform a DNS lookup. Supported only on .NET8+")
        };

        var instrumentations = new Instrumentation[]
        {
            new(new[] { "ASPNET" }, new[] { new InstrumentedComponent("ASP.NET Framework (.NET Framework)", "See general requirements") }, "MVC / WebApi (Only integrated pipeline mode supported). Metrics requires trace instrumentation.", "beta", "community", new[] { new Dependency("ASP.NET Instrumentation for OpenTelemetry", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNet", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNet", "1.7.0-beta.1", "beta"), new Dependency("ASP.NET Telemetry HttpModule for OpenTelemetry", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule", "1.6.0-beta.2", "beta") }, new SignalsList[] { new TracesList(), new MetricList(new MetricData("http.server.request.duration", histogram, "Measures the duration of inbound HTTP requests.")) }, Array.Empty<Setting>()),
            new("ASPNETCORE", new InstrumentedComponent("ASP.NET Core", "See general requirements"), "Metrics automatically activates `Microsoft.AspNetCore.Hosting.HttpRequestIn` spans.", "beta", "community", new Dependency("ASP.NET Core Instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.AspNetCore", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNetCore", "1.7.0", "beta"), new SignalsList[] { new TracesList(), new MetricList(aspNetCoreMetrics) }),
            new("AZURE", new InstrumentedComponent("Azure SDK", "`Azure.` prefixed packages, released after October 1, 2021"), null, "beta", "third-party", new TracesList()),
            new("ELASTICSEARCH", new InstrumentedComponent("Elastic.Clients.Elasticsearch", "8.0.0 to 8.9.3"), "Versions 8.10.0 and higher are supported by `Elastic.Transport` instrumentation.", "beta", "third-party", new TracesList()),
            new("ELASTICTRANSPORT", new InstrumentedComponent("Elastic.Transport", "0.4.16 and higher"), null, "beta", "third-party", new TracesList()),
            new("ENTITYFRAMEWORKCORE", new InstrumentedComponent("Microsoft.EntityFrameworkCore", "6.0.12 and higher"), "Not supported on .NET Framework", "beta", "community", new Dependency("EntityFrameworkCore Instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.EntityFrameworkCore", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.EntityFrameworkCore", "1.0.0-beta.9", "beta"), new SignalsList[] { new TracesList() }),
            new(new[] { "GRAPHQL" }, new[] { new InstrumentedComponent("GraphQL", "7.5.0 and higher") }, "Not supported on .NET Framework", "beta", "third-party", Array.Empty<Dependency>(), new SignalsList[] { new TracesList() }, new Setting[] { new("OTEL_DOTNET_AUTO_GRAPHQL_SET_DOCUMENT", "Whether the GraphQL instrumentation can pass raw queries as a `graphql.document` attribute. As queries might contain sensitive information, the default value is `false`.", "false", "boolean", SettingsData.InstrumentationCategory) }),
            new("GRPCNETCLIENT", new InstrumentedComponent("Grpc.Net.Client", "2.52.0 to 3.0.0"), null, "beta", "community", new Dependency("Grpc.Net.Client Instrumentation for OpenTelemetry", "https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.GrpcNetClient", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.GrpcNetClient", "1.6.0-beta.3", "beta"), new SignalsList[] { new TracesList() }),
            new(new[] { "HTTPCLIENT" }, new[] { new InstrumentedComponent("System.Net.Http.HttpClient", "See general requirements"),  new InstrumentedComponent("System.Net.HttpWebRequest", "See general requirements") }, null, "beta", "community", new[] { new Dependency("HttpClient and HttpWebRequest instrumentation for OpenTelemetry", "https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.Http", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Http", "1.7.0", "beta") }, new SignalsList[] { new TracesList(), new MetricList(httpClientMetrics) }, Array.Empty<Setting>()),
            new("KAFKA", new InstrumentedComponent("Confluent.Kafka", "1.4.0 to 3.0.0"), null, "beta", "community", new TracesList()),
            new("MASSTRANSIT", new InstrumentedComponent("MassTransit", "8.0.0 and higher"), "Not supported on .NET Framework", "beta", "third-party", new TracesList()),
            new("MONGODB", new InstrumentedComponent("MongoDB.Driver.Core", "2.13.3 to 3.0.0"), "Not supported on .NET Framework", "beta", "third-party", new TracesList()),
            new("MYSQLCONNECTOR", new InstrumentedComponent("MySqlConnector", "2.0.0 and higher"), null, "beta", "third-party", new TracesList()),
            new("MYSQLDATA", new InstrumentedComponent("MySql.Data", "8.1.0 and higher"), "Not supported on .NET Framework", "beta", "third-party", new TracesList()),
            new("NPGSQL", new InstrumentedComponent("Npgsql", "6.0.0 and higher"), null, "beta", "third-party", new TracesList()),
            new(new[] { "NSERVICEBUS" }, new[] { new InstrumentedComponent("NServiceBus", "8.0.0 and higher") }, null, "beta", "third-party", Array.Empty<Dependency>(), new SignalsList[] { new TracesList(), new MetricList(new MetricData("nservicebus.messaging.successes", counter, "Number of messages successfully processed by the endpoint."), new MetricData("nservicebus.messaging.fetches", counter, "Number of messages retrieved from the queue by the endpoint."), new MetricData("nservicebus.messaging.failures", counter, "Number of messages unsuccessfully processed by the endpoint.")) }, Array.Empty<Setting>()),
            new("QUARTZ", new InstrumentedComponent("Quartz", "3.4.0 and higher"), "Not supported on .NET Framework 4.7.1 and lower", "beta", "community", new Dependency("QuartzNET Instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Quartz", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Quartz", "1.0.0-beta.1", "beta"), new SignalsList[] { new TracesList() }),
            new(new[] { "SQLCLIENT" }, new[] { new InstrumentedComponent("Microsoft.Data.SqlClient", "v3.* is not supported on .NET Framework"), new InstrumentedComponent("System.Data.SqlClient", "4.8.5 and higher"), new InstrumentedComponent("System.Data", "Shipped with .NET Framework") }, null, "beta", "community", Array.Empty<Dependency>(), new SignalsList[] { new TracesList() }, Array.Empty<Setting>()),
            new("STACKEXCHANGEREDIS", new InstrumentedComponent("StackExchange.Redis", "2.0.405 to 3.0.0"), "Not supported on .NET Framework", "beta", "community", new Dependency("StackExchange.Redis Instrumentation for OpenTelemetry", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.StackExchangeRedis", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.StackExchangeRedis", "1.0.0-rc9.13", "beta"), new SignalsList[] { new TracesList() }),
            new(new[] { "WCFCLIENT", "WCFSERVICE" }, new[] { new InstrumentedComponent("System.ServiceModel", "4.7.0 and higher of `System.ServiceModel.Primitives`") }, "Service side not supported on .NET. `WCFCLIENT for client side instrumentation and `WCFSERVICE` for service side instrumentation", "beta", "community", new[] { new Dependency("WCF Instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Wcf", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Wcf", "1.0.0-rc.14", "beta") }, new SignalsList[] { new TracesList() }, Array.Empty<Setting>()),

            new("NETRUNTIME", new InstrumentedComponent(".NET runtime", null), null, "beta", "community", new Dependency("Runtime Instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Runtime", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Runtime", "1.7.0", "stable"), new SignalsList[] { new MetricList(netRuntimeMetrics) }),
            new("PROCESS", new InstrumentedComponent("Process", null), null, "beta", "community", new Dependency("Process Instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Process", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Process", "0.5.0-beta.4", "beta"), new SignalsList[] { new MetricList(new MetricData("process.memory.usage", upDownCounter, "The amount of physical memory allocated for this process."), new("process.memory.virtual", upDownCounter, "The amount of committed virtual memory for this process."), new MetricData("process.cpu.time", counter, "Total CPU seconds broken down by different states, such as user and system."), new("process.cpu.count", upDownCounter, "Total CPU seconds broken down by different states, such as user and system."), new("process.threads", upDownCounter, "Process threads count.")) }),

            new("ILOGGER", new InstrumentedComponent("Microsoft.Extensions.Logging", "8.0.0 and higher"), "Not supported on .NET Framework", "beta", "community", new LogLists())
        };

        return instrumentations;
    }
}
