// <copyright file="MockSpansCollector.cs" company="Splunk Inc.">
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

// <copyright file="MockSpansCollector.cs" company="OpenTelemetry Authors">
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

#nullable disable

#if NETFRAMEWORK
using System.Net;
#else
using Microsoft.AspNetCore.Http;
#endif
using System.Collections.Concurrent;
using System.Text;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;

public class MockSpansCollector : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestHttpServer _listener;

    private readonly BlockingCollection<Collected> _spans = new(100); // bounded to avoid memory leak
    private readonly List<Expectation> _expectations = [];

    public MockSpansCollector(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;

#if NETFRAMEWORK
        _listener = new TestHttpServer(output, HandleHttpRequests, host, "/v1/traces/");
#else
        _listener = new TestHttpServer(output, HandleHttpRequests, "/v1/traces");
#endif
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

    public OtlpResourceExpector ResourceExpector { get; } = new();

    public void Dispose()
    {
        WriteOutput("Shutting down.");
        ResourceExpector.Dispose();
        _spans.Dispose();
        _listener.Dispose();
    }

    public void Expect(string instrumentationScopeName, Func<Span, bool> predicate = null, string description = null)
    {
        predicate ??= x => true;
        description ??= "<no description>";

        _expectations.Add(new Expectation { InstrumentationScopeName = instrumentationScopeName, Predicate = predicate, Description = description });
    }

    public void AssertExpectations(TimeSpan? timeout = null)
    {
        if (_expectations.Count == 0)
        {
            throw new InvalidOperationException("Expectations were not set");
        }

        var missingExpectations = new List<Expectation>(_expectations);
        var expectationsMet = new List<Collected>();
        var additionalEntries = new List<Collected>();

        timeout ??= Timeout.Expectation;
        var cts = new CancellationTokenSource();

        try
        {
            cts.CancelAfter(timeout.Value);
            foreach (var resourceSpans in _spans.GetConsumingEnumerable(cts.Token))
            {
                var found = false;
                for (var i = missingExpectations.Count - 1; i >= 0; i--)
                {
                    if (missingExpectations[i].InstrumentationScopeName != resourceSpans.InstrumentationScopeName)
                    {
                        continue;
                    }

                    if (!missingExpectations[i].Predicate(resourceSpans.Span))
                    {
                        continue;
                    }

                    expectationsMet.Add(resourceSpans);
                    missingExpectations.RemoveAt(i);
                    found = true;
                    break;
                }

                if (!found)
                {
                    additionalEntries.Add(resourceSpans);
                    continue;
                }

                if (missingExpectations.Count == 0)
                {
                    return;
                }
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            // CancelAfter called with non-positive value
            FailExpectations(missingExpectations, expectationsMet, additionalEntries);
        }
        catch (OperationCanceledException)
        {
            // timeout
            FailExpectations(missingExpectations, expectationsMet, additionalEntries);
        }
    }

    public void AssertEmpty(TimeSpan? timeout = null)
    {
        timeout ??= Timeout.NoExpectation;
        if (_spans.TryTake(out var resourceSpan, timeout.Value))
        {
            Assert.Fail($"Expected nothing, but got: {resourceSpan}");
        }
    }

    private static void FailExpectations(
        List<Expectation> missingExpectations,
        List<Collected> expectationsMet,
        List<Collected> additionalEntries)
    {
        var message = new StringBuilder();
        message.AppendLine();

        message.AppendLine("Missing expectations:");
        foreach (var logline in missingExpectations)
        {
            message.AppendLine($"  - \"{logline.Description}\"");
        }

        message.AppendLine("Entries meeting expectations:");
        foreach (var logline in expectationsMet)
        {
            message.AppendLine($"    \"{logline}\"");
        }

        message.AppendLine("Additional entries:");
        foreach (var logline in additionalEntries)
        {
            message.AppendLine($"  + \"{logline}\"");
        }

        Assert.Fail(message.ToString());
    }

#if NETFRAMEWORK
    private void HandleHttpRequests(HttpListenerContext ctx)
    {
        var traceMessage = ExportTraceServiceRequest.Parser.ParseFrom(ctx.Request.InputStream);
        HandleTraceMessage(traceMessage);

        ctx.GenerateEmptyProtobufResponse<ExportTraceServiceResponse>();
    }
#else
    private async Task HandleHttpRequests(HttpContext ctx)
    {
        using var bodyStream = await ctx.ReadBodyToMemoryAsync();
        var traceMessage = ExportTraceServiceRequest.Parser.ParseFrom(bodyStream);
        HandleTraceMessage(traceMessage);

        await ctx.GenerateEmptyProtobufResponseAsync<ExportTraceServiceResponse>();
    }
#endif

    private void HandleTraceMessage(ExportTraceServiceRequest traceMessage)
    {
        foreach (var resourceSpan in traceMessage.ResourceSpans ?? Enumerable.Empty<ResourceSpans>())
        {
            ResourceExpector.Collect(resourceSpan.Resource);
            foreach (var scopeSpans in resourceSpan.ScopeSpans ?? Enumerable.Empty<ScopeSpans>())
            {
                foreach (var span in scopeSpans.Spans ?? Enumerable.Empty<Span>())
                {
                    _spans.Add(new Collected
                    {
                        InstrumentationScopeName = scopeSpans.Scope.Name,
                        Span = span
                    });
                }
            }
        }
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockSpansCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }

    private class Expectation
    {
        public string InstrumentationScopeName { get; set; }

        public Func<Span, bool> Predicate { get; set; }

        public string Description { get; set; }
    }

    private class Collected
    {
        public string InstrumentationScopeName { get; set; }

        public Span Span { get; set; } // protobuf type

        public override string ToString()
        {
            return $"InstrumentationScopeName = {InstrumentationScopeName}, Span = {Span}";
        }
    }
}
