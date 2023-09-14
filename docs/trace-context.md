# Correlating logs with traces

Use the trace metadata to correlate traces with log events.

## Automatic correlation

By default, if application uses `Microsoft.Extensions.Logging` for logging,
additional [`LoggingProvider`](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging-providers)
that exports logs in `OTLP` format to a local collector will be added.
Exported logs contain associated trace context.

## Manual correlation

You can configure logging libraries to include tracing attributes in logs written
to existing logs destination.

### Serilog

`Serilog` log events can be enriched with trace context.
There are multiple ways to achieve that.

#### Use existing enricher

You can use one of available enrichers, e.g [`Serilog.Enrichers.Span`](https://www.nuget.org/packages/Serilog.Enrichers.Span)

In order to have trace context injected into logs:

- add package reference to `Serilog.Enrichers.Span`:

```sh
dotnet add package Serilog.Enrichers.Span
```

- add enricher and configure it`s options as required:

```c#
Log.Logger = new LoggerConfiguration()
            .Enrich.WithSpan(new SpanOptions { IncludeTraceFlags = true }) // add enricher
            .WriteTo.File(
                new JsonFormatter(renderMessage: true),
                "log.txt",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true)
            .CreateLogger();
```

- if events are recorded in `json` format, e.g by using `JsonFormatter`,
no additional changes are needed.

Your logs will now have trace context injected into them:

```json
{"Timestamp":"2023-09-14T16:37:45.9098509+02:00","Level":"Information","MessageTemplate":"Logged inside activity","RenderedMessage":"Logged inside activity","Properties":{"TraceFlags":"Recorded","SpanId":"3649cecf468d3ac6","TraceId":"91ea1932714ca3d0f9a697453e9e83b2","ParentId":"0000000000000000"}}
```

- if events are recorded in plain text format, adjust output template
to include additional properties provided by enricher, e.g:

```c#
Log.Logger = new LoggerConfiguration()
            .Enrich.WithSpan(new SpanOptions { IncludeTraceFlags = true }) // add enricher
            .WriteTo.File(
                "log.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}|TraceId={TraceId}|SpanId={SpanId}|TraceFlags={TraceFlags}",
                rollOnFileSizeLimit: true)
            .CreateLogger();
```

Your logs will now have trace context injected into them:

```text
[16:38:50 INF] Logged inside activity
|TraceId=4f624fb18be91c18cd6e2a762896dfc6|SpanId=69366bf7fb7cf68b|TraceFlags=Recorded
```

#### Create custom enricher

As an alternative, you can create your own [enricher](https://github.com/serilog/serilog/wiki/Enrichment)
to add trace context as properties to log events.

- implement enricher that adds trace context properties, e.g:

> **Note**
> Implementation below is simple and nonefficient

```c#
private class TestEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        logEvent.AddPropertyIfAbsent(new LogEventProperty("SpanId", new ScalarValue(activity?.SpanId)));
        logEvent.AddPropertyIfAbsent(new LogEventProperty("TraceId", new ScalarValue(activity?.TraceId)));
        logEvent.AddPropertyIfAbsent(new LogEventProperty("TraceFlags", new ScalarValue(activity?.ActivityTraceFlags)));
    }
}
```

- add enricher when configuring logger:

```c#
new LoggerConfiguration()
            .Enrich.With<TestEnricher>() // add custom enricher
            .WriteTo.File(
                new JsonFormatter(renderMessage: true), // add JsonFormatter
                "log.txt",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true)
            .CreateLogger();
```

- if events are recorded in `json` format, e.g by using `JsonFormatter`,
no additional changes are needed.

Your logs will now have trace context injected into them:

```json
{"Timestamp":"2023-09-14T16:44:30.0349993+02:00","Level":"Information","MessageTemplate":"Logged inside activity","RenderedMessage":"Logged inside activity","Properties":{"SpanId":"cc54857885557cd4","TraceId":"821dceafbef764cc91efc21d04f48927","TraceFlags":"Recorded"}}
```

- if events are recorded in plain text format, adjust output template to include
additional properties provided by enricher, e.g:

```c#
Log.Logger = new LoggerConfiguration()
            .Enrich.With<TestEnricher>()
            .WriteTo.File(
                "log.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}|TraceId={TraceId}|SpanId={SpanId}|TraceFlags={TraceFlags}",
                rollOnFileSizeLimit: true)
            .CreateLogger();
```

Your logs will now have trace context injected into them:

```text
[16:42:47 INF] Logged inside activity
|TraceId=de368caa17c4b63154cb8eae99f0db84|SpanId=d2343289a8628728|TraceFlags=Recorded
```

### NLog

In order to have trace context injected into logs:

- add package reference to `NLog.DiagnosticSource`:

```sh
dotnet add package NLog.DiagnosticSource
```

- add extension to your `nlog.config`:

```xml
<extensions>
    <add assembly="NLog.DiagnosticSource"/>
</extensions>
```

- adjust layout of your target to use activity related properties:

```xml
<target xsi:type="File" name="logfile" fileName="c:\temp\console-example.log"
                layout="${longdate}|${level}|${message}|TraceId=${activity:property=TraceId}|SpanId=${activity:property=SpanId}|ParentId=${activity:property=ParentId}|${all-event-properties} ${exception:format=tostring}" />
```

Your logs will now have trace context injected into them:

```text
2023-09-14 16:53:25.9139|Info|Logged inside activity|TraceId=23276df3a4a54414d196b88d71338806|SpanId=6e20050fc23d9a2a|TraceFlags=Recorded|  
```
