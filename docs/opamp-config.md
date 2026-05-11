# OpAMP

You can configure Splunk Distribution of OpenTelemetry .NET to report effective
configuration using Open Agent Management Protocol (OpAMP) to a configured
OpAMP server.

Effective configuration reporting is enabled when OpAMP is enabled.

## Configuration

### Environment variables

See the [docs](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v1.16.0-beta.1/docs/config.md#opamp-client)
for more details.

### File-based configuration

See the [docs](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v1.16.0-beta.1/docs/file-based-configuration.md#opamp)
for more details.

## Effective configuration contents

When enabled, the OpAMP configuration includes the values for the following settings:

- `SPLUNK_PROFILER_ENABLED`
- `SPLUNK_PROFILER_MEMORY_ENABLED`
- `SPLUNK_SNAPSHOT_PROFILER_ENABLED`
- `SPLUNK_SNAPSHOT_PROFILER_SAMPLING_INTERVAL`
- `SPLUNK_PROFILER_CALL_STACK_INTERVAL`
- `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT`
- `OTEL_EXPORTER_OTLP_METRICS_ENDPOINT`
- `OTEL_EXPORTER_OTLP_LOGS_ENDPOINT`
- `OTEL_SERVICE_NAME`
