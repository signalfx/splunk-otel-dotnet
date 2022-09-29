// <copyright file="MockTracesCollector.cs" company="Splunk Inc.">
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
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Trace.V1;
using Splunk.OpenTelemetry.AutoInstrumentation.Integration.Tests.Helpers.Models;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Integration.Tests.Helpers;

public class MockTracesCollector : IDisposable
{
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(20);

    private readonly ITestOutputHelper _output;
    private readonly TestHttpListener _listener;

    public MockTracesCollector(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;
        _listener = new TestHttpListener(output, HandleHttpRequests, host);
    }

    public event EventHandler<EventArgs<HttpListenerContext>>? RequestReceived;

    public event EventHandler<EventArgs<ExportTraceServiceRequest>>? RequestDeserialized;

    public bool ShouldDeserializeTraces { get; set; } = true;

    public int Port { get => _listener.Port; }

    public IImmutableList<ResourceSpans> Spans { get; private set; } = ImmutableList<ResourceSpans>.Empty;

    public IImmutableList<NameValueCollection> RequestHeaders { get; private set; } = ImmutableList<NameValueCollection>.Empty;

    public async Task<IImmutableList<ResourceSpans>> WaitForSpansAsync(
        int count,
        TimeSpan? timeout = null)
    {
        timeout ??= DefaultWaitTimeout;
        var deadline = DateTime.Now.Add(timeout.Value);

        var spans = ImmutableList<Span>.Empty;

        while (DateTime.Now < deadline)
        {
            spans = Spans.SelectMany(resourceSpans => resourceSpans.ScopeSpans).SelectMany(scopeSpans => scopeSpans.Spans).ToImmutableList();

            if (spans.Count >= count)
            {
                break;
            }

            await Task.Delay(500);
        }

        return Spans;
    }

    public void Dispose()
    {
        WriteOutput($"Shutting down. Total traces requests received: '{Spans.Count}'");
        _listener.Dispose();
    }

    protected virtual void OnRequestReceived(HttpListenerContext context)
    {
        RequestReceived?.Invoke(this, new EventArgs<HttpListenerContext>(context));
    }

    protected virtual void OnRequestDeserialized(ExportTraceServiceRequest traceRequest)
    {
        RequestDeserialized?.Invoke(this, new EventArgs<ExportTraceServiceRequest>(traceRequest));
    }

    private void HandleHttpRequests(HttpListenerContext ctx)
    {
        OnRequestReceived(ctx);
        var rawUrl = ctx.Request.RawUrl;

        if (rawUrl != null)
        {
            if (rawUrl.Equals("/healthz", StringComparison.OrdinalIgnoreCase))
            {
                CreateHealthResponse(ctx);
                return;
            }

            if (rawUrl.Equals("/v1/traces", StringComparison.OrdinalIgnoreCase))
            {
                if (ShouldDeserializeTraces)
                {
                    var message = ExportTraceServiceRequest.Parser.ParseFrom(ctx.Request.InputStream);
                    OnRequestDeserialized(message);

                    var spans = message.ResourceSpans;

                    lock (this)
                    {
                        Spans = Spans.AddRange(spans);
                        RequestHeaders = RequestHeaders.Add(new NameValueCollection(ctx.Request.Headers));
                    }
                }

                // NOTE: HttpStreamRequest doesn't support Transfer-Encoding: Chunked
                // (Setting content-length avoids that)
                ctx.Response.ContentType = "application/x-protobuf";
                ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                var responseMessage = new ExportTraceServiceResponse();
                ctx.Response.ContentLength64 = responseMessage.CalculateSize();
                responseMessage.WriteTo(ctx.Response.OutputStream);
                ctx.Response.Close();
                return;
            }
        }

        // We received an unsupported request
        ctx.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
        ctx.Response.Close();
    }

    private void CreateHealthResponse(HttpListenerContext ctx)
    {
        ctx.Response.ContentType = "text/plain";
        var buffer = Encoding.UTF8.GetBytes("OK");
        ctx.Response.ContentLength64 = buffer.LongLength;
        ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
        ctx.Response.StatusCode = (int)HttpStatusCode.OK;
        ctx.Response.Close();
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockTracesCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }
}
