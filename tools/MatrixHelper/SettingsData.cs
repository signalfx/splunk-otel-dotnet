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
    private const string DiagnosticCategory = "diagnostic logging";

    public static Setting[] GetSettings()
    {
        var settings = new Setting[]
        {
            // general
            new("SPLUNK_TRACE_RESPONSE_HEADER_ENABLED", "Activated by default. Adds server trace information to HTTP response headers. The default value is `true`.", "true", "boolean", GeneralCategory),
            new("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", "Names of the executable files that you don't want the profiler to instrument. Supports multiple semicolon-separated values, for example: `ReservedProcess.exe;powershell.exe`. Notice that applications launched using dotnet MyApp.dll have process name `dotnet` or `dotnet.exe`. Can't be set using the web.config or app.config files.", string.Empty, "string", GeneralCategory),
            new("OTEL_DOTNET_AUTO_TRACES_ENABLED", "Long Traces are collected by default. To deactivate trace collection, set the environment variable to `false`. Data from custom or manual instrumentation is not affected.", "true", "boolean", GeneralCategory),
            new("OTEL_DOTNET_AUTO_METRICS_ENABLED", "etrics are collected by default. To deactivate metric collection, set the environment variable to `false`. Data from custom or manual instrumentation is not affected.", "true", "boolean", GeneralCategory),
            new("OTEL_DOTNET_AUTO_LOGS_ENABLED", "Logs are collected by default. To deactivate log collection, set the environment variable to `false`. Data from custom or manual instrumentation is not affected.", "true", "boolean", GeneralCategory),
            new("OTEL_DOTNET_AUTO_OPENTRACING_ENABLED", "Activates the OpenTracing tracer. The default value is `false`.", "false", "boolean", GeneralCategory),
            new("OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED", "Activates immediate redirection of the assemblies used by the automatic instrumentation on the .NET Framework. The default values is `true`. Can't be set using the web.config or app.config files.", "true", "boolean", GeneralCategory),
            new("OTEL_DOTNET_AUTO_FLUSH_ON_UNHANDLEDEXCEPTION", "Controls whether the telemetry data is flushed when an `AppDomain.UnhandledException` event is raised. Set to `true` when experiencing missing telemetry at the same time of unhandled exceptions.", "false", "boolean", GeneralCategory),
            new("OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED", "Activates RuleEngine. The default values is `true`. RuleEngine increases the stability of the instrumentation by validating assemblies for unsupported scenarios.", "true", "boolean", GeneralCategory),
            new("OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED", "Activate to let the process fail when automatic instrumentation can't be executed. This setting is for debugging purposes, don't use it in production environments. The default value is `false`. Can't be set using the web.config or app.config files.", "false", "boolean", GeneralCategory),

            // exporter
            new("OTEL_EXPORTER_OTLP_ENDPOINT", "The URL to where traces and metrics are sent. The default value is `http://localhost:4318`. Setting a value overrides the `SPLUNK_REALM` environment variable.", string.Empty, "string", ExporterCategory),
            new("SPLUNK_REALM", "The name of your organization's realm, for example, `us0`. When you set the realm, telemetry is sent directly to the ingest endpoint of Splunk Observability Cloud, bypassing the Splunk Distribution of OpenTelemetry Collector.", string.Empty, "string", ExporterCategory),
            new("SPLUNK_ACCESS_TOKEN", "A Splunk authentication token that lets exporters send data directly to Splunk Observability Cloud. Unset by default. Required if you need to send data to the Splunk Observability Cloud ingest endpoint.", string.Empty, "string", ExporterCategory),

            // trace propagation
            new("OTEL_PROPAGATORS", "Comma-separated list of propagators for the tracer. The default value is `tracecontext,baggage`. Supported values are `b3multi`, `b3`, `tracecontext`, and `baggage`.", "tracecontext,baggage", "string", TracePropagationCategory),

            // samplers
            new("OTEL_TRACES_SAMPLER", "Sampler to use. The default value is `parentbased_always_on`. Supported values are `always_on`, `always_off`, `traceidratio`, `parentbased_always_on`, `parentbased_always_off`, and `parentbased_traceidratio`.", "parentbased_always_on", "string", SamplersCategory),
            new("OTEL_TRACES_SAMPLER_ARG", "Semicolon-separated list of rules for the `rules` sampler. The default value is `1.0` for `parentbased_always_on`.", "1.0", "string", SamplersCategory),

            // resource detectors
            new("OTEL_DOTNET_AUTO_RESOURCE_DETECTOR_ENABLED", "Activates or deactivates all resource detectors. The default values is `true`.", "true", "boolean", ResourceDetectorCategory),
            new("OTEL_DOTNET_AUTO_{DECTECTOR}_RESOURCE_DETECTOR_ENABLED", "Activates or deactivates a specific resource detector, where `{DETECTOR}` is the uppercase identifier of the resource detector you want to activate. Overrides `OTEL_DOTNET_AUTO_RESOURCE_DETECTOR_ENABLED`.", "depends on `OTEL_DOTNET_AUTO_RESOURCE_DETECTOR_ENABLED`", "boolean", ResourceDetectorCategory),

            // instrumentation
            new("OTEL_SERVICE_NAME", "Name of the service or application you're instrumenting. Takes precedence over the service name defined in the `OTEL_RESOURCE_ATTRIBUTES` variable.", string.Empty, "string", InstrumentationCategory),
            new("OTEL_RESOURCE_ATTRIBUTES", "Comma-separated list of resource attributes added to every reported span. For example, `key1=val1,key2=val2`.", string.Empty, "string", InstrumentationCategory),
            new("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "Comma-separated list of additional `System.Diagnostics.ActivitySource` names to be added to the tracer at startup. Use it to capture spans from manual instrumentation.", string.Empty, "string", InstrumentationCategory),
            new("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "Comma-separated list of additional `System.Diagnostics.Metrics.Meter` names to be added to the meter at the startup. Use it to capture custom metrics.", string.Empty, "string", InstrumentationCategory),
            new("OTEL_SPAN_ATTRIBUTE_COUNT_LIMIT", "Maximum number of attributes per span. Default value is unlimited.", string.Empty, "int", InstrumentationCategory),
            new("OTEL_SPAN_EVENT_COUNT_LIMIT", "Maximum number of events per span. Default value is unlimited.", string.Empty, "int", InstrumentationCategory),
            new("OTEL_SPAN_LINK_COUNT_LIMIT", "Maximum number of links per span. Default value is `1000`.", "1000", "int", InstrumentationCategory),
            new("OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT", "Maximum length of strings for attribute values. Values larger than the limit are truncated. Default value is `1200`. Empty values are treated as infinity.", "1200", "int", InstrumentationCategory),
            new("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES", "Comma-separated list of additional legacy source names to be added to the tracer at the startup. Use it to capture `System.Diagnostics.Activity` objects created without using the `System.Diagnostics.ActivitySource` API.", string.Empty, "string", InstrumentationCategory),

            // diagnostic logging
            new("OTEL_LOG_LEVEL", "Sets the logging level for instrumentation log messages. Possible values are `none`, `error`, `warn`, `info`, and `debug`. The default value is `info`. Can't be set using the web.config or app.config files.", "info", "string", DiagnosticCategory),
            new("OTEL_DOTNET_AUTO_LOG_DIRECTORY", "Directory of the .NET tracer logs. The default value is `/var/log/opentelemetry/dotnet` for Linux, and `%ProgramData%\\OpenTelemetry .NET AutoInstrumentation\\logs` for Windows. Can't be set using the web.config or app.config files.", string.Empty, "string", DiagnosticCategory),
            new("OTEL_DOTNET_AUTO_TRACES_CONSOLE_EXPORTER_ENABLED", "Whether the traces console exporter is activated. The default value is `false`.", "false", "boolean", DiagnosticCategory),
            new("OTEL_DOTNET_AUTO_METRICS_CONSOLE_EXPORTER_ENABLED", "Whether the metrics console exporter is activated. The default value is `false`.", "false", "boolean", DiagnosticCategory),
            new("OTEL_DOTNET_AUTO_LOGS_CONSOLE_EXPORTER_ENABLED", "Whether the logs console exporter is activated. The default value is `false`.The default value is `false`.", "false", "boolean", DiagnosticCategory),
            new("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "Whether the log state have to be formatted. The default value is `false`.", "false", "boolean", DiagnosticCategory),
        };

        return settings;
    }
}
