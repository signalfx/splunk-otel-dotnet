# Using the Splunk.OpenTelemetry.AutoInstrumentation NuGet package

## When to use the NuGet package

Use the NuGet package in the following scenarios:

1. You control the application build but not the machine/container where
  the application is running.
2. Support instrumentation of [`self-contained`](https://learn.microsoft.com/en-us/dotnet/core/deploying/#publish-self-contained)
  applications.
3. Facilitate developer experimentation with automatic instrumentation through
  NuGet packages.
4. Solve version conflicts between the dependencies used by the application and the
  automatic instrumentation.

## Limitations

While NuGet packages are a convenient way to deploy automatic
instrumentation, they can't be used in all cases. The most common
reasons for not using NuGet packages include the following:

1. You can't add the package to the application project. For example,
the application is from a third party that can't add the package.
2. Reduce disk usage, or the size of a virtual machine, when multiple applications
to be instrumented are installed in a single machine. In this case you can use
a single deployment for all .NET applications running on the machine.
3. A legacy application that can't be migrated to the [SDK-style project](https://learn.microsoft.com/en-us/nuget/resources/check-project-format#check-the-project-format).

## Using the NuGet packages

To automatically instrument your application with the Splunk Distribution
of OpenTelemetry .NET add
the `Splunk.OpenTelemetry.AutoInstrumentation` package to your project:

```terminal
dotnet add [<PROJECT>] package Splunk.OpenTelemetry.AutoInstrumentation --prerelease
```

If the application references packages that can be instrumented, but require
other packages for the instrumentation to work, the build will fail and prompt
you to either add the missing instrumentation package or to skip the
instrumentation of the corresponding package:

```terminal
~packages/opentelemetry.autoinstrumentation.buildtasks/1.9.0/build/OpenTelemetry.AutoInstrumentation.BuildTasks.targets(29,5): error : OpenTelemetry.AutoInstrumentation: add a reference to the instrumentation package 'MongoDB.Driver.Core.Extensions.DiagnosticSources' version 1.4.0 or add 'MongoDB.Driver.Core' to the property 'SkippedInstrumentations' to suppress this error.
```

To resolve the error either add the recommended instrumentation package or skip
the instrumentation of the listed package by adding it to the `SkippedInstrumentation`
property. For example:

```csproj
  <PropertyGroup>
    <SkippedInstrumentations>MongoDB.Driver.Core;StackExchange.Redis</SkippedInstrumentations>
  </PropertyGroup>
```

The same property can be also specified directly using the terminal.
Notice that the `;` separator needs to be properly escaped as '%3B':

```powershell
  dotnet build -p:SkippedInstrumentations=StackExchange.Redis%3BMongoDB.Driver.Core
```

To distribute the appropriate native runtime components with your .NET application,
specify a [Runtime Identifier (RID)](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog)
to build the application using `dotnet build` or `dotnet publish`. This might
require choosing between distributing a
[_self-contained_ or a _framework-dependent_](https://learn.microsoft.com/en-us/dotnet/core/deploying/)
application. Both types are compatible with automatic instrumentation.

Use the script in the output folder of the build to launch the
application with automatic instrumentation activated.

- On Windows, use `splunk-launch.cmd <application_executable>`
- On Linux or Unix, use `splunk-launch.sh <application_executable>`

If you launch the application using the `dotnet` CLI, add `dotnet` after the script.

- On Windows, use `splunk-launch.cmd dotnet <application>`
- On Linux and Unix, use `splunk-launch.sh dotnet <application>`

The script passes to the application all the command-line parameters you provide.
