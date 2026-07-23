# OpAMP

You can configure Splunk Distribution of OpenTelemetry .NET to report effective
configuration using Open Agent Management Protocol (OpAMP) to a configured
OpAMP server.

Effective configuration reporting is enabled when OpAMP is enabled and the
active configuration passes startup validation. Automatic SDK setup is required;
`OTEL_SDK_DISABLED=true` remains supported as a valid no-op SDK configuration.

After automatic instrumentation initializes, the plugin sends one full-state
report. When effective configuration reporting is enabled, that report contains
the current effective configuration together with the agent description,
capabilities, and health supplied by the OpAMP client. Later server requests
produce a fresh full-state report.

## OpAMP client configuration

Configure the OpAMP client through the OpenTelemetry .NET auto-instrumentation
OpAMP options:

- [Environment variables](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v1.16.0/docs/config.md#opamp-client)
- [File-based configuration](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v1.16.0/docs/file-based-configuration.md#opamp)

## Effective configuration

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

Endpoint hosts and paths are not redacted and must not contain secrets.

Endpoints that cannot be represented as absolute HTTP or HTTPS URIs cause
effective-configuration resolution to fail. In this case, the agent omits the
effective-configuration section but may still send the full-state report.

The serialized effective configuration file is limited to 4 MiB. Content that
exceeds this limit is rejected rather than truncated.

File-based configuration may report multiple active OTLP endpoints per signal.
The environment representation supports only one endpoint per signal; multiple
active endpoints therefore cause effective-configuration resolution to fail.

## Remote configuration

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

After a remote configuration is applied, subsequent OpAMP effective
configuration reports reflect the active CPU profiler runtime state.

### Remote configuration payload

```yaml
distribution:
  splunk:
    profiling:
      always_on:
        cpu_profiler:
          sampling_interval: 10000
```

### Runtime behavior

Profiling remote configuration only works when the
[.NET CLR Profiler](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v1.16.0/docs/config.md#net-clr-profiler)
is enabled before the process starts. Without the .NET CLR Profiler, profiling
does not run, and remote configuration cannot enable or configure the .NET CLR
Profiler at runtime.

Enabling remote configuration initializes the continuous profiling pipeline at
startup so that CPU profiling can be enabled later at runtime. This setup occurs
even when CPU profiling is initially disabled and includes the
`PprofInOtlpLogsExporter`, the upstream `BufferProcessor`, and its background
export thread. On .NET Framework, it also starts the profiler canary thread.
Native CPU sample collection remains disabled until CPU profiling is enabled,
but the initialized pipeline can still add CPU, memory, and thread overhead.
Leave remote configuration disabled when runtime profiler activation is not
required.

Supported at runtime:

- `always_on.cpu_profiler`: enable or disable CPU profiling.
- `always_on.cpu_profiler.sampling_interval`: update CPU sampling interval.
