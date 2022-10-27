# Draft documentation

> The official documentation for this distribution can be found in the
> [Splunk Docs](https://docs.splunk.com/Observability/gdi/get-data-in/application/dotnet/get-started.html)
> site.
> For instructions on how to contribute to the docs, see
> [CONTRIBUTING.md](../CONTRIBUTING.md#documentation).

This Splunk distribution comes with the following defaults:

- [W3C tracecontext](https://www.w3.org/TR/trace-context/) and
  [W3C baggage](https://www.w3.org/TR/baggage/) context propagation.
- OTLP over HTTP exporter configured to send spans to a locally running [Splunk OpenTelemetry
  Collector](https://github.com/signalfx/splunk-otel-collector)

## Usage (Linux and macOS)

Install the Splunk Distribution of OpenTelemetry .NET
and instrument your .NET application using the official shell scripts.

For example:

```sh
curl -sSfL https://raw.githubusercontent.com/signalfx/splunk-otel-dotnet/v0.0.1-alpha.2/splunk-otel-dotnet-install.sh -O
sh ./splunk-otel-dotnet-install.sh
. $HOME/.splunk-otel-dotnet/instrument.sh
OTEL_SERVICE_NAME=myapp OTEL_RESOURCE_ATTRIBUTES=deployment.environment=staging,service.version=1.0.0 dotnet run
```

The [splunk-otel-dotnet-install.sh](../splunk-otel-dotnet-install.sh) script
downloads and installs the distribution.
It has to be invoked only once.
It uses environment variables as parameters:

| Parameter               | Description                                                      | Required | Default value             |
|-------------------------|------------------------------------------------------------------|----------|---------------------------|
| `OTEL_DOTNET_AUTO_HOME` | Location where binaries are to be installed                      | No       | `$HOME/.splunk-otel-dotnet` |
| `OS_TYPE`               | Possible values: `linux-glibc`, `linux-musl`, `macos`, `windows` | No       | *Calculated*              |
| `TMPDIR`                | Temporary directory used when downloading the files              | No       | `$(mktemp -d)`            |
| `VERSION`               | Version to download                                              | No       | `v0.0.1-alpha.2`           |

The [instrument.sh](../instrument.sh) script
enables the instrumentation in the current shell session.
It has to be run before you run a .NET application.
uses environment variables as parameters:

| Parameter               | Description                                                            | Required | Default value             |
|-------------------------|------------------------------------------------------------------------|----------|---------------------------|
| `ENABLE_PROFILING`      | Whether to set the .NET CLR Profiler, possible values: `true`, `false` | No       | `true`                    |
| `OTEL_DOTNET_AUTO_HOME` | Location where binaries are to be installed                            | No       | `$HOME/.splunk-otel-dotnet` |
| `OS_TYPE`               | Possible values: `linux-glibc`, `linux-musl`, `macos`, `windows`       | No       | *Calculated*              |

> On macOS [`coreutils`](https://formulae.brew.sh/formula/coreutils) is required.

## Usage (Windows)

Install the Splunk Distribution of OpenTelemetry .NET
and instrument your .NET application using the official PowerShell script module.

For example:

```powershell
# Download and import the module
$module_url = "https://github.com/signalfx/splunk-otel-dotnet/releases/download/v0.0.1-alpha.2/Splunk.OTel.DotNet.psm1"
$download_path = Join-Path $env:temp "Splunk.OTel.DotNet.psm1"
Invoke-WebRequest -Uri $module_url -OutFile $download_path
Import-Module $download_path

# Install core files
Install-OpenTelemetryCore

# Setup IIS instrumentation
Register-OpenTelemetryForIIS

# Setup your Windows Service instrumentation
Register-OpenTelemetryForWindowsService -WindowsServiceName "MyServiceName" -OTelServiceName "MyServiceDisplayName"

# Setup environment to start instrumentation from the current PowerShell session
Register-OpenTelemetryForCurrentSession -OTelServiceName "MyServiceDisplayName"

# Get current installation location
Get-OpenTelemetryInstallDirectory

# List all available commands
Get-Command -Module OpenTelemetry.DotNet.Auto

# Get command's usage information
Get-Help Install-OpenTelemetryCore -Detailed
```

⚠️ Register for IIS and Windows Service performs a service restart.

## Advanced configuration

For advanced configuration options, refer to
the [Advanced configuration](advanced-config.md) documentation.

## Manual instrumentation

See [manual-instrumentation.md](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v0.4.0-beta.1/docs/manual-instrumentation.md).

## Migrating

If you're currently using the [SignalFx Instrumentation for .NET](https://github.com/signalfx/signalfx-dotnet-tracing)
and want to migrate to the Splunk Distribution of OpenTelemetry .NET,
see the [Migrating](migrating.md) documentation.

## Troubleshooting

For troubleshooting information, see the
[Troubleshooting](troubleshooting.md) documentation.
