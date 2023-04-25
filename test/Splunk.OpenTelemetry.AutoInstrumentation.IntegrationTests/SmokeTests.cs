// <copyright file="SmokeTests.cs" company="Splunk Inc.">
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

// <copyright file="SmokeTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
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

using System.Reflection;
using Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests;

public class SmokeTests : TestHelper
{
    private const string ServiceName = "TestApplication.Smoke";

    public SmokeTests(ITestOutputHelper output)
        : base("Smoke", output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS", "HttpClient");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTraces()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("MyCompany.MyProduct.MyLibrary");
#if NETFRAMEWORK
        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpWebRequest");
#elif NET7_0_OR_GREATER
        collector.Expect("System.Net.Http");
#else
        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpClient");
#endif

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SdkOptionLimits()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect(
            "MyCompany.MyProduct.MyLibrary",
            span => span.Attributes.FirstOrDefault(att => att.Key == "long")?.Value.StringValue == new string('*', 12000));

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitMetrics()
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);
        collector.Expect("MyCompany.MyProduct.MyLibrary", metric => metric.Name == "MyFruitCounter");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication();

        collector.AssertExpectations();
    }

#if !NETFRAMEWORK
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitLogs()
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);
        collector.Expect(logRecord => System.Convert.ToString(logRecord.Body) == "{ \"stringValue\": \"SmokeTest app log\" }");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");
        EnableBytecodeInstrumentation();
        RunTestApplication();

        collector.AssertExpectations();
    }
#endif

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void TracesResource()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.ResourceExpector.Expect("service.name", ServiceName);
        collector.ResourceExpector.Expect("telemetry.sdk.name", "opentelemetry");
        collector.ResourceExpector.Expect("telemetry.sdk.language", "dotnet");
        collector.ResourceExpector.Expect("telemetry.sdk.version", typeof(global::OpenTelemetry.Resources.Resource).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version);
        collector.ResourceExpector.Expect("telemetry.auto.version", "0.7.0");
        collector.ResourceExpector.Expect("splunk.distro.version", typeof(Plugin).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication();

        collector.ResourceExpector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void MetricsResource()
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);
        collector.ResourceExpector.Expect("service.name", ServiceName);
        collector.ResourceExpector.Expect("telemetry.sdk.name", "opentelemetry");
        collector.ResourceExpector.Expect("telemetry.sdk.language", "dotnet");
        collector.ResourceExpector.Expect("telemetry.sdk.version", typeof(global::OpenTelemetry.Resources.Resource).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version);
        collector.ResourceExpector.Expect("telemetry.auto.version", "0.7.0");
        collector.ResourceExpector.Expect("splunk.distro.version", typeof(Plugin).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication();

        collector.ResourceExpector.AssertExpectations();
    }

#if !NETFRAMEWORK // The feature is not supported on .NET Framework
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void LogsResource()
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);
        collector.ResourceExpector.Expect("service.name", ServiceName);
        collector.ResourceExpector.Expect("telemetry.sdk.name", "opentelemetry");
        collector.ResourceExpector.Expect("telemetry.sdk.language", "dotnet");
        collector.ResourceExpector.Expect("telemetry.sdk.version", typeof(global::OpenTelemetry.Resources.Resource).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version);
        collector.ResourceExpector.Expect("telemetry.auto.version", "0.7.0");
        collector.ResourceExpector.Expect("splunk.distro.version", typeof(Plugin).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version);

        EnableBytecodeInstrumentation();
        RunTestApplication();

        collector.ResourceExpector.AssertExpectations();
    }
#endif
}
