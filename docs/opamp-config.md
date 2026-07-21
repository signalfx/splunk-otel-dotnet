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

## Configuration

### Environment variables

See the [docs](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v1.16.0/docs/config.md#opamp-client)
for more details.

### File-based configuration

See the [docs](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v1.16.0/docs/file-based-configuration.md#opamp)
for more details.

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

Reported OTLP endpoints preserve the URI scheme, host, effective port, and
normalized path.
User information, query, and fragment components are removed to avoid
disclosing credentials or other sensitive data embedded in the URL.
Endpoint hosts and paths are not redacted and should not contain secrets.
Distinct active endpoints that differ only in redacted components remain
separate exporter entries, although their reported endpoint values are
identical.

An endpoint that cannot be represented as an absolute HTTP or HTTPS URI is
treated as an effective-configuration resolution failure. The plugin does not
emit a placeholder or a partial endpoint set. A full-state report can still be
sent without the effective-configuration section.

Effective configuration file content is bounded to 512 KiB of UTF-8 data. The
limit applies to the serialized file body. OpAMP file-name and content-type
fields, protobuf framing, and other request fields are outside it; values also
present in the serialized body, such as `otel_config_file`, count as file
content. The plugin does not impose additional fixed length or count caps on
file names or OTLP endpoints.

For file-based configuration, all actively used OTLP endpoints represented by
the effective-config schema are reported when the complete serialized body fits
within the budget. The environment representation supports at most one active
endpoint per signal. Multiple endpoints for a signal therefore cause
effective-configuration resolution to fail. Content that exceeds the limit is
rejected rather than truncated; in either failure case, a full-state report can
still be sent without effective configuration.
