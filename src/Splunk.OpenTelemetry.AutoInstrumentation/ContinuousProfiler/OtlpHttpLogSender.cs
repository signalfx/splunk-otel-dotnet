// <copyright file="OtlpHttpLogSender.cs" company="Splunk Inc.">
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

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using OpenTelemetry;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;
using Splunk.OpenTelemetry.AutoInstrumentation.Proto.Logs.V1;

namespace Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

/// <summary>
/// Sends logs in binary-encoded protobuf format over HTTP.
/// </summary>
internal class OtlpHttpLogSender
{
    private static readonly ILogger Logger = new Logger();

    private readonly Uri _logsEndpointUrl;
    private readonly HttpClient _httpClient = new();

    public OtlpHttpLogSender(Uri logsEndpointUrl)
    {
        _logsEndpointUrl = logsEndpointUrl ?? throw new ArgumentNullException(nameof(logsEndpointUrl));
    }

    public void Send(LogsData logsData, CancellationToken cancellationToken)
    {
        try
        {
            // Prevents the exporter's operations from being instrumented.
            using var scope = SuppressInstrumentationScope.Begin();

            using var request = new HttpRequestMessage(HttpMethod.Post, _logsEndpointUrl);
            request.Content = new ExportLogsDataContent(logsData);
#if NET
            using var httpResponse = _httpClient.Send(request, cancellationToken);
#else
            using var httpResponse = _httpClient.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
#endif
            httpResponse.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"HTTP error sending thread samples to {_logsEndpointUrl}");
        }
    }

    private sealed class ExportLogsDataContent : HttpContent
    {
        private static readonly MediaTypeHeaderValue ProtobufMediaTypeHeader = new("application/x-protobuf");

        private readonly LogsData _logsData;

        public ExportLogsDataContent(LogsData logsData)
        {
            _logsData = logsData;
            Headers.ContentType = ProtobufMediaTypeHeader;
        }

#if NET
        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            SerializeToStreamInternal(stream);
        }
#endif

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            SerializeToStreamInternal(stream);
            return Task.CompletedTask;
        }

        protected override bool TryComputeLength(out long length)
        {
            // We can't know the length of the content being pushed to the output stream.
            length = -1;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SerializeToStreamInternal(Stream stream)
        {
            Vendors.ProtoBuf.Serializer.Serialize(stream, _logsData);
        }
    }
}
