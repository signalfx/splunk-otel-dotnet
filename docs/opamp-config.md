# OpAMP

You can configure Splunk Distribution of OpenTelemetry .NET to report effective
configuration using Open Agent Management Protocol (OpAMP) to a configured
OpAMP server.

Effective configuration reporting is enabled when OpAMP is enabled and the
active configuration passes startup validation. Automatic SDK setup is required;
`OTEL_SDK_DISABLED=true` remains supported as a valid no-op SDK configuration.

## Configuration

### OpAMP client

Configure the OpAMP client through the OpenTelemetry .NET auto-instrumentation
OpAMP options:

- [Environment variables](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v1.16.0/docs/config.md#opamp-client)
- [File-based configuration](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v1.16.0/docs/file-based-configuration.md#opamp)

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

When instrumentation is configured without file-based configuration, the OpAMP
effective configuration contains one file named `environment` with content type
`text/plain; format=properties; vendor=splunk; v=1.0.0`.

The properties body contains the final values for:

- `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT`
- `OTEL_EXPORTER_OTLP_METRICS_ENDPOINT`
- `OTEL_EXPORTER_OTLP_LOGS_ENDPOINT`
- `SPLUNK_PROFILER_ENABLED`
- `SPLUNK_PROFILER_MEMORY_ENABLED`
- `SPLUNK_SNAPSHOT_PROFILER_ENABLED`
- `SPLUNK_SNAPSHOT_PROFILER_SAMPLING_INTERVAL`
- `SPLUNK_PROFILER_CALL_STACK_INTERVAL`
- `OTEL_CONFIG_FILE`

For .NET Framework applications, `SPLUNK_PROFILER_MEMORY_ENABLED` is always
reported as `false` because memory profiling is not supported on .NET Framework.

When instrumentation is configured with file-based configuration, the OpAMP
effective configuration contains one file named after the configured YAML file
path with content type `application/yaml; vendor=splunk; v=1.0.0`.

The YAML body is a filtered effective representation of the active configuration:
resolved OTLP endpoints for active providers, plus active Splunk profiling
settings. Environment variable templates and omitted YAML defaults are reported
as their final evaluated values.

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
[.NET CLR Profiler](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v1.16.0/docs/config.md#net-clr-profiler)
is enabled before the process starts. Without the .NET CLR Profiler, profiling
does not run, and remote configuration cannot enable or configure the .NET CLR
Profiler at runtime.

Supported at runtime:

- `always_on.cpu_profiler`: enable or disable CPU profiling.
- `always_on.cpu_profiler.sampling_interval`: update CPU sampling interval.
