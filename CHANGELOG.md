# Changelog

All notable changes to this component are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
This component adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased](https://github.com/signalfx/splunk-otel-dotnet/compare/v1.9.0...HEAD)

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [1.9.0](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v1.9.0)

This release is built on top of [OpenTelemetry .NET Auto Instrumentation v1.10.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.10.0)
bringing changes also from [v1.10.0-beta.1](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.10.0-beta.1).

> [!WARNING]
> Version 1.9.0 of Splunk Distribution of OpenTelemetry .NET will no longer work
> with .NET 6 or .NET 7. .NET 6 reached End of Life on November 12, 2024
> and .NET 7 reached End of Life on May 14, 2024.
> Customers who want to continue instrumenting .NET 6 or .NET 7 services must use
> Splunk Distribution of OpenTelemetry .NET version 1.8.0 or less.
> Best effort support for Splunk Distribution of OpenTelemetry .NET is provided
> up to November 12, 2025 for the last versions of .NET 6 (version 6.0.36)
> or .NET 7 (version 7.0.20) only.

### Changed

- Updated [OpenTelemetry .NET Auto Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation):
  [`1.10.0`](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.10.0).

## [1.8.0](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v1.8.0)

This release is built on top of [OpenTelemetry .NET Auto Instrumentation v1.9.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.9.0).

### Changed

- Updated [OpenTelemetry .NET Auto Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation):
  [`1.9.0`](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.9.0).

## [1.7.0](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v1.7.0)

This release is built on top of [OpenTelemetry .NET Auto Instrumentation v1.8.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.8.0).

### Changed

- Updated [OpenTelemetry .NET Auto Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation):
  [`1.8.0`](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.8.0).

## [1.6.0](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v1.6.0)

This release is built on top of [OpenTelemetry .NET Auto Instrumentation v1.7.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.7.0).

### Changed

- Updated [OpenTelemetry .NET Auto Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation):
  [`1.7.0`](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.7.0).

## [1.5.0](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v1.5.0)

This release is built on top of [OpenTelemetry .NET Auto Instrumentation v1.6.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.6.0).

### Changed

- Updated [OpenTelemetry .NET Auto Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation):
  [`1.6.0`](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.6.0).

## [1.4.0](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v1.4.0)

This release is built on top of [OpenTelemetry .NET Auto Instrumentation v1.4.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.4.0).

### Changed

- Updated [OpenTelemetry .NET Auto Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation):
  [`1.4.0`](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.4.0).

## [1.3.0](https://github.com/signalfx/splunk-otel-dotnet/releases/tag/v1.3.0)

This release is built on top of [OpenTelemetry .NET Auto Instrumentation v1.3.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.3.0).

### Added

- Added support for AlwaysOn Profiling for CPU and memory allocation.

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
