// <copyright file="SettingsData.cs" company="Splunk Inc.">
// Copyright Splunk Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace MatrixHelper;

internal static class SettingsData
{
    public const string InstrumentationCategory = "instrumentation";
    private const string GeneralCategory = "general";
    private const string ExporterCategory = "exporter";
    private const string TracePropagationCategory = "trace propagation";
    private const string SamplersCategory = "sampler";
    private const string ResourceDetectorCategory = "resource detector";
    private const string ResourceAttributesCategory = "resource attributes";
    private const string DiagnosticCategory = "diagnostic logging";
    private const string ProfilingCategory = "profiling";
    private const string OTLPCategory = "otlp";

    public static Setting[] GetSettings()
    {
        var settings = new Setting[]
        {
            // general
            new("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", "Names of the executable files that you don't want the profiler to instrument. Supports multiple semicolon-separated values, for example: `ReservedProcess.exe;powershell.exe`. Notice that applications launched using dotnet MyApp.dll have process name `dotnet` or `dotnet.exe`. Can't be set using the web.config or app.config files.", string.Empty, "string", GeneralCategory),
            new("OTEL_DOTNET_AUTO_TRACES_ENABLED", "Long Traces are collected by default. To deactivate trace collection, set the environment variable to `false`. Data from custom or manual instrumentation is not affected.", "true", "boolean", GeneralCategory),
            new("OTEL_DOTNET_AUTO_METRICS_ENABLED", "Metrics are collected by default. To deactivate metric collection, set the environment variable to `false`. Data from custom or manual instrumentation is not affected.", "true", "boolean", GeneralCategory),
            new("OTEL_DOTNET_AUTO_LOGS_ENABLED", "Logs are collected by default. To deactivate log collection, set the environment variable to `false`. Data from custom or manual instrumentation is not affected.", "true", "boolean", GeneralCategory),
            new("OTEL_DOTNET_AUTO_OPENTRACING_ENABLED", "This feature is deprecated. Avoid usage. Convert your OpenTracing dependency to OpenTelemetry. It activates the OpenTracing tracer. The default value is `false`.", "false", "boolean", GeneralCategory),
            new("OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED", "Activates immediate redirection of the assemblies used by the automatic instrumentation on the .NET Framework. The default values is `true`. Can't be set using the web.config or app.config files.", "true", "boolean", GeneralCategory),
            new("OTEL_DOTNET_AUTO_FLUSH_ON_UNHANDLEDEXCEPTION", "Controls whether the telemetry data is flushed when an `AppDomain.UnhandledException` event is raised. Set to `true` when experiencing missing telemetry at the same time of unhandled exceptions.", "false", "boolean", GeneralCategory),
            new("OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED", "Activates RuleEngine. The default values is `true`. RuleEngine increases the stability of the instrumentation by validating assemblies for unsupported scenarios.", "true", "boolean", GeneralCategory),
            new("OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED", "Activate to let the process fail when automatic instrumentation can't be executed. This setting is for debugging purposes, don't use it in production environments. The default value is `false`. Can't be set using the web.config or app.config files.", "false", "boolean", GeneralCategory),

            // exporter
            new("OTEL_LOGS_EXPORTER", "Comma-separated list of exporters. Supported options: `otlp`, `console`, `none`.", "otlp", "string", ExporterCategory),
            new("OTEL_METRICS_EXPORTER", "Comma-separated list of exporters. Supported options: `otlp`, `console`, `none`.", "otlp", "string", ExporterCategory),
            new("OTEL_TRACES_EXPORTER", "Comma-separated list of exporters. Supported options: `otlp`, `console`, `none`.", "otlp", "string", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_ENDPOINT", "The URL to where traces, metrics, and logs are sent. The default value is `http://localhost:4318`. Setting a value overrides the `SPLUNK_REALM` environment variable.", string.Empty, "string", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", "Equivalent to `OTEL_EXPORTER_OTLP_ENDPOINT`, but applies only to logs.", string.Empty, "string", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", "Equivalent to `OTEL_EXPORTER_OTLP_ENDPOINT`, but applies only to metrics.", string.Empty, "string", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "Equivalent to `OTEL_EXPORTER_OTLP_ENDPOINT`, but applies only to traces.", string.Empty, "string", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_PROTOCOL", "OTLP exporter transport protocol. Supported values are `grpc`, `http/protobuf`.", "http/protobuf", "string", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_TRACES_PROTOCOL", "Equivalent to `OTEL_EXPORTER_OTLP_PROTOCOL`, but applies only to traces.", "http/protobuf", "string", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_METRICS_PROTOCOL", "Equivalent to `OTEL_EXPORTER_OTLP_PROTOCOL`, but applies only to metrics.", "http/protobuf", "string", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_LOGS_PROTOCOL", "Equivalent to `OTEL_EXPORTER_OTLP_PROTOCOL`, but applies only to logs.", "http/protobuf", "string", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_TIMEOUT", "The max waiting time (in milliseconds) for the backend to process each batch.", "10000", "int", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_TRACES_TIMEOUT", "Equivalent to `OTEL_EXPORTER_OTLP_TIMEOUT`, but applies only to traces.", "10000", "int", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_METRICS_TIMEOUT", "Equivalent to `OTEL_EXPORTER_OTLP_TIMEOUT`, but applies only to metrics.", "10000", "int", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_LOGS_TIMEOUT", "Equivalent to `OTEL_EXPORTER_OTLP_TIMEOUT`, but applies only to logs.", "10000", "int", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_HEADERS", "Comma-separated list of additional HTTP headers sent with each export, for example: `Authorization=secret,X-Key=Value`.", string.Empty, "string", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_TRACES_HEADERS", "Equivalent to `OTEL_EXPORTER_OTLP_HEADERS`, but applies only to traces.", string.Empty, "string", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_METRICS_HEADERS", "Equivalent to `OTEL_EXPORTER_OTLP_HEADERS`, but applies only to metrics.", string.Empty, "string", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_LOGS_HEADERS", "Equivalent to `OTEL_EXPORTER_OTLP_HEADERS`, but applies only to logs.", string.Empty, "string", ExporterCategory),
            new("SPLUNK_REALM", "The name of your organization's realm, for example, `us0`. When you set the realm, telemetry is sent directly to the ingest endpoint of Splunk Observability Cloud, bypassing the Splunk Distribution of OpenTelemetry Collector.", string.Empty, "string", ExporterCategory),
            new("SPLUNK_ACCESS_TOKEN", "A Splunk authentication token that lets exporters send data directly to Splunk Observability Cloud. Unset by default. Required if you need to send data to the Splunk Observability Cloud ingest endpoint.", string.Empty, "string", ExporterCategory),
            new("OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE", "The aggregation temporality to use on the basis of instrument kind. The supported options are `Cumulative` for all instrument kinds and `Delta` for Counter, Asynchronous Counter, and Histogram instrument kinds. If you use `Delta` for UpDownCounter and Asynchronous UpDownCounter instrument kinds, the `Cumulative` aggregation temporality will be used. `LowMemory`, from the OpenTelemetry specification, is not supported.", "Cumulative", "string", ExporterCategory),

            // profiling
            new("SPLUNK_PROFILER_ENABLED", "Activates AlwaysOn Profiling.", "false", "boolean", ProfilingCategory),
            new("SPLUNK_PROFILER_MEMORY_ENABLED", "Activates memory profiling.", "false", "boolean", ProfilingCategory),
            new("SPLUNK_PROFILER_LOGS_ENDPOINT", "The collector endpoint for profiler logs.", "http://localhost:4318/v1/logs", "string", ProfilingCategory),
            new("SPLUNK_PROFILER_CALL_STACK_INTERVAL", "Frequency with which call stacks are sampled, in milliseconds.", "10000", "int", ProfilingCategory),
            new("SPLUNK_PROFILER_EXPORT_TIMEOUT", "Export timeout, in milliseconds.", "3000", "int", ProfilingCategory),
            new("SPLUNK_PROFILER_MAX_MEMORY_SAMPLES", "Maximum memory samples collected per minute. Maximum value is 200.", "200", "int", ProfilingCategory),
            new("SPLUNK_PROFILER_EXPORT_INTERVAL", "Export interval timeout, in milliseconds. Minimum value is 500.", "500", "int", ProfilingCategory),

            // snapshots
            new("SPLUNK_SNAPSHOT_PROFILER_ENABLED", "Activates snapshots collection.", "false", "boolean", ProfilingCategory),
            new("SPLUNK_SNAPSHOT_SAMPLING_INTERVAL", "Sampling interval for snapshot collections, in milliseconds.", "60", "int", ProfilingCategory),
            new("SPLUNK_SNAPSHOT_SELECTION_PROBABILITY", "Sets probability of selecting trace for snapshots.", "0.01", "double", ProfilingCategory),
            new("SPLUNK_SNAPSHOT_HIGH_RES_TIMER_ENABLED", "Sets default timer precision on Windows to 1ms.", "false", "bool", ProfilingCategory),

            // trace propagation
            new("OTEL_PROPAGATORS", "Comma-separated list of propagators for the tracer. The default value is `tracecontext,baggage`. Supported values are `b3multi`, `b3`, `tracecontext`, and `baggage`.", "tracecontext,baggage", "string", TracePropagationCategory),

            // samplers
            new("OTEL_TRACES_SAMPLER", "Sampler to use. The default value is `parentbased_always_on`. Supported values are `always_on`, `always_off`, `traceidratio`, `parentbased_always_on`, `parentbased_always_off`, and `parentbased_traceidratio`.", "parentbased_always_on", "string", SamplersCategory),
            new("OTEL_TRACES_SAMPLER_ARG", "Semicolon-separated list of rules for the `rules` sampler. The default value is `1.0` for `traceidratio`.", "1.0", "string", SamplersCategory),

            // resource detectors
            new("OTEL_DOTNET_AUTO_RESOURCE_DETECTOR_ENABLED", "Activates or deactivates all resource detectors. The default values is `true`.", "true", "boolean", ResourceDetectorCategory),
            new("OTEL_DOTNET_AUTO_{DECTECTOR}_RESOURCE_DETECTOR_ENABLED", "Activates or deactivates a specific resource detector, where `{DETECTOR}` is the uppercase identifier of the resource detector you want to activate. Overrides `OTEL_DOTNET_AUTO_RESOURCE_DETECTOR_ENABLED`.", "depends on `OTEL_DOTNET_AUTO_RESOURCE_DETECTOR_ENABLED`", "boolean", ResourceDetectorCategory),

            // resource attributes
            new("OTEL_SERVICE_NAME", "Name of the service or application you're instrumenting. Takes precedence over the service name defined in the `OTEL_RESOURCE_ATTRIBUTES` variable.", string.Empty, "string", ResourceAttributesCategory),
            new("OTEL_RESOURCE_ATTRIBUTES", "Comma-separated list of resource attributes added to every reported span. For example, `key1=val1,key2=val2`.", string.Empty, "string", ResourceAttributesCategory),

            // instrumentation
            new("SPLUNK_TRACE_RESPONSE_HEADER_ENABLED", "Activated by default. Adds server trace information to HTTP response headers. The default value is `true`.", "true", "boolean", InstrumentationCategory),
            new("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "Comma-separated list of additional `System.Diagnostics.ActivitySource` names to be added to the tracer at startup. Use it to capture spans from manual instrumentation.", string.Empty, "string", InstrumentationCategory),
            new("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "Comma-separated list of additional `System.Diagnostics.Metrics.Meter` names to be added to the meter at the startup. Use it to capture custom metrics.", string.Empty, "string", InstrumentationCategory),
            new("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES", "Comma-separated list of additional legacy source names to be added to the tracer at the startup. Use it to capture `System.Diagnostics.Activity` objects created without using the `System.Diagnostics.ActivitySource` API.", string.Empty, "string", OTLPCategory),
            new("OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED", "Activates or deactivates all instrumentations. Can’t be set using the web.config or app.config files.", "true", "boolean", InstrumentationCategory),
            new("OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED", "Activates or deactivates all trace instrumentations. Overrides `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`. Inherits the value of the `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED` environment variable. Can’t be set using the web.config or app.config files.", string.Empty, "boolean", InstrumentationCategory),
            new("OTEL_DOTNET_AUTO_TRACES_{INSTRUMENTATION}_INSTRUMENTATION_ENABLED", "Activates or deactivates a specific trace instrumentation, where `{INSTRUMENTATION}` is the case-sensitive name of the instrumentation. Overrides `OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED`. Inherits the value of the `OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED` environment variable. Can’t be set using the web.config or app.config files. See Supported libraries for a complete list of supported instrumentations and their names.", string.Empty, "boolean", InstrumentationCategory),
            new("OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED", "Activates or deactivates all metric instrumentations. Overrides `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`. Inherits the value of the `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED` environment variable. Can’t be set using the web.config or app.config files.", string.Empty, "boolean", InstrumentationCategory),
            new("OTEL_DOTNET_AUTO_METRICS_{INSTRUMENTATION}_INSTRUMENTATION_ENABLED", "Activates or deactivates a specific metric instrumentation, where `{INSTRUMENTATION}` is the case-sensitive name of the instrumentation. Overrides `OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED`. Inherits the value of the `OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED` environment variable. Can’t be set using the web.config or app.config files. See Supported libraries for a complete list of supported instrumentations and their names.", string.Empty, "boolean", InstrumentationCategory),
            new("OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED", "Activates or deactivates all log instrumentations. Overrides `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`. Inherits the value of the `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED` environment variable. Can’t be set using the web.config or app.config files.", string.Empty, "boolean", InstrumentationCategory),
            new("OTEL_DOTNET_AUTO_LOGS_{INSTRUMENTATION}_INSTRUMENTATION_ENABLED", "Activates or deactivates a specific log instrumentation, where `{INSTRUMENTATION}` is the case-sensitive name of the instrumentation. Overrides `OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED`. Inherits the value of the `OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED` environment variable. Can’t be set using the web.config or app.config files. See Supported libraries for a complete list of supported instrumentations and their names.", string.Empty, "boolean", InstrumentationCategory),

            // OTLP
            new("OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT", "Maximum length of strings for attribute values. Values larger than the limit are truncated. Default value is `1200`. Empty values are treated as infinity.", "1200", "int", OTLPCategory),
            new("OTEL_ATTRIBUTE_COUNT_LIMIT", "Maximum allowed span attribute count. Default value is `128`.", "128", "int", OTLPCategory),
            new("OTEL_SPAN_ATTRIBUTE_VALUE_LENGTH_LIMIT", "Maximum allowed attribute value size. Not applicable for metrics.", string.Empty, "int", OTLPCategory),
            new("OTEL_SPAN_ATTRIBUTE_COUNT_LIMIT", "Maximum number of attributes per span. Default value is unlimited.", string.Empty, "int", OTLPCategory),
            new("OTEL_SPAN_EVENT_COUNT_LIMIT", "Maximum number of events per span. Default value is unlimited.", string.Empty, "int", OTLPCategory),
            new("OTEL_SPAN_LINK_COUNT_LIMIT", "Maximum number of links per span. Default value is `1000`.", "1000", "int", OTLPCategory),
            new("OTEL_EVENT_ATTRIBUTE_COUNT_LIMIT", "Maximum allowed attribute per span event count. Default value is `128`.", "128", "int", OTLPCategory),
            new("OTEL_LINK_ATTRIBUTE_COUNT_LIMIT", "Maximum allowed attribute per span link count. Default value is `128`.", "128", "int", OTLPCategory),
            new("OTEL_LOGRECORD_ATTRIBUTE_VALUE_LENGTH_LIMIT", "Maximum allowed log record attribute value size.", string.Empty, "int", OTLPCategory),
            new("OTEL_LOGRECORD_ATTRIBUTE_COUNT_LIMIT", "Maximum allowed log record attribute count. Default value is `128`.", "128", "int", OTLPCategory),

            // diagnostic logging
            new("OTEL_DOTNET_AUTO_LOGGER", "AutoInstrumentation diagnostic logs sink. (supported values: `none`,`file`,`console`).", "file", "string", DiagnosticCategory),
            new("OTEL_LOG_LEVEL", "Sets the logging level for instrumentation log messages. Possible values are `none`, `error`, `warn`, `info`, and `debug`. Can't be set using the web.config or app.config files.", "info", "string", DiagnosticCategory),
            new("OTEL_DOTNET_AUTO_LOG_DIRECTORY", @"Directory of the .NET tracer logs. The default value is `/var/log/opentelemetry/dotnet` for Linux, and `%ProgramData%\OpenTelemetry .NET AutoInstrumentation\logs` for Windows. Can't be set using the web.config or app.config files.", string.Empty, "string", DiagnosticCategory),
            new("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", "Maximum size (in bytes) of a single log file created by the Auto Instrumentation.", "10485760", "int", DiagnosticCategory),
            new("OTEL_DOTNET_AUTO_TRACES_CONSOLE_EXPORTER_ENABLED", "Deprecated. Whether the traces console exporter is activated. It can be configured by `OTEL_TRACES_EXPORTER`.", "false", "boolean", DiagnosticCategory)
        };

        return settings;
    }
}
