# OpAMP

You can configure Splunk Distribution of OpenTelemetry .NET to report effective
configuration using Open Agent Management Protocol (OpAMP) to a configured
OpAMP server.

Effective configuration reporting is enabled when OpAMP is enabled.

## Configuration

### OpAMP

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
are enabled, the distribution advertises `AcceptsRemoteConfig` and listens for
an OpAMP `AgentRemoteConfig.AgentConfigMap` entry named `splunk.remote.config`
with content type `application/yaml`.

Remote configuration values are applied in memory only. The agent does not
write remote configuration payloads to disk.

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

## Remote configuration payload

```yaml
distribution:
  splunk:
    profiling:
      always_on:
        cpu_profiler:
          sampling_interval: 10000
        memory_profiler:
          max_memory_samples: 200
      callgraphs:
        sampling_interval: 40
        selection_probability: 0.01
        high_resolution_timer_enabled: false
```

Only `distribution.splunk.profiling` is used. Unknown keys and unsupported
sections are ignored.

## Runtime behavior

Profiling remote configuration only works when the
[.NET CLR Profiler](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v1.16.0-beta.1/docs/config.md#net-clr-profiler)
is enabled before the process starts. Without the .NET CLR Profiler, profiling
does not run, and remote configuration cannot enable or configure the .NET CLR
Profiler at runtime.

The presence of a profiler section enables that profiler. Omitting the section
disables it.

Supported at runtime:

- `always_on.cpu_profiler`: enable or disable CPU profiling.
- `always_on.cpu_profiler.sampling_interval`: update CPU sampling interval.
- `always_on.memory_profiler`: enable or disable memory allocation profiling on
  .NET.
- `always_on.memory_profiler.max_memory_samples`: update maximum memory samples
  per minute on .NET.
- `callgraphs`: enable or disable snapshot profiling.
- `callgraphs.sampling_interval`: update snapshot sampling interval.
- `callgraphs.selection_probability`: update snapshot selection probability.

Ignored:

- `callgraphs.high_resolution_timer_enabled`, because changing the Windows
  high-resolution timer requires process startup/shutdown handling.
- Profiling exporter settings such as endpoint, export interval, and timeout.
- Non-profiling configuration such as realm, access token, trace response
  header settings, OTLP exporter settings, and instrumentation settings.
- Memory allocation profiling on .NET Framework, because it is not supported by
  the profiler runtime.

After a remote configuration is applied, subsequent OpAMP effective
configuration reports reflect the active runtime profiling state.
