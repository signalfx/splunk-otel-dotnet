using YamlDotNet.Serialization;

namespace MatrixHelper;

public static class SettingsData
{
    public static Setting[] GetSettings()
    {
        var generalCategory = "general";
        var exporterCategory = "exporter";
        var tracePropagationCategory = "trace propagation";
        var samplersCategory = "sampler";
        var resourceDetectorCategory = "resource detector";
        var instrumentationCategory = "instrumentation";
        var diagnosticCategory = "diagnostic logging";

        var settings = new Setting[]
        {
            // general
            new("SPLUNK_TRACE_RESPONSE_HEADER_ENABLED", "Activated by default. Adds server trace information to HTTP response headers. For more information, see :ref:`server-trace-information-dotnet-otel`. The default value is `true`.", "true", "boolean", generalCategory, "How to handle ref links?" ),
            new("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", "Names of the executable files that you don't want the profiler to instrument. Supports multiple semicolon-separated values, for example: `ReservedProcess.exe;powershell.exe`. Notice that applications launched using dotnet MyApp.dll have process name `dotnet` or `dotnet.exe`. Can't be set using the web.config or app.config files.", "", generalCategory, "string"),
            new("OTEL_DOTNET_AUTO_TRACES_ENABLED", "Long Traces are collected by default. To deactivate trace collection, set the environment variable to `false`. Data from custom or manual instrumentation is not affected.", "true", "boolean", generalCategory),
            new("OTEL_DOTNET_AUTO_METRICS_ENABLED", "etrics are collected by default. To deactivate metric collection, set the environment variable to `false`. Data from custom or manual instrumentation is not affected.", "true", "boolean", generalCategory),
            new("OTEL_DOTNET_AUTO_LOGS_ENABLED", "Logs are collected by default. To deactivate log collection, set the environment variable to `false`. Data from custom or manual instrumentation is not affected.", "true", "boolean", generalCategory),
            new("OTEL_DOTNET_AUTO_OPENTRACING_ENABLED", "Activates the OpenTracing tracer. The default value is `false`. See :ref:`migrate-signalfx-dotnet-to-dotnet-otel` for more information.", "false", "boolean", generalCategory, "ref link"),
            new("OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED", "Activates immediate redirection of the assemblies used by the automatic instrumentation on the .NET Framework. The default values is `true`. Can't be set using the web.config or app.config files.", "true", "boolean", generalCategory, "default value in description, also in other records"),
            new("OTEL_DOTNET_AUTO_FLUSH_ON_UNHANDLEDEXCEPTION", "Controls whether the telemetry data is flushed when an `AppDomain.UnhandledException` event is raised. Set to `true` when experiencing missing telemetry at the same time of unhandled exceptions.", "false", "boolean", generalCategory),
            new("OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED", "Activates RuleEngine. The default values is `true`. RuleEngine increases the stability of the instrumentation by validating assemblies for unsupported scenarios.", "true", "boolean", generalCategory),
            new("OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED", "Activate to let the process fail when automatic instrumentation can't be executed. This setting is for debugging purposes, don't use it in production environments. The default value is `false`. Can't be set using the web.config or app.config files.", "false", "boolean", generalCategory),

            // exporter
            new("OTEL_EXPORTER_OTLP_ENDPOINT", "The URL to where traces and metrics are sent. The default value is `http://localhost:4318`. Setting a value overrides the `SPLUNK_REALM` environment variable.", "", "string", exporterCategory),
            new("SPLUNK_REALM", "The name of your organization's realm, for example, `us0`. When you set the realm, telemetry is sent directly to the ingest endpoint of Splunk Observability Cloud, bypassing the Splunk Distribution of OpenTelemetry Collector." , "", "string", exporterCategory),
            new("SPLUNK_ACCESS_TOKEN", "A Splunk authentication token that lets exporters send data directly to Splunk Observability Cloud. Unset by default. Required if you need to send data to the Splunk Observability Cloud ingest endpoint. See :ref:`admin-tokens`.", "", "string", exporterCategory, "ref link"),

            // trace propagation
            new("OTEL_PROPAGATORS", "Comma-separated list of propagators for the tracer. The default value is `tracecontext,baggage`. Supported values are `b3multi`, `b3`, `tracecontext`, and `baggage`.", "tracecontext,baggage", "string", tracePropagationCategory, "potential enum values"),
            
            // samplers
            new("OTEL_TRACES_SAMPLER", "Sampler to use. The default value is `parentbased_always_on`. Supported values are `always_on`, `always_off`, `traceidratio`, `parentbased_always_on`, `parentbased_always_off`, and `parentbased_traceidratio`.", "parentbased_always_on", "string", samplersCategory, "enum?"),
            new("OTEL_TRACES_SAMPLER_ARG", "Semicolon-separated list of rules for the `rules` sampler. The default value is `1.0`.", "1,0", "string", samplersCategory, "default value 1.0 is true only for parentbased_always_on, other may have different default values"),

            // resource detectors
            new("OTEL_DOTNET_AUTO_RESOURCE_DETECTOR_ENABLED", "Activates or deactivates all resource detectors. The default values is `true`.", "true", "boolean", resourceDetectorCategory),
            new("OTEL_DOTNET_AUTO_{DECTECTOR}_RESOURCE_DETECTOR_ENABLED", "Activates or deactivates a specific resource detector, where `{DETECTOR}` is the uppercase identifier of the resource detector you want to activate. Overrides `OTEL_DOTNET_AUTO_RESOURCE_DETECTOR_ENABLED`.", "depends on `OTEL_DOTNET_AUTO_RESOURCE_DETECTOR_ENABLED`", "boolean", resourceDetectorCategory, "how to handle this default value? description is enough?"),

            // instrumentation
            new("OTEL_SERVICE_NAME", "Name of the service or application you're instrumenting. Takes precedence over the service name defined in the `OTEL_RESOURCE_ATTRIBUTES` variable.", "we have some logic here if it is not manually set: https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/9ccd2174be3c5e14f0b159a7e0e7543516318d28/src/OpenTelemetry.AutoInstrumentation/Configurations/ServiceNameConfigurator.cs", "string", instrumentationCategory, "default value description"),
            new("OTEL_RESOURCE_ATTRIBUTES", "Comma-separated list of resource attributes added to every reported span. For example, `key1=val1,key2=val2`.", "", "string", instrumentationCategory),
            new("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "Comma-separated list of additional `System.Diagnostics.ActivitySource` names to be added to the tracer at startup. Use it to capture spans from manual instrumentation.", "", "string", instrumentationCategory),
            new("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "Comma-separated list of additional `System.Diagnostics.Metrics.Meter` names to be added to the meter at the startup. Use it to capture custom metrics.", "", "string", instrumentationCategory),
            new("OTEL_SPAN_ATTRIBUTE_COUNT_LIMIT", "Maximum number of attributes per span. Default value is unlimited.", "", "int", instrumentationCategory, "int?"),
            new("OTEL_SPAN_EVENT_COUNT_LIMIT", "Maximum number of events per span. Default value is unlimited.", "", "int", instrumentationCategory, "int?"),
            new("OTEL_SPAN_LINK_COUNT_LIMIT", "Maximum number of links per span. Default value is `1000`.", "1000", "int", instrumentationCategory, "int?"),
            new("OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT", "Maximum length of strings for attribute values. Values larger than the limit are truncated. Default value is `1200`. Empty values are treated as infinity.", "1200", "int", instrumentationCategory, "int?"),
            new("OTEL_DOTNET_AUTO_GRAPHQL_SET_DOCUMENT", "Whether the GraphQL instrumentation can pass raw queries as a `graphql.document` attribute. As queries might contain sensitive information, the default value is `false`.", "false", "boolean", instrumentationCategory),
            new("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES", "Comma-separated list of additional legacy source names to be added to the tracer at the startup. Use it to capture `System.Diagnostics.Activity` objects created without using the `System.Diagnostics.ActivitySource` API.", "", "string", instrumentationCategory),

            // diagnostic logging
            new("OTEL_LOG_LEVEL", "Sets the logging level for instrumentation log messages. Possible values are `none`, `error`, `warn`, `info`, and `debug`. The default value is `info`. Can't be set using the web.config or app.config files.", "info", "enum", diagnosticCategory, "handle enum values/mark as string"),
            new("OTEL_DOTNET_AUTO_LOG_DIRECTORY", "Directory of the .NET tracer logs. The default value is `/var/log/opentelemetry/dotnet` for Linux, and `%ProgramData%\\OpenTelemetry .NET AutoInstrumentation\\logs` for Windows. Can't be set using the web.config or app.config files.", "see descruotuin", "string", diagnosticCategory, "defaultvalue"),
            new("OTEL_DOTNET_AUTO_TRACES_CONSOLE_EXPORTER_ENABLED", "Whether the traces console exporter is activated. The default value is `false`.", "false", "boolean", diagnosticCategory),
            new("OTEL_DOTNET_AUTO_METRICS_CONSOLE_EXPORTER_ENABLED", "Whether the metrics console exporter is activated. The default value is `false`.", "false", "boolean", diagnosticCategory),
            new("OTEL_DOTNET_AUTO_LOGS_CONSOLE_EXPORTER_ENABLED", "Whether the logs console exporter is activated. The default value is `false`.The default value is `false`.", "false", "boolean", diagnosticCategory),
            new("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "Whether the log state have to be formatted. The default value is `false`.", "false", "boolean", diagnosticCategory),

            // manual installation
            new("MANUAL SETTINGS", "How to handle https://docs.splunk.com/observability/en/gdi/get-data-in/application/otel-dotnet/configuration/advanced-dotnet-configuration.html#environment-variables-for-manual-installation?", "N/A", "N/A", "manual", "See description")
        };

        return settings;
    }
}

public class Setting(string name, string description, string defaultValue, string type, string category, string? toDoComment = null)
{
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;

    public string Default { get; set; } = defaultValue;

    public string Type { get; set; } = type;

    public string Category { get; set; } = category;

    // TODO remove this property
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? ToDoComment { get; set; } = toDoComment;
}
