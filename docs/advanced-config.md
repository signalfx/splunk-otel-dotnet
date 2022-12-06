# Advanced configuration

## OpenTelemetry configuration

See [Open Telemetry Auto Instrumentation documentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v0.4.0-beta.1/docs/config.md)
for configuration details.

## Splunk distribution configuration

## Manual installation

Download and install the latest binaries from
[the latest release](https://github.com/signalfx/splunk-otel-dotnet/releases/latest).

> The path where you place the binaries is referenced as `$INSTALL_DIR` in this documentation.

## Manual instrumentation

When running your application, make sure to:

1. Set the [resources](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v0.4.0-beta.1/docs/config.md#resources).
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

> Some settings can be omitted on .NET (Core). For more information, see the [documentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v0.4.0-beta.1/docs/config.md#net-clr-profiler).

## Splunk plugin settings

Note: .NET Framework apps can read settings also from `Web.config` and `App.config`.

| Environment variable                   | Description                                | Default value |
|----------------------------------------|--------------------------------------------|---------------|
| `SPLUNK_REALM`                         | Specifies direct OTLP ingest realm.        |               |
| `SPLUNK_ACCESS_TOKEN`                  | Specifies direct OTLP ingest access token. |               |
| `SPLUNK_TRACE_RESPONSE_HEADER_ENABLED` | Enables Splunk RUM integration.            | `true`        |
