# Agents

## Context

- This repo builds a Splunk plugin/distribution layer on top of
OpenTelemetry .NET automatic instrumentation.
- Main code lives in one plugin assembly that configures Splunk defaults, profiling,
snapshots and resource attributes.
- Full validation downloads/unpacks the upstream auto-instrumentation distribution,
copies Splunk plugin binaries into it, then runs unit and integration tests.

## Commands

| Command | Purpose |
|---|---|
| `.\build.cmd Workflow` | Full CI-like build: clean, restore, unpack upstream distribution, compile, test, and package |
| `.\build.cmd NuGetWorkflow` | Build and test the NuGet package workflow |
| `dotnet test test\Splunk.OpenTelemetry.AutoInstrumentation.Tests\Splunk.OpenTelemetry.AutoInstrumentation.Tests.csproj --framework net8.0 --no-restore -v minimal` | Focused unit test pass on `net8.0` |
| `dotnet test test\Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests\Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.csproj --framework net8.0 --filter "Category!=NuGetPackage" --no-restore -v minimal` | Focused integration test pass after distribution is prepared |
| `dotnet format .\Splunk.OpenTelemetry.AutoInstrumentation.slnx --no-restore --verify-no-changes` | Formatting check used by CI |
| `dotnet build src\Splunk.OpenTelemetry.AutoInstrumentation\Splunk.OpenTelemetry.AutoInstrumentation.csproj --framework net8.0` | Fast plugin build for local iteration |

## Testing

- Integration tests use mock collectors and test applications under `test/test-applications/integrations/`.
- Smoke tests expect `OpenTelemetryDistribution/` to contain freshly built plugin;
run the Nuke workflow or the relevant build targets before relying on smoke results.

## Rules & Patterns

- Prefer focused `dotnet test` commands for local iteration;
use `.\build.cmd Workflow` before broad validation or packaging.
- Treat `OpenTelemetryDistribution/`, `NuGetPackage/`, `bin/Matrix/`, `bin/InstallationScripts/`,
and `test-artifacts/` as generated outputs.
- Do not edit files under `src/Splunk.OpenTelemetry.AutoInstrumentation/Vendors`
unless the task is explicitly about vendored code.
- Follow the Splunk GDI specification for distribution defaults,
configuration behavior, telemetry conventions, and profiling semantics;
document intentional differences.
- Keep implementation, docs, YAML fixtures, and smoke tests in sync.
- Keep generated distribution structure snapshots intentional;
update verified files only when packaging output really changed.

## Related repositories

- `open-telemetry/opentelemetry-dotnet-instrumentation`:
auto-instrumentation plugin hooks, settings parsing, distribution layout,
startup/shutdown behavior.
- `open-telemetry/opentelemetry-dotnet`: SDK, exporters, provider behavior,
OTLP defaults.
- `open-telemetry/opentelemetry-dotnet-contrib`:
contrib packages used by the distribution, including related
exporter/instrumentation packages when relevant.
- `signalfx/gdi-specification`: Splunk GDI requirements for distribution defaults,
configuration behavior, telemetry conventions, and profiling semantics.
