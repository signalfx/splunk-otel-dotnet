# Draft documentation

> The official documentation for this distribution can be found in the
> [Splunk Docs](https://docs.splunk.com/Observability/gdi/get-data-in/application/dotnet/get-started.html)
> site.
> For instructions on how to contribute to the docs, see
> [CONTRIBUTING.md](../CONTRIBUTING.md#documentation).

## Getting started

This Splunk distribution comes with the following defaults:

- [W3C tracecontext](https://www.w3.org/TR/trace-context/) and
  [W3C baggage](https://www.w3.org/TR/baggage/) context propagation.
- OTLP over HTTP exporter configured to send spans to a locally running [Splunk OpenTelemetry
  Collector](https://github.com/signalfx/splunk-otel-collector)

### Install

Download and extract the appropriate binaries from
[the latest release](https://github.com/signalfx/splunk-otel-dotnet/releases/latest).

> The path where you put the binaries is referenced as `$INSTALL_DIR`

### Basic configuration

When running your application, make sure to:

1. Set the [resources](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v0.3.1-beta.1/docs/config.md#resources).
1. Set the environment variables from the table below.

| Environment variable                 | .NET version           | Value                                                                          |
|--------------------------------------|------------------------|--------------------------------------------------------------------------------|
| `COR_ENABLE_PROFILING`               | .NET Framework         | `1`                                                                            |
| `COR_PROFILER`                       | .NET Framework         | `{918728DD-259F-4A6A-AC2B-B85E1B658318}`                                       |
| `COR_PROFILER_PATH_32`               | .NET Framework         | `$INSTALL_DIR/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll`            |
| `COR_PROFILER_PATH_64`               | .NET Framework         | `$INSTALL_DIR/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll`            |
| `CORECLR_ENABLE_PROFILING`           | .NET (Core)            | `1`                                                                            |
| `CORECLR_PROFILER`                   | .NET (Core)            | `{918728DD-259F-4A6A-AC2B-B85E1B658318}`                                       |
| `CORECLR_PROFILER_PATH`              | .NET (Core) on Linux   | `$INSTALL_DIR/OpenTelemetry.AutoInstrumentation.Native.so`                     |
| `CORECLR_PROFILER_PATH`              | .NET (Core) on macOS   | `$INSTALL_DIR/OpenTelemetry.AutoInstrumentation.Native.dylib`                  |
| `CORECLR_PROFILER_PATH_32`           | .NET (Core) on Windows | `$INSTALL_DIR/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll`            |
| `CORECLR_PROFILER_PATH_64`           | .NET (Core) on Windows | `$INSTALL_DIR/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll`            |
| `DOTNET_ADDITIONAL_DEPS`             | .NET (Core)            | `$INSTALL_DIR/AdditionalDeps`                                                  |
| `DOTNET_SHARED_STORE`                | .NET (Core)            | `$INSTALL_DIR/store`                                                           |
| `DOTNET_STARTUP_HOOKS`               | .NET (Core)            | `$INSTALL_DIR/netcoreapp3.1/OpenTelemetry.AutoInstrumentation.StartupHook.dll` |
| `OTEL_DOTNET_AUTO_HOME`              | All versions           | `$INSTALL_DIR`                                                                 |
| `OTEL_DOTNET_AUTO_INTEGRATIONS_FILE` | All versions           | `$INSTALL_DIR/integrations.json`                                               |

> Some settings can be omitted on .NET (Core). For more information, see the [documentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v0.3.1-beta.1/docs/config.md#net-clr-profiler).

## Advanced configuration

For advanced configuration options, refer to
the [Advanced configuration](advanced-config.md) documentation.

## Manual instrumentation

See [manual-instrumentation.md](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v0.3.1-beta.1/docs/manual-instrumentation.md).

## Migrating

If you're currently using the [SignalFx Instrumentation for .NET](https://github.com/signalfx/signalfx-dotnet-tracing)
and want to migrate to the Splunk Distribution of OpenTelemetry .NET,
see the [Migrating](migrating.md) documentation.

## Troubleshooting

For troubleshooting information, see the
[Troubleshooting](troubleshooting.md) documentation.
