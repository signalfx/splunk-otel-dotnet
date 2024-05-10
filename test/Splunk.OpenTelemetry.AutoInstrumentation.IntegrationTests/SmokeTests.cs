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

using FluentAssertions;
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

        collector.ResourceExpector.ExpectDistributionResources(ServiceName);

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

        collector.ResourceExpector.ExpectDistributionResources(ServiceName);

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

        collector.ResourceExpector.ExpectDistributionResources(ServiceName);

        EnableBytecodeInstrumentation();
        RunTestApplication();

        collector.ResourceExpector.AssertExpectations();
    }
#endif

    [Fact]
    public void ManagedLogsHaveNoSensitiveData()
    {
        var tempLogsDirectory = DirectoryHelpers.CreateTempDirectory();
        var secretIdentificators = new[] { "api", "Token", "SeCrEt", "KEY", "PASSWORD", "PASS", "PWD", "HEADER", "CREDENTIALS" };

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY", tempLogsDirectory.FullName);
        SetEnvironmentVariable("OTEL_LOG_LEVEL", "debug");

        foreach (var item in secretIdentificators)
        {
            SetEnvironmentVariable($"OTEL_{item}_VALUE", "this is secret!");
        }

        try
        {
            RunTestApplication();

            var managedLog = tempLogsDirectory.GetFiles("otel-dotnet-auto-*-Splunk-*.log").Single();
            var managedLogContent = File.ReadAllText(managedLog.FullName);
            managedLogContent.Should().NotBeNullOrWhiteSpace();

            var environmentVariables = ParseSettingsLog(managedLogContent, "Environment Variables:");
            VerifyVariables(environmentVariables);

#if NETFRAMEWORK
            var appSettings = ParseSettingsLog(managedLogContent, "AppSettings:");
            VerifyVariables(appSettings);
#endif
        }
        finally
        {
            tempLogsDirectory.Delete(true);
        }

        void VerifyVariables(ICollection<KeyValuePair<string, string>> options)
        {
            options.Should().NotBeEmpty();

            var secretVariables = options
                .Where(item => secretIdentificators.Any(i => item.Key.Contains(i)))
                .ToList();

            secretVariables.Should().NotBeEmpty();
            secretVariables.Should().AllSatisfy(secret => secret.Value.Should().Be("<hidden>"));
        }
    }

    private static ICollection<KeyValuePair<string, string>> ParseSettingsLog(string log, string marker)
    {
        var lines = log.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var variables = lines
            .SkipWhile(x => !x.EndsWith(marker))
            .Skip(1)
            .TakeWhile(x => x.StartsWith("\t"))
            .Select(x => x.Trim())
            .ToEnvironmentVariablesList();

        return variables;
    }
}
