// <copyright file="MockLContinuousProfilerCollector.cs" company="Splunk Inc.">
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

// <copyright file="MockLContinuousProfilerCollector.cs" company="OpenTelemetry Authors">
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
#if NET6_0_OR_GREATER

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Proto.Collector.Logs.V1;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;

public class MockLContinuousProfilerCollector : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestHttpServer _listener;
    private readonly BlockingCollection<ExportLogsServiceRequest> _logs = new(100); // bounded to avoid memory leak

    public MockLContinuousProfilerCollector(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;

        _listener = new(output, HandleHttpRequests, "/v1/logs");
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

    public ExportLogsServiceRequest[] GetAllLogs()
    {
        return _logs.ToArray();
    }

    public void Dispose()
    {
        WriteOutput($"Shutting down. Total logs requests received: '{_logs.Count}'");
        _logs.Dispose();
        _listener.Dispose();
    }

    private async Task HandleHttpRequests(HttpContext ctx)
    {
        using var bodyStream = await ctx.ReadBodyToMemoryAsync();
        var metricsMessage = ExportLogsServiceRequest.Parser.ParseFrom(bodyStream);
        HandleLogsMessage(metricsMessage);

        await ctx.GenerateEmptyProtobufResponseAsync<ExportLogsServiceResponse>();
    }

    private void HandleLogsMessage(ExportLogsServiceRequest logsMessage)
    {
        _logs.Add(logsMessage);
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockLContinuousProfilerCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }
}
#endif
