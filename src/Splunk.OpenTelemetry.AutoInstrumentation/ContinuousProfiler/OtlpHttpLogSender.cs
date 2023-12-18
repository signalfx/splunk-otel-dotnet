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

    public OtlpHttpLogSender(Uri logsEndpointUrl)
    {
        _logsEndpointUrl = logsEndpointUrl ?? throw new ArgumentNullException(nameof(logsEndpointUrl));
    }

    public void Send(LogsData logsData)
    {
        HttpWebRequest httpWebRequest;

        try
        {
#pragma warning disable SYSLIB0014
            // TODO muted SYSLIB0014
            httpWebRequest = WebRequest.CreateHttp(_logsEndpointUrl);
#pragma warning restore SYSLIB0014
            httpWebRequest.ContentType = "application/x-protobuf";
            httpWebRequest.Method = "POST";
            // TODO how to disable tracing? httpWebRequest.Headers.Add(CommonHttpHeaderNames.TracingEnabled, "false");

            using var stream = httpWebRequest.GetRequestStream();
            Vendors.ProtoBuf.Serializer.Serialize(stream, logsData);
            stream.Flush();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Exception preparing request to send thread samples to {_logsEndpointUrl}");
            return;
        }

        try
        {
            using var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            if (httpWebResponse.StatusCode >= HttpStatusCode.OK && httpWebResponse.StatusCode < HttpStatusCode.MultipleChoices)
            {
                return;
            }

            Logger.Warning($"HTTP error sending thread samples to {_logsEndpointUrl}: {httpWebResponse.StatusCode}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Exception sending thread samples to {_logsEndpointUrl}");
        }
    }
}
