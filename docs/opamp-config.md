# OpAMP

You can configure Splunk Distribution of OpenTelemetry .NET to report effective
configuration using Open Agent Management Protocol (OpAMP) to a configured
OpAMP server.

Effective configuration reporting is enabled when OpAMP is enabled.

## Configuration

### OpAMP client

Configure the OpAMP client through the OpenTelemetry .NET auto-instrumentation
OpAMP options:

- [Environment variables](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v1.16.0-beta.1/docs/config.md#opamp-client)
- [File-based configuration](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v1.16.0-beta.1/docs/file-based-configuration.md#opamp)

### Remote configuration

Remote configuration is a Splunk OpAMP feature flag. Configure it through:

- [Advanced Configuration](advanced-config.md) for the
  `SPLUNK_OPAMP_REMOTE_CONFIG` environment variable.
- [File-based Configuration](file-based-configuration.md) for
  `opamp/development.features.remote_config`.

Remote configuration is opt-in. When OpAMP and the remote configuration feature
are enabled, the distribution advertises `AcceptsRemoteConfig` and
`ReportsRemoteConfig` and listens for an OpAMP
`AgentRemoteConfig.AgentConfigMap` entry named `splunk.remote.config` with
content type `application/yaml`.

The distribution reports remote configuration status for each new
`AgentRemoteConfig.config_hash`. It sends `Applying` before processing the
payload, then `Applied` after the payload is applied or `Failed` with an error
message when the payload cannot be applied.

## Effective configuration contents

When enabled, the OpAMP configuration includes the values for the following settings:

- `SPLUNK_PROFILER_ENABLED`
- `SPLUNK_PROFILER_MEMORY_ENABLED`
- `SPLUNK_SNAPSHOT_PROFILER_ENABLED`
- `SPLUNK_SNAPSHOT_PROFILER_SAMPLING_INTERVAL`
- `SPLUNK_PROFILER_CALL_STACK_INTERVAL`
- `OTEL_EXPORTER_OTLP_TRACES_ENDPOINTS`
- `OTEL_EXPORTER_OTLP_METRICS_ENDPOINTS`
- `OTEL_EXPORTER_OTLP_LOGS_ENDPOINTS`
- `OTEL_SERVICE_NAME`

For .NET Framework applications, `SPLUNK_PROFILER_MEMORY_ENABLED` is always
reported as `false` because memory profiling is not supported on .NET Framework.

After a remote configuration is applied, subsequent OpAMP effective
configuration reports reflect the active CPU profiler runtime state.

## Remote configuration payload

```yaml
distribution:
  splunk:
    profiling:
      always_on:
        cpu_profiler:
          sampling_interval: 10000
```

## Runtime behavior

Profiling remote configuration only works when the
[.NET CLR Profiler](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v1.16.0-beta.1/docs/config.md#net-clr-profiler)
is enabled before the process starts. Without the .NET CLR Profiler, profiling
does not run, and remote configuration cannot enable or configure the .NET CLR
Profiler at runtime.

Supported at runtime:

- `always_on.cpu_profiler`: enable or disable CPU profiling.
- `always_on.cpu_profiler.sampling_interval`: update CPU sampling interval.
