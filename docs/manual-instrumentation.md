# Manually instrument a .NET application

The automatic instrumentation provides a base you can build upon by adding your own
custom instrumentation. By using both instrumentation approaches,
you can produce a more detailed representation of the logic
and functionality of your application, clients, and framework.

## Traces

For the list of steps required, see the [upstream documentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/docs/manual-instrumentation.md).

## Metrics

1. Add the `System.Diagnostics.DiagnosticSource` dependency to your project:

    ```xml
    <PackageReference Include="System.Diagnostics.DiagnosticSource" Version="7.0.0" />
    ```

1. Create a `Meter` instance:

    ```csharp
    using var meter = new Meter("My.Application", "1.0");
    ```

1. Create an `Instrument`:

    ```csharp
    var counter = meter.CreateCounter<long>("custom.counter", description: "Custom counter's description");
    ```

1. Update the `Instrument` value:

    ```csharp
    counter.Add(1);
    ```

1. Register your `Meter` with OpenTelemetry.AutoInstrumentation by setting the
`OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES` environment variable:

    ```bash
    OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES=My.Application
    ```

Further reading:

- [.NET Automatic instrumentation](https://opentelemetry.io/docs/instrumentation/net/automatic/)
- [.NET Manual instrumentation](https://opentelemetry.io/docs/instrumentation/net/manual/)
- [OpenTracing Shim for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Shims.OpenTracing)
