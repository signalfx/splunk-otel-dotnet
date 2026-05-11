# OpAmp

You can configure Splunk Distribution of OpenTelemetry .NET to report effective configuration
using Open Agent Management Protocol (OpAMP) to a configured OpAMP server.

## Configuration

### Environment variables

This feature can be enabled by setting the `OTEL_DOTNET_AUTO_OPAMP_ENABLED` environment variable to `true`
and configuring the `OTEL_DOTNET_AUTO_OPAMP_SERVER_URL` environment variable with the OpAMP server endpoint.

### File-based configuration

This feature can be enabled by setting the `opamp` section in the YAML configuration file.
See the [file-based configuration documentation](./file-based-configuration.md) for more details on how to configure
the instrumentation using the YAML file.

```yaml
opamp/development:
  # Configure the server endpoint. If not explicitly set, a default
  # URL is used: https://localhost:4318/v1/opamp.
  server_url: https://localhost:4318/v1/opamp
```

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
