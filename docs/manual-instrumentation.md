# Manually instrument a .NET application

The auto-instrumentation provides a base you can build on by adding your own
custom instrumentation. By using both instrumentation approaches,
you'll be able to present a more detailed representation of the logic
and functionality of your application, clients, and framework.

## Instrument using System.Diagnostics API

For the list of steps required, see [link](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/docs/manual-instrumentation.md).

## Instrument using OpenTracing API

1. Add the `OpenTelemetry.Shims.OpenTracing` dependency to your project:

    ```xml
    <PackageReference Include="OpenTelemetry.Shims.OpenTracing" Version="1.0.0-rc9.10" />
    ```

1. Set `OTEL_DOTNET_AUTO_OPENTRACING_ENABLED` environment variable to `true`

1. Obtain the `OpenTracing.Util.GlobalTracer` instance:

    ```csharp
    var tracer = GlobalTracer.Instance;
    ```

1. Create a span. Optionally, set tags:

    ```csharp
    // Create an active span that will be automatically parented by any existing span in this context
    using (IScope scope = tracer.BuildSpan("MyTracedFunctionality").StartActive(finishSpanOnDispose: true))
    {
        var span = scope.Span;
        span.SetTag("MyTag", "MyValue");        
    }    
    ```

Further reading:

- [.NET Automatic instrumentation](https://opentelemetry.io/docs/instrumentation/net/automatic/)
- [.NET Manual instrumentation](https://opentelemetry.io/docs/instrumentation/net/manual/)
- [OpenTracing Shim for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Shims.OpenTracing)
