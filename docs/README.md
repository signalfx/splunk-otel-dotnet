# Draft documentation

> The official documentation for this distribution can be found in the
> [Splunk Docs](https://docs.splunk.com/Observability/gdi/get-data-in/application/dotnet/get-started.html)
> site.
> For instructions on how to contribute to the docs, see
> [CONTRIBUTING.md](../CONTRIBUTING.md#documentation).

## Requirements

TODO

## Getting started

This Splunk distribution comes with the following defaults:

- [W3C tracecontext](https://www.w3.org/TR/trace-context/) and
  [W3C baggage](https://www.w3.org/TR/baggage/) context propagation.
- OTLP over HTTP exporter configured to send spans to a locally running [Splunk OpenTelemetry
  Connector](https://github.com/signalfx/splunk-otel-collector)

Install the distribution:

TODO

### Basic configuration

TODO

## Advanced configuration

For advanced configuration options, refer to
the [Advanced configuration](config.md) documentation.

## Manual instrumentation

TODO

## Migrating

If you're currently using the [SignalFx Instrumentation for .NET](https://github.com/signalfx/signalfx-dotnet-tracing)
and want to migrate to the Splunk Distribution of OpenTelemetry .NET,
see the [Migrating](migrating.md) documentation.

## Troubleshooting

For troubleshooting information, see the
[Troubleshooting](troubleshooting.md) documentation.
