# Correlating logs with traces

Use the trace metadata to correlate traces with log events.

## Automatic correlation

> **NOTE**
> Automatic correlation currently works only for .NET applications.

By default, if application uses `Microsoft.Extensions.Logging` for logging,
additional [`LoggingProvider`](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging-providers)
that exports logs in `OTLP` format to a local collector will be added.
Exported logs contain associated trace context.

## Manual correlation

You can configure logging libraries to include tracing attributes in logs written
to existing logs destination.

### Serilog

You can use one of available enrichers, e.g [`Serilog.Enrichers.Span`](https://www.nuget.org/packages/Serilog.Enrichers.Span)
or create your own [enricher](https://github.com/serilog/serilog/wiki/Enrichment)
to add trace context as properties to log events.

### NLog

You can simply use [`NLog.DiagnosticSource`](https://www.nuget.org/packages/NLog.DiagnosticSource).
([documentation](https://github.com/NLog/NLog.DiagnosticSource)).
