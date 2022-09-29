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

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using Splunk.OpenTelemetry.AutoInstrumentation.Integration.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Integration.Tests;

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
    public async Task SubmitsTraces()
    {
        var spans = await RunTestApplicationAsync();

        AssertAllSpansReceived(spans);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitMetrics()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        const int expectedMetricRequests = 1;

        using var collector = new MockMetricsCollector(Output);
        RunTestApplication(metricsAgentPort: collector.Port);
        var metricRequests = collector.WaitForMetrics(expectedMetricRequests, TimeSpan.FromSeconds(5));

        using (new AssertionScope())
        {
            metricRequests.Count.Should().Be(expectedMetricRequests);

            var resourceMetrics = metricRequests.Single().ResourceMetrics.Single();

            var expectedServiceNameAttribute = new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = ServiceName } };
            resourceMetrics.Resource.Attributes.Should().ContainEquivalentOf(expectedServiceNameAttribute);

            var customClientScope = resourceMetrics.ScopeMetrics.Single(rm => rm.Scope.Name.Equals("MyCompany.MyProduct.MyLibrary", StringComparison.OrdinalIgnoreCase));
            var myFruitCounterMetric = customClientScope.Metrics.FirstOrDefault(m => m.Name.Equals("MyFruitCounter", StringComparison.OrdinalIgnoreCase));
            myFruitCounterMetric.Should().NotBeNull();
            myFruitCounterMetric?.DataCase.Should().Be(global::OpenTelemetry.Proto.Metrics.V1.Metric.DataOneofCase.Sum);
            myFruitCounterMetric?.Sum.DataPoints.Count.Should().Be(1);

            var myFruitCounterAttributes = myFruitCounterMetric?.Sum.DataPoints[0].Attributes;
            myFruitCounterAttributes.Should().NotBeNull();
            myFruitCounterAttributes?.Count.Should().Be(1);
            myFruitCounterAttributes?.Single(a => a.Key == "name").Value.StringValue.Should().Be("apple");
        }
    }

    private static void AssertAllSpansReceived(IImmutableList<ResourceSpans> spans)
    {
        const int expectedSpanCount = 2;

        var spanList = spans
            .SelectMany(resourceSpans => resourceSpans.ScopeSpans)
            .SelectMany(scopeSpans => scopeSpans.Spans)
            .ToList();

        spanList.Count.Should().Be(expectedSpanCount, $"Expecting {expectedSpanCount} spans, received {spans.Count}");
        if (expectedSpanCount > 0)
        {
            var expectedServiceNameAttribute = new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = ServiceName } };
            foreach (var span in spans)
            {
                span.Resource.Attributes.Should().ContainEquivalentOf(expectedServiceNameAttribute);
            }

            spanList.Should().Contain(span => span.Name == "SayHello");
            spanList.Should().Contain(span => span.Name == "HTTP GET");
        }
    }

    private async Task<IImmutableList<ResourceSpans>> RunTestApplicationAsync(bool enableStartupHook = true)
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");

        using var agent = new MockTracesCollector(Output);
        RunTestApplication(agent.Port, enableStartupHook: enableStartupHook);
        return await agent.WaitForSpansAsync(2, TimeSpan.FromSeconds(5));
    }
}
