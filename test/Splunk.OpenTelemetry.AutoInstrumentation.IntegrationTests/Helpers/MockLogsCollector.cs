// <copyright file="MockLogsCollector.cs" company="Splunk Inc.">
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

// <copyright file="MockLogsCollector.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Logs.V1;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;

public class MockLogsCollector : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestHttpServer _listener;
    private readonly BlockingCollection<LogRecord> _logs = new(100); // bounded to avoid memory leak
    private readonly List<Expectation> _expectations = [];

    public MockLogsCollector(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;

#if NETFRAMEWORK
        _listener = new(output, HandleHttpRequests, host, "/v1/logs/");
#else
        _listener = new(output, HandleHttpRequests, "/v1/logs");
#endif
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

    public OtlpResourceExpector ResourceExpector { get; } = new();

    public void Dispose()
    {
        WriteOutput($"Shutting down. Total logs requests received: '{_logs.Count}'");
        ResourceExpector.Dispose();
        _logs.Dispose();
        _listener.Dispose();
    }

    public void Expect(Func<LogRecord, bool> predicate, string description = null)
    {
        description ??= "<no description>";

        _expectations.Add(new Expectation { Predicate = predicate, Description = description });
    }

    public void AssertExpectations(TimeSpan? timeout = null)
    {
        if (_expectations.Count == 0)
        {
            throw new InvalidOperationException("Expectations were not set");
        }

        var missingExpectations = new List<Expectation>(_expectations);
        var expectationsMet = new List<LogRecord>();
        var additionalEntries = new List<LogRecord>();

        timeout ??= Timeout.Expectation;
        var cts = new CancellationTokenSource();

        try
        {
            cts.CancelAfter(timeout.Value);
            foreach (var logRecord in _logs.GetConsumingEnumerable(cts.Token))
            {
                bool found = false;
                for (int i = missingExpectations.Count - 1; i >= 0; i--)
                {
                    if (!missingExpectations[i].Predicate(logRecord))
                    {
                        continue;
                    }

                    expectationsMet.Add(logRecord);
                    missingExpectations.RemoveAt(i);
                    found = true;
                    break;
                }

                if (!found)
                {
                    additionalEntries.Add(logRecord);
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
        if (_logs.TryTake(out var logRecord, timeout.Value))
        {
            Assert.Fail($"Expected nothing, but got: {logRecord}");
        }
    }

    private static void FailExpectations(
        List<Expectation> missingExpectations,
        List<LogRecord> expectationsMet,
        List<LogRecord> additionalEntries)
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
        var logsMessage = ExportLogsServiceRequest.Parser.ParseFrom(ctx.Request.InputStream);
        HandleLogsMessage(logsMessage);

        ctx.GenerateEmptyProtobufResponse<ExportLogsServiceResponse>();
    }
#else
    private async Task HandleHttpRequests(HttpContext ctx)
    {
        using var bodyStream = await ctx.ReadBodyToMemoryAsync();
        var metricsMessage = ExportLogsServiceRequest.Parser.ParseFrom(bodyStream);
        HandleLogsMessage(metricsMessage);

        await ctx.GenerateEmptyProtobufResponseAsync<ExportLogsServiceResponse>();
    }
#endif

    private void HandleLogsMessage(ExportLogsServiceRequest logsMessage)
    {
        foreach (var resourceLogs in logsMessage.ResourceLogs ?? Enumerable.Empty<ResourceLogs>())
        {
            ResourceExpector.Collect(resourceLogs.Resource);
            foreach (var scopeLogs in resourceLogs.ScopeLogs ?? Enumerable.Empty<ScopeLogs>())
            {
                foreach (var logRecord in scopeLogs.LogRecords ?? Enumerable.Empty<LogRecord>())
                {
                    _logs.Add(logRecord);
                }
            }
        }
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockLogsCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }

    private class Expectation
    {
        public Func<LogRecord, bool> Predicate { get; set; }

        public string Description { get; set; }
    }
}
