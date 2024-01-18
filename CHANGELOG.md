# Changelog

All notable changes to this component are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
This component adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased](https://github.com/signalfx/splunk-otel-dotnet/compare/v1.3.0...HEAD)

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [1.3.0](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v1.3.0)

This release is built on top of [OpenTelemetry .NET Auto Instrumentation v1.3.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.3.0).

### Added

Added support for AlwaysOn Profiling for CPU and memory allocation.

### Changed

- Updated [OpenTelemetry .NET Auto Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation):
  [`1.3.0`](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.3.0).

## [1.2.1](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v1.2.1)

This release is built on top of [OpenTelemetry .NET Auto Instrumentation v1.2.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.2.0).
This release is being created to publish the corresponding Docker image.

### Added

- Workflow to publish Docker image.

## [1.2.0](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v1.2.0)

This release is built on top of [OpenTelemetry .NET Auto Instrumentation v1.2.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.2.0).

### Added

- Ability to update installation via PS module (`Splunk.OTel.DotNet.psm1`).

### Changed

- Updated [OpenTelemetry .NET Auto Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation):
[`1.2.0`](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.2.0).
- Updated value for following attributes:
  - `telemetry.distro.name` to `splunk-otel-dotnet`
  - `telemetry.distro.version` to current release version.

### Deprecated

- Deprecate `splunk.otel.version` attribute.

## [1.1.0](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v1.1.0)

This release is built on top of [OpenTelemetry .NET Auto Instrumentation v1.1.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.1.0).

### Changed

- Updated [OpenTelemetry .NET Auto Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation):
  [`1.1.0`](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.1.0).

## [1.0.2](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v1.0.2)

This release is built on top of [OpenTelemetry .NET Auto Instrumentation v1.0.2](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.0.2).

### Changed

- Updated [OpenTelemetry .NET Auto Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation):
  [`1.0.2`](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.0.2).

## [1.0.0-rc.3](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v1.0.0-rc.3)

This is a release candidate,
built on top of [OpenTelemetry .NET Auto Instrumentation v1.0.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.0.0).

### Changed

- Updated [OpenTelemetry .NET Auto Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation):
  [`1.0.0`](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.0.0).

## [1.0.0-rc.2](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v1.0.0-rc.2)

This is a release candidate,
built on top of [OpenTelemetry .NET Auto Instrumentation v1.0.0-rc.2](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.0.0-rc.2).

### Added

- Added NuGet package `Splunk.OpenTelemetry.AutoInstrumentation`.

### Changed

- Updated [OpenTelemetry .NET Auto Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation):
  [`1.0.0-rc.2`](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.0.0-rc.2).

## [1.0.0-rc.1](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v1.0.0-rc.1)

This is a release candidate,
built on top of [OpenTelemetry .NET Auto Instrumentation v1.0.0-rc.1](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.0.0-rc.1).

### Changed

- Updated [OpenTelemetry .NET Auto Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation):
  [`1.0.0-rc.1`](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.0.0-rc.1).

## [0.2.0-beta.1](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v0.2.0-beta.1)

This is a beta release,
built on top of [OpenTelemetry .NET Auto Instrumentation v0.7.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.7.0).

### Changed

- Updated [OpenTelemetry .NET Auto Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation):
  [`0.7.0`](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.7.0).

## [0.1.0-beta.1](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v0.1.0-beta.1)

This is a beta release,
built on top of [OpenTelemetry .NET Auto Instrumentation v0.6.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.6.0).

### Added

- .NET Framework settings can read configuration from Web.config and App.config
- Add `SPLUNK_REALM` configuration key to specify direct ingest realm.
- Add `SPLUNK_ACCESS_TOKEN` configuration key to authorize direct ingest.
- Add `SPLUNK_TRACE_RESPONSE_HEADER_ENABLED` configuration key
  to support Splunk RUM.

### Changed

- Updated [OpenTelemetry .NET Auto Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation):
  [`0.6.0`](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.6.0).

## [0.0.1-alpha.2](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v0.0.1-alpha.2)

This is an alpha release,
built on top of [OpenTelemetry .NET Auto Instrumentation v0.4.0-beta.1](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.4.0-beta.1).

## [0.0.1-alpha.1](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v0.0.1-alpha.1)

This is the first alpha release,
built on top of [OpenTelemetry .NET Auto Instrumentation v0.3.1-beta.1](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.3.1-beta.1).
