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

        const string conditionallyBundled = "APM bundled, if data points for the metric contain `telemetry.sdk.language` attribute.";

        var netRuntimeMetrics = new MetricData[]
        {
            new("dotnet.process.cpu.time", counter, "CPU time used by the process. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.process.memory.working_set", upDownCounter, "The number of bytes of physical memory mapped to the process context. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.gc.collections", counter, "The number of garbage collections that have occurred since the process has started. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.gc.heap.total_allocated", counter, "The approximate number of bytes allocated on the managed GC heap since the process started. The returned value does not include any native allocations. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.gc.last_collection.memory.committed_size", upDownCounter, "The amount of committed virtual memory in use by the .NET GC, as observed during the latest garbage collection. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.gc.last_collection.heap.size", upDownCounter, "The managed GC heap size (including fragmentation, conditionallyBundled, true), as observed during the latest garbage collection. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.gc.last_collection.heap.fragmentation.size", upDownCounter, "The heap fragmentation, as observed during the latest garbage collection. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.gc.pause.time", counter, "The total amount of time paused in GC since the process started. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.jit.compiled_il.size", counter, "Count of bytes of intermediate language that have been compiled since the process started. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.jit.compiled_methods", counter, "The number of times the JIT compiler (re)compiled methods since the process started. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.jit.compilation.time", counter, "The amount of time the JIT compiler has spent compiling methods since the process started. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.thread_pool.thread.count", upDownCounter, "The number of thread pool threads that currently exist. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.thread_pool.work_item.count", counter, "The number of work items that the thread pool has completed since the process started. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.thread_pool.queue.length", upDownCounter, "The number of work items that are currently queued to be processed by the thread pool. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.monitor.lock_contentions", counter, "The number of times there was contention when trying to acquire a monitor lock since the process started. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.timer.count", upDownCounter, "The number of timer instances that are currently active. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.assembly.count", upDownCounter, "The number of .NET assemblies that are currently loaded. Only available for .NET 9.", conditionallyBundled),
            new("dotnet.exceptions", counter, "The number of exceptions that have been thrown in managed code. Only available for .NET 9.", conditionallyBundled),
            new("process.runtime.dotnet.gc.collections.count", counter, "Number of garbage collections since the process started. Only available for .NET 8. and .NET Framework.", conditionallyBundled),
            new("process.runtime.dotnet.gc.heap.size", upDownCounter, "Heap size, as observed during the last garbage collection. Only available for .NET 8.", conditionallyBundled),
            new("process.runtime.dotnet.gc.heap.fragmentation.size", upDownCounter, "Heap fragmentation, as observed during the last garbage collection. Only available for .NET 8.", conditionallyBundled),
            new("process.runtime.dotnet.gc.objects.size", upDownCounter, "Count of bytes currently in use by live objects in the GC heap. Only available for .NET 8. and .NET Framework.", conditionallyBundled),
            new("process.runtime.dotnet.gc.allocations.size", counter, "Count of bytes allocated on the managed GC heap since the process started. Only available for .NET 8.", conditionallyBundled),
            new("process.runtime.dotnet.gc.committed_memory.size", upDownCounter, "Amount of committed virtual memory for the managed GC heap, as observed during the last garbage collection. Only available for .NET 8.", conditionallyBundled),
            new("process.runtime.dotnet.gc.duration", counter, "The total amount of time paused in GC since the process start. Only available for .NET 8.", conditionallyBundled),
            new("process.runtime.dotnet.monitor.lock_contention.count", counter, "Contentions count when trying to acquire a monitor lock since the process started. Only available for .NET 8. and .NET Framework.", conditionallyBundled),
            new("process.runtime.dotnet.thread_pool.threads.count", counter, "Number of thread pool threads, as observed during the last measurement. Only available for .NET 8.", conditionallyBundled),
            new("process.runtime.dotnet.thread_pool.completed_items.count", counter, "Number of work items processed by the thread pool since the process started. Only available for .NET 8.", conditionallyBundled),
            new("process.runtime.dotnet.thread_pool.queue.length", upDownCounter, "Number of work items currently queued for processing by the thread pool. Only available for .NET 8.", conditionallyBundled),
            new("process.runtime.dotnet.jit.il_compiled.size", counter, "Bytes of intermediate language that have been compiled since the process started. Only available for .NET 8.", conditionallyBundled),
            new("process.runtime.dotnet.jit.methods_compiled.count", counter, "Number of times the JIT compiler compiled a method since the process started. Only available for .NET 8.", conditionallyBundled),
            new("process.runtime.dotnet.jit.compilation_time", counter, "Amount of time the compiler spent compiling methods since the process started. Only available for .NET 8.", conditionallyBundled),
            new("process.runtime.dotnet.timer.count", upDownCounter, "Number of timer instances currently active. Only available for .NET 8.", conditionallyBundled),
            new("process.runtime.dotnet.assemblies.count", upDownCounter, "Number of .NET assemblies that are currently loaded. Only available for .NET 8. and .NET Framework.", conditionallyBundled),
            new("process.runtime.dotnet.exceptions.count", counter, "Count of exceptions thrown in managed code since the observation started. Only available for .NET 8. and .NET Framework.", conditionallyBundled),
        };

        var aspNetCoreMetrics = new MetricData[]
        {
            new("http.server.request.duration", histogram, "Duration of the inbound HTTP request.", conditionallyBundled),
            new("http.server.active_requests", upDownCounter, "Number of active HTTP server requests."),
            new("kestrel.active_connections", upDownCounter, "Number of connections that are currently active on the server."),
            new("kestrel.connection.duration", histogram, "The duration of connections on the server."),
            new("kestrel.rejected_connections", counter, "Number of connections rejected by the server. Connections are rejected when the currently active count exceeds the value configured with MaxConcurrentConnections."),
            new("kestrel.queued_connections", upDownCounter, "Number of connections that are currently queued and are waiting to start."),
            new("kestrel.queued_requests", upDownCounter, "Number of HTTP requests on multiplexed connections (HTTP/2 and HTTP/3) that are currently queued and are waiting to start."),
            new("kestrel.upgraded_connections", upDownCounter, "Number of HTTP connections that are currently upgraded (WebSockets). The number only tracks HTTP/1.1 connections."),
            new("kestrel.tls_handshake.duration", histogram, "The duration of TLS handshakes on the server."),
            new("kestrel.active_tls_handshakes", upDownCounter, "Number of TLS handshakes that are currently in progress on the server."),
            new("signalr.server.connection.duration", histogram, "The duration of connections on the server."),
            new("signalr.server.active_connections", upDownCounter, "Number of connections that are currently active on the server."),
            new("aspnetcore.routing.match_attempts", counter, "Number of requests that were attempted to be matched to an endpoint."),
            new("aspnetcore.diagnostics.exceptions", counter, "Number of exceptions caught by exception handling middleware."),
            new("aspnetcore.rate_limiting.active_request_leases", upDownCounter, "Number of HTTP requests that are currently active on the server that hold a rate limiting lease."),
            new("aspnetcore.rate_limiting.request_lease.duration", histogram, "The duration of rate limiting leases held by HTTP requests on the server."),
            new("aspnetcore.rate_limiting.queued_requests", upDownCounter, "Number of HTTP requests that are currently queued, waiting to acquire a rate limiting lease."),
            new("aspnetcore.rate_limiting.request.time_in_queue", histogram, "The duration of HTTP requests in a queue, waiting to acquire a rate limiting lease."),
            new("aspnetcore.rate_limiting.requests", counter,  "Number of requests that tried to acquire a rate limiting lease. Requests could be rejected by global or endpoint rate limiting policies. Or the request could be canceled while waiting for the lease.")
        };

        var httpClientMetrics = new MetricData[]
        {
            new("http.client.request.duration", histogram, "Duration of HTTP client requests.", conditionallyBundled),
            // following metrics .NET8+ only
            new("http.client.active_requests", upDownCounter, "Number of outbound HTTP requests that are currently active on the client. Supported only on .NET 8+"),
            new("http.client.open_connections", upDownCounter, "Number of outbound HTTP connections that are currently active or idle on the client. Supported only on .NET 8+"),
            new("http.client.connection.duration", histogram, "The duration of successfully established outbound HTTP connections. Supported only on .NET 8+"),
            new("http.client.request.time_in_queue", histogram, "The amount of time requests spent on a queue waiting for an available connection. Supported only on .NET 8+"),
            new("dns.lookup.duration", histogram, "Measures the time taken to perform a DNS lookup. Supported only on .NET 8+")
        };

        var instrumentations = new Instrumentation[]
        {
            new(new[] { "ASPNET" }, new[] { new InstrumentedComponent("ASP.NET Framework (.NET Framework)", "See general requirements") }, "MVC / WebApi (Only integrated pipeline mode supported). Metrics requires trace instrumentation.", "beta", "community", new[] { new Dependency("ASP.NET Instrumentation for OpenTelemetry", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNet", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNet", "1.11.0-beta.2", "beta"), new Dependency("ASP.NET Telemetry HttpModule for OpenTelemetry", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule", "1.11.0-beta.2", "beta") }, new SignalsList[] { new TracesList(), new MetricList(new MetricData("http.server.request.duration", histogram, "Measures the duration of inbound HTTP requests.", conditionallyBundled)) }, new Setting[] { new("OTEL_DOTNET_AUTO_TRACES_ASPNET_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS", "A comma-separated list of HTTP header names. ASP.NET instrumentations will capture HTTP request header values for all configured header names.", string.Empty, "string", SettingsData.InstrumentationCategory), new("OTEL_DOTNET_AUTO_TRACES_ASPNET_INSTRUMENTATION_CAPTURE_RESPONSE_HEADERS", "A comma-separated list of HTTP header names. ASP.NET instrumentations will capture HTTP response header values for all configured header names. Not supported on IIS Classic mode.", string.Empty, "string", SettingsData.InstrumentationCategory), new("OTEL_DOTNET_EXPERIMENTAL_ASPNET_DISABLE_URL_QUERY_REDACTION", "Whether the ASP.NET instrumentation turns off redaction of the `url.query` attribute value. ", "false", "boolean", SettingsData.InstrumentationCategory) }),
            new(new[] { "ASPNETCORE" }, new[] { new InstrumentedComponent("ASP.NET Core", "See general requirements") }, "Metrics automatically activates `Microsoft.AspNetCore.Hosting.HttpRequestIn` spans.", "beta", "community", new[] { new Dependency("ASP.NET Core Instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet-ontrib/tree/main/src/OpenTelemetry.Instrumentation.AspNetCore", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNetCore", "1.11.1", "beta") }, new SignalsList[] { new TracesList(), new MetricList(aspNetCoreMetrics) }, new Setting[] { new("OTEL_DOTNET_AUTO_TRACES_ASPNETCORE_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS", "A comma-separated list of HTTP header names. ASP.NET Core instrumentations will capture HTTP request header values for all configured header names.", string.Empty, "string", SettingsData.InstrumentationCategory), new("OTEL_DOTNET_AUTO_TRACES_ASPNETCORE_INSTRUMENTATION_CAPTURE_RESPONSE_HEADERS", "A comma-separated list of HTTP header names. ASP.NET Core instrumentations will capture HTTP response header values for all configured header names.", string.Empty, "string", SettingsData.InstrumentationCategory), new("OTEL_DOTNET_EXPERIMENTAL_ASPNETCORE_DISABLE_URL_QUERY_REDACTION", "Whether the ASP.NET Core instrumentation turns off redaction of the `url.query` attribute value.", "false", "boolean", SettingsData.InstrumentationCategory) }),
            new("AZURE", new InstrumentedComponent("Azure SDK", "`Azure.` prefixed packages, released after October 1, 2021"), null, "beta", "third-party", new TracesList()),
            new("ELASTICSEARCH", new InstrumentedComponent("Elastic.Clients.Elasticsearch", "8.0.0 to 8.9.3"), "Versions 8.10.0 and higher are supported by `Elastic.Transport` instrumentation.", "beta", "third-party", new TracesList()),
            new("ELASTICTRANSPORT", new InstrumentedComponent("Elastic.Transport", "0.4.16 and higher"), null, "beta", "third-party", new TracesList()),
            new(new[] { "ENTITYFRAMEWORKCORE" }, new[] { new InstrumentedComponent("Microsoft.EntityFrameworkCore", "6.0.12 and higher") }, "Not supported on .NET Framework", "beta", "community", new[] { new Dependency("EntityFrameworkCore Instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.EntityFrameworkCore", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.EntityFrameworkCore", "1.11.0-beta.2", "beta") }, new SignalsList[] { new TracesList() }, new Setting[] { new("OTEL_DOTNET_AUTO_ENTITYFRAMEWORKCORE_SET_DBSTATEMENT_FOR_TEXT", "Whether the Entity Framework Core instrumentation can pass SQL statements through the `db.statement` attribute. Queries might contain sensitive information. If set to `false`, `db.statement` is recorded only for executing stored procedures.", "false", "boolean", SettingsData.InstrumentationCategory) }),
            new(new[] { "GRAPHQL" }, new[] { new InstrumentedComponent("GraphQL", "7.5.0 and higher") }, "Not supported on .NET Framework", "beta", "third-party", Array.Empty<Dependency>(), new SignalsList[] { new TracesList() }, new Setting[] { new("OTEL_DOTNET_AUTO_GRAPHQL_SET_DOCUMENT", "Whether the GraphQL instrumentation can pass raw queries through the `graphql.document` attribute. Queries might contain sensitive information.", "false", "boolean", SettingsData.InstrumentationCategory) }),
            new(new[] { "GRPCNETCLIENT" }, new[] { new InstrumentedComponent("Grpc.Net.Client", "2.52.0 to 3.0.0") }, null, "beta", "community", new[] { new Dependency("Grpc.Net.Client Instrumentation for OpenTelemetry", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.GrpcNetClient", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.GrpcNetClient", "1.11.0-beta.2", "beta") }, new SignalsList[] { new TracesList() }, new Setting[] { new("OTEL_DOTNET_AUTO_TRACES_GRPCNETCLIENT_INSTRUMENTATION_CAPTURE_REQUEST_METADATA", "A comma-separated list of gRPC metadata names. Grpc.Net.Client instrumentations will capture gRPC request metadata values for all configured metadata names.", string.Empty, "string", SettingsData.InstrumentationCategory), new("OTEL_DOTNET_AUTO_TRACES_GRPCNETCLIENT_INSTRUMENTATION_CAPTURE_RESPONSE_METADATA", "A comma-separated list of gRPC metadata names. Grpc.Net.Client instrumentations will capture gRPC response metadata values for all configured metadata names.", string.Empty, "string", SettingsData.InstrumentationCategory) }),
            new(new[] { "HTTPCLIENT" }, new[] { new InstrumentedComponent("System.Net.Http.HttpClient", "See general requirements"),  new InstrumentedComponent("System.Net.HttpWebRequest", "See general requirements") }, null, "beta", "community", new[] { new Dependency("HttpClient and HttpWebRequest instrumentation for OpenTelemetry", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Http", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Http", "1.11.1", "beta") }, new SignalsList[] { new TracesList(), new MetricList(httpClientMetrics) }, new Setting[] { new("OTEL_DOTNET_AUTO_TRACES_HTTP_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS", "A comma-separated list of HTTP header names. HTTP Client instrumentations will capture HTTP request header values for all configured header names.", string.Empty, "string", SettingsData.InstrumentationCategory), new("OTEL_DOTNET_AUTO_TRACES_HTTP_INSTRUMENTATION_CAPTURE_RESPONSE_HEADERS", "A comma-separated list of HTTP header names. HTTP Client instrumentations will capture HTTP response header values for all configured header names.", string.Empty, "string", SettingsData.InstrumentationCategory), new("OTEL_DOTNET_EXPERIMENTAL_HTTPCLIENT_DISABLE_URL_QUERY_REDACTION", "Whether the HTTP client instrumentation turns off redaction of the `url.full` attribute value.", "false", "boolean", SettingsData.InstrumentationCategory) }),
            new("KAFKA", new InstrumentedComponent("Confluent.Kafka", "ARM64: 1.8.2 to 3.0.0. Other platforms: 1.4.0 to 3.0.0"), null, "beta", "community", new TracesList()),
            new("MASSTRANSIT", new InstrumentedComponent("MassTransit", "8.0.0 and higher"), "Not supported on .NET Framework", "beta", "third-party", new TracesList()),
            new(new[] { "MONGODB" }, new[] { new InstrumentedComponent("MongoDB.Driver.Core", "2.7.0 to 3.0.0"), new InstrumentedComponent("MongoDB.Driver.Core", "3.0.0 to 4.0.0") }, "Not supported on .NET Framework", "beta", "third-party", Array.Empty<Dependency>(), new SignalsList[] { new TracesList() }, Array.Empty<Setting>()),
            new("MYSQLCONNECTOR", new InstrumentedComponent("MySqlConnector", "2.0.0 and higher"), null, "beta", "third-party", new TracesList()),
            new("MYSQLDATA", new InstrumentedComponent("MySql.Data", "8.1.0 and higher"), "Not supported on .NET Framework", "beta", "third-party", new TracesList()),
            new("NPGSQL", new InstrumentedComponent("Npgsql", "6.0.0 and higher"), null, "beta", "third-party", new TracesList()),
            new(new[] { "ORACLEMDA" }, new[] { new InstrumentedComponent("Oracle.ManagedDataAccess.Core", "23.4.0 and higher"), new InstrumentedComponent("Oracle.ManagedDataAccess", "23.4.0 and higher") }, "Not supported on ARM64", "beta", "third-party",  Array.Empty<Dependency>(), new SignalsList[] { new TracesList() }, new Setting[] { new("OTEL_DOTNET_AUTO_ORACLEMDA_SET_DBSTATEMENT_FOR_TEXT", "Whether the Oracle Client instrumentation can pass SQL statements through the `db.statement` attribute. Queries might contain sensitive information. If set to `false`, `db.statement` is recorded only for executing stored procedures.", "false", "boolean", SettingsData.InstrumentationCategory) }),
            new(new[] { "NSERVICEBUS" }, new[] { new InstrumentedComponent("NServiceBus", "8.0.0 to 10.0.0") }, null, "beta", "third-party", Array.Empty<Dependency>(), new SignalsList[] { new TracesList(), new MetricList(new MetricData("nservicebus.messaging.successes", counter, "Number of messages successfully processed by the endpoint."), new MetricData("nservicebus.messaging.fetches", counter, "Number of messages retrieved from the queue by the endpoint."), new MetricData("nservicebus.messaging.failures", counter, "Number of messages unsuccessfully processed by the endpoint.")) }, Array.Empty<Setting>()),
            new("QUARTZ", new InstrumentedComponent("Quartz", "3.4.0 and higher"), "Not supported on .NET Framework 4.7.1 and lower", "beta", "community", new Dependency("QuartzNET Instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Quartz", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Quartz", "1.11.0-beta.2", "beta"), new SignalsList[] { new TracesList() }),
            new("RABBITMQ", new InstrumentedComponent("RabbitMQ.Client", "6.0.0 and higher"), null, "beta", "third-party", new TracesList()),
            new(new[] { "SQLCLIENT" }, new[] { new InstrumentedComponent("Microsoft.Data.SqlClient", "v3.* is not supported on .NET Framework"), new InstrumentedComponent("System.Data.SqlClient", "4.8.5 and higher"), new InstrumentedComponent("System.Data", "Shipped with .NET Framework") }, null, "beta", "community", new Dependency[] { new Dependency("SqlClient instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.SqlClient", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.SqlClient", "1.11.0-beta.2", "beta") }, new SignalsList[] { new TracesList() },  new Setting[] { new("OTEL_DOTNET_AUTO_SQLCLIENT_SET_DBSTATEMENT_FOR_TEXT", "Whether the SQL Client instrumentation can pass SQL statements through the `db.statement` attribute. Queries might contain sensitive information. If set to `false`, `db.statement` is recorded only for executing stored procedures. Not supported on .NET Framework for System.Data.SqlClient.", "false", "boolean", SettingsData.InstrumentationCategory) }),
            new("STACKEXCHANGEREDIS", new InstrumentedComponent("StackExchange.Redis", "2.6.122 to 3.0.0"), "Not supported on .NET Framework", "beta", "community", new Dependency("StackExchange.Redis Instrumentation for OpenTelemetry", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.StackExchangeRedis", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.StackExchangeRedis", "1.11.0-beta.2", "beta"), new SignalsList[] { new TracesList() }),
            new(new[] { "WCFCLIENT", "WCFSERVICE" }, new[] { new InstrumentedComponent("System.ServiceModel", "4.7.0 and higher of `System.ServiceModel.Primitives`") }, "Service side not supported on .NET. `WCFCLIENT` for client side instrumentation and `WCFSERVICE` for service side instrumentation", "beta", "community", new[] { new Dependency("WCF Instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Wcf", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Wcf", "1.11.0-beta.2", "beta") }, new SignalsList[] { new TracesList() }, Array.Empty<Setting>()),
            new("NETRUNTIME", new InstrumentedComponent(".NET runtime", null), null, "beta", "community", new Dependency("Runtime Instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Runtime", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Runtime", "1.11.1", "stable"), new SignalsList[] { new MetricList(netRuntimeMetrics) }),
            new("PROCESS", new InstrumentedComponent("Process", null), null, "beta", "community", new Dependency("Process Instrumentation for OpenTelemetry .NET", "https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Process", "https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Process", "1.11.0-beta.2", "beta"), new SignalsList[]
            {
                new MetricList(
                    new MetricData("process.memory.usage", upDownCounter, "The amount of physical memory allocated for this process.", conditionallyBundled),
                    new("process.memory.virtual", upDownCounter, "The amount of committed virtual memory for this process.", conditionallyBundled),
                    new MetricData("process.cpu.time", counter, "Total CPU seconds broken down by different states, such as user and system.", conditionallyBundled),
                    new("process.cpu.count", upDownCounter, "Total CPU seconds broken down by different states, such as user and system.", conditionallyBundled),
                    new("process.threads", upDownCounter, "Process threads count.", conditionallyBundled))
            }),
            new("ILOGGER", new InstrumentedComponent("Microsoft.Extensions.Logging", "9.0.0 and higher"), "Not supported on .NET Framework", "beta", "community", new LogLists()),
            new(new[] { "LOG4NET" }, new[] { new InstrumentedComponent("log4net", "2.0.13 to 4.0.0") }, null, "beta", "community", Array.Empty<Dependency>(), new SignalsList[] { new LogLists() }, new[] { new Setting("OTEL_DOTNET_AUTO_LOGS_ENABLE_LOG4NET_BRIDGE", "Enables `log4net` bridge. When `log4net` logs bridge is enabled, and `log4net` is configured with at least 1 appender, application logs are exported in OTLP format in addition to being written into their currently configured destination.", "false", "boolean", SettingsData.InstrumentationCategory) })
        };

        return instrumentations;
    }
}
