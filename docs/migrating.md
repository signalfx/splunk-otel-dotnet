# Migrating

## Configuration

This section outlines configuration settings available in
[SignalFx Instrumentation for .NET](https://github.com/signalfx/signalfx-dotnet-tracing/),
and their equivalents in
Splunk distribution of OpenTelemetry .NET.

## Main settings

The following settings are common to most instrumentation scenarios:

| Setting                   | Description                                                                           | OpenTelemetry equivalent                       |
|---------------------------|---------------------------------------------------------------------------------------|------------------------------------------------|
| `SIGNALFX_ENV`            | The value for the `deployment.environment` tag added to every span.                   | Use `OTEL_RESOURCE_ATTRIBUTES` |
| `SIGNALFX_SERVICE_NAME`   | The name of the application or service.                                               | `OTEL_SERVICE_NAME`                            |
| `SIGNALFX_VERSION`        | The version of the application. When set, it populates the `version` tag on spans.    |  Use `OTEL_RESOURCE_ATTRIBUTES` |

To set `version` tag on spans to `1.0.0`, configure environment variables:

- `SIGNALFX_VERSION=1.0.0` for SignalFx Instrumentation for .NET
- `OTEL_RESOURCE_ATTRIBUTES=version=1.0.0` for
Splunk distribution of OpenTelemetry .NET

## Global management settings

| Setting                               | Description                                                                                                                                          | SignalFx default                                           | OpenTelemetry equivalent                                                                                   |
|---------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------|------------------------------------------------------------------------------------------------------------|
| `SIGNALFX_AZURE_APP_SERVICES`         | Set to indicate that the profiler is running in the context of Azure App Services.                                                                   | `false`                                           | none                                                                                                       |
| `SIGNALFX_DOTNET_TRACER_HOME`         | Installation location. Must be set manually to `/opt/signalfx` when instrumenting applications on Linux or background services in Azure App Service. | _Automatically set ONLY by the Windows installer_ | `OTEL_DOTNET_AUTO_HOME` (on Linux set to `$HOME/.splunk-otel-dotnet`)                                      |
| `SIGNALFX_PROFILER_EXCLUDE_PROCESSES` | Sets the filename of executables the profiler cannot attach to. Supports multiple semicolon-separated values, for example: `MyApp.exe;dotnet.exe`    |                                                   | `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES`                                                                       |
| `SIGNALFX_PROFILER_PROCESSES`         | Sets the filename of executables the profiler can attach to. Supports multiple semicolon-separated values, for example: `MyApp.exe;dotnet.exe`       |                                                   | none                                                                                                       |
| `SIGNALFX_TRACE_CONFIG_FILE`          | Path of the JSON configuration file.                                                                                                                 |                                                   | none                                                                                                       |
| `SIGNALFX_TRACE_ENABLED`              | Enable to activate the tracer.                                                                                                                       | `true`                                            | none                                                                                                       |
| `SIGNALFX_METRICS_{0}_ENABLED`        | Configuration pattern for enabling or disabling a specific group of metrics.                                                                         | `false`                                           | `OTEL_DOTNET_AUTO_METRICS_ENABLED_INSTRUMENTATIONS`/ `OTEL_DOTNET_AUTO_METRICS_DISABLED_INSTRUMENTATIONS`  |

## Global instrumentation settings

| Setting                              | Description                                                                                                                                                                                                                                | SignalFx default   | OpenTelemetry equivalent                                                                                              |
|--------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------|-----------------------------------------------------------------------------------------------------------------------|
| `SIGNALFX_DISABLED_INTEGRATIONS`     | Comma-separated list of disabled library instrumentations.                                                                                                                                                                                 |           | `OTEL_DOTNET_AUTO_TRACES_DISABLED_INSTRUMENTATIONS` (for the list of supported instrumentations, see [here](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/docs/config.md#instrumentations))                                                                  |
| `SIGNALFX_RECORDED_VALUE_MAX_LENGTH` | The maximum length an attribute value can have. Values longer than this are truncated. Values are discarded entirely when set to 0, and ignored when set to a negative value.                                                              | `12000`   | `OTEL_SPAN_ATTRIBUTE_VALUE_LENGTH_LIMIT`                                                                              |
| `SIGNALFX_GLOBAL_TAGS`               | Comma-separated list of key-value pairs that specify global tags added to all telemetry signals. For example: `"key1:val1,key2:val2"`. If unset, the value of the `SIGNALFX_TRACE_GLOBAL_TAGS` environment variable is used, if available. |           | `OTEL_RESOURCE_ATTRIBUTES`                                                                                            |
| `SIGNALFX_TRACE_{0}_ENABLED`         | Configuration pattern for enabling or disabling a specific library instrumentation. For example, in order to disable Kafka instrumentation, set `SIGNALFX_TRACE_Kafka_ENABLED=false`                                                       | `true`    | `OTEL_DOTNET_AUTO_TRACES_DISABLED_INSTRUMENTATIONS` (e.g `OTEL_DOTNET_AUTO_TRACES_DISABLED_INSTRUMENTATIONS`=MongoDB) |

## Exporter settings

Use following settings to configure where and how the telemetry data is being exported.

| Setting                                  | Description                                                                                                                                                                                           | SignalFx default                              | OpenTelemetry equivalent                                            |
|------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------|---------------------------------------------------------------------|
| `SIGNALFX_ACCESS_TOKEN`                  | Splunk Observability Cloud access token for your organization. It enables sending telemetry directly to the Splunk Observability Cloud ingest endpoint.                                               |                                      | `SPLUNK_ACCESS_TOKEN`                                               |
| `SIGNALFX_REALM`                         | Your Splunk Observability Cloud realm. To find your realm, open Splunk Observability Cloud, click Settings, then click on your username.                                                              | `none` (local collector)             | `SPLUNK_REALM`                                                      |
| `SIGNALFX_ENDPOINT_URL`                  | The URL to where trace exporters send traces. Overrides `SIGNALFX_REALM` configuration for the traces ingestion endpoint.                                                                             | `http://localhost:9411/api/v2/spans` | `OTEL_EXPORTER_OTLP_ENDPOINT` |
| `SIGNALFX_METRICS_ENDPOINT_URL`          | The URL to where metric exporters send metrics. Overrides `SIGNALFX_REALM` configuration for the metrics ingestion endpoint.                                                                          | `http://localhost:9943/v2/datapoint` | `OTEL_EXPORTER_OTLP_ENDPOINT` |
| `SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED`   | Enable to export traces that contain a minimum number of closed spans, as defined by `SIGNALFX_TRACE_PARTIAL_FLUSH_MIN_SPANS`.                                                                        | `false`                              | none                                                                |
| `SIGNALFX_TRACE_PARTIAL_FLUSH_MIN_SPANS` | Minimum number of closed spans in a trace before it's exported. The default value is ``500``. Requires the value of the ``SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED`` environment variable to be ``true``. | `500`                                | none                                                                |
| `SIGNALFX_TRACE_BUFFER_SIZE`             | The size of the trace exporter buffer, expressed as the number of traces.                                                                                                                             | `1000`                               | `OTEL_BSP_MAX_QUEUE_SIZE` (default: 2048)                           |

## Trace propagation settings

| Setting                | Description                                                                                                                                                   | SignalFx default   | OpenTelemetry equivalent                                                                               |
|------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------|------------------------------------------------------------------------------------|
| `SIGNALFX_PROPAGATORS` | Comma-separated list of the propagators for the tracer. Available propagators are: `B3`, `W3C`. The Tracer will try to execute extraction in the given order. | `B3,W3C`  | `OTEL_PROPAGATORS`(default: `tracecontext`, `baggage`, available: `b3multi`, `b3`) |

[OpenTelemetry `OTEL_PROPAGATORS`](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/sdk-environment-variables.md#general-sdk-configuration)
to `SIGNALFX_PROPAGATORS` values mapping:

| `OTEL_PROPAGATORS` value | `SIGNALFX_PROPAGATORS` value |
|--------------------------|------------------------------|
| `b3multi`                | `B3`                         |
| `tracecontext`           | `W3C`                        |

In order to keep same propagators configured after migrating from signalfx-dotnet-instrumentation,
following should be set: `OTEL_PROPAGATORS=b3multi,tracecontext`.

## Library-specific instrumentation settings

| Setting                                                | Description                                                                                                                                                                                                                   | SignalFx default                             | OpenTelemetry equivalent                                 |
|--------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------|----------------------------------------------------------|
| `SIGNALFX_HTTP_CLIENT_ERROR_STATUSES`                  | Comma-separated list of HTTP client response statuses for which the spans are set as errors, for example: `300, 400-499`.                                                                                                     | `400-599`                           | none                                                     |
| `SIGNALFX_HTTP_SERVER_ERROR_STATUSES`                  | Comma-separated list of HTTP server response statuses for which the spans are set as errors, for example: `300, 400-599`.                                                                                                     | `500-599`                           | none                                                     |
| `SIGNALFX_INSTRUMENTATION_ELASTICSEARCH_TAG_QUERIES`   | Enable the tagging of an Elasticsearch command PostData as `db.statement`. It might introduce overhead for direct streaming users.                                                                                            | `true`                              | none                                                     |
| `SIGNALFX_INSTRUMENTATION_MONGODB_TAG_COMMANDS`        | Enable the tagging of a Mongo command BsonDocument as `db.statement`.                                                                                                                                                         | `true`                              | Not configurable using environment variable.             |
| `SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS`          | Enable the tagging of a Redis command as `db.statement`.                                                                                                                                                                      | `true`                              | Not configurable using environment variable.             |
| `SIGNALFX_LOGS_INJECTION`                              | Enable to inject trace IDs, span IDs, service name, and environment into logs.                                                                                                                                                | `false`                             | Logs correlated if `Microsoft.Extensions.Logging` used.  |
| `SIGNALFX_TRACE_DELAY_WCF_INSTRUMENTATION_ENABLED`     | Enable the updated WCF instrumentation that delays execution until later in the WCF pipeline when the WCF server exception handling is established.                                                                           | `false`                             | none                                                     |
| `SIGNALFX_TRACE_HEADER_TAGS`                           | Comma-separated map of HTTP header keys to tag names, automatically applied as tags on traces.                                                                                                                                | `"x-my-header:my-tag,header2:tag2"` | none                                                     |
| `SIGNALFX_TRACE_HTTP_CLIENT_EXCLUDED_URL_SUBSTRINGS`   | Comma-separated list of URL substrings. Matching URLs are ignored by the tracer. For example, `subdomain,xyz,login,download`.                                                                                                 |                                     | Not configurable using environment variable.             |
| `SIGNALFX_TRACE_KAFKA_CREATE_CONSUMER_SCOPE_ENABLED`   | Enable to close consumer scope on method enter, and start a new one on method exit.                                                                                                                                           | `true`                              | none                                                     |
| `SIGNALFX_TRACE_RESPONSE_HEADER_ENABLED`               | Enable to add server trace information to HTTP response headers. It enables [Splunk Real User Monitoring (RUM)](https://docs.splunk.com/Observability/rum/intro-to-rum.html) integration when using ASP.NET and ASP.NET Core. | `true`                              | `SPLUNK_TRACE_RESPONSE_HEADER_ENABLED`                   |
| `SIGNALFX_TRACE_ROUTE_TEMPLATE_RESOURCE_NAMES_ENABLED` | ASP.NET span and resource names are based on routing configuration if applicable.                                                                                                                                             | `true`                              | none (fixed behavior equivalent to setting to `true`)    |
