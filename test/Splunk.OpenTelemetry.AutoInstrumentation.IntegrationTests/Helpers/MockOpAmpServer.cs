// <copyright file="MockOpAmpServer.cs" company="Splunk Inc.">
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

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Buffers;
using Microsoft.AspNetCore.Http;
#endif

using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Text;
using Google.Protobuf;
using OpAmp.Proto.V1;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;

internal sealed class MockOpAmpServer : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestHttpServer _listener;

    private readonly List<Expectation> _expectations = new();
    private readonly BlockingCollection<AgentToServer> _frames = new(10);
    private readonly object _effectiveConfigFramesLock = new();
    private readonly List<EffectiveConfigFrameSnapshot> _effectiveConfigFrames = [];
    private RemoteConfigResponse? _remoteConfigResponse;

    public MockOpAmpServer(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;
#if NETFRAMEWORK
        _listener = new TestHttpServer(output, HandleHttpRequests, host, "/v1/opamp/");
#else
        _listener = new TestHttpServer(output, nameof(MockOpAmpServer), new PathHandler(HandleHttpRequests, "/v1/opamp"));
#endif
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port => _listener.Port;

    public void Expect(Func<AgentToServer, bool>? predicate = null, string? description = null)
    {
        predicate ??= _ => true;
        _expectations.Add(new Expectation(predicate, description));
    }

    public void AssertExpectations(TimeSpan? timeout = null)
    {
        if (_expectations.Count == 0)
        {
            throw new InvalidOperationException("Expectations were not set.");
        }

        var missingExpectations = new List<Expectation>(_expectations);
        var expectationsMet = new List<AgentToServer>();
        var additionalEntries = new List<AgentToServer>();

        timeout ??= TestTimeout.Expectation;
        using var cts = new CancellationTokenSource();

        try
        {
            cts.CancelAfter(timeout.Value);
            foreach (var frame in _frames.GetConsumingEnumerable(cts.Token))
            {
                var found = false;
                for (var i = missingExpectations.Count - 1; i >= 0; i--)
                {
                    var missingExpectation = missingExpectations[i];

                    if (!missingExpectation.Predicate(frame))
                    {
                        continue;
                    }

                    expectationsMet.Add(frame);
                    missingExpectations.RemoveAt(i);
                    found = true;
                    break;
                }

                if (!found)
                {
                    additionalEntries.Add(frame);
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
            FailExpectations(missingExpectations, expectationsMet, additionalEntries);
        }
        catch (OperationCanceledException)
        {
            FailExpectations(missingExpectations, expectationsMet, additionalEntries);
        }
    }

    public void AssertEmpty(TimeSpan? timeout = null)
    {
        timeout ??= TestTimeout.NoExpectation;
        if (_frames.TryTake(out var frame, timeout.Value))
        {
            Assert.Fail($"Expected nothing, but got: {frame}");
        }
    }

    public void OfferRemoteConfig(
        string body,
        string contentType = "application/yaml",
        string fileName = "splunk.remote.config",
        string configHash = "splunk.remote.config")
    {
        _remoteConfigResponse = new RemoteConfigResponse(body, contentType, fileName, configHash);
    }

    public void ExpectEffectiveConfigPayload(
        string fileName,
        string contentType,
        Func<string, bool> payloadPredicate,
        string? description = null)
    {
        Expect(
            frame => TryReadEffectiveConfigPayload(frame, fileName, contentType, out var payload) && payloadPredicate(payload),
            description);
    }

    public void AssertEffectiveConfigPayloads(
        string fileName,
        string contentType,
        Func<string, bool> finalPayloadPredicate)
    {
        var frames = GetEffectiveConfigFrames();
        Assert.NotEmpty(frames);

        var fileCounts = frames.Select(frame => frame.Files.Count).ToArray();
        if (fileCounts.Any(count => count != 1))
        {
            Assert.Fail($"Expected each effective config frame to contain exactly one file, but received file counts: {string.Join(", ", fileCounts)}.");
        }

        var files = frames.SelectMany(frame => frame.Files).ToArray();
        Assert.All(files, file =>
        {
            Assert.Equal(fileName, file.Name);
            Assert.Equal(contentType, file.ContentType);
            Assert.False(string.IsNullOrWhiteSpace(file.Body));
        });

        var duplicatePayloads = files
            .GroupBy(file => file.Body, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        Assert.Empty(duplicatePayloads);

        var finalPayload = frames
            .OrderBy(frame => frame.SequenceNum)
            .Last()
            .Files[0]
            .Body;
        Assert.True(finalPayloadPredicate(finalPayload), "The final effective config payload did not contain the expected values.");
    }

    public void Dispose()
    {
        WriteOutput("Shutting down.");
        _listener.Dispose();
        _frames.Dispose();
    }

    private static bool TryReadEffectiveConfigPayload(AgentToServer frame, string fileName, string contentType, out string payload)
    {
        payload = string.Empty;

        var configMap = frame.EffectiveConfig?.ConfigMap?.ConfigMap;
        if (configMap == null ||
            configMap.Count != 1 ||
            !configMap.TryGetValue(fileName, out var configFile) ||
            configFile.ContentType != contentType)
        {
            return false;
        }

        payload = configFile.Body.ToStringUtf8();
        return !string.IsNullOrWhiteSpace(payload);
    }

    private static void FailExpectations(
        List<Expectation> missingExpectations,
        List<AgentToServer> expectationsMet,
        List<AgentToServer> additionalEntries)
    {
        var message = new StringBuilder();
        message.AppendLine();

        message.AppendLine("Missing expectations:");
        foreach (var expectation in missingExpectations)
        {
            message.AppendLine(string.Format(CultureInfo.InvariantCulture, "  - \"{0}\"", expectation.Description));
        }

        message.AppendLine("Entries meeting expectations:");
        foreach (var frame in expectationsMet)
        {
            message.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"{0}\"", frame));
        }

        message.AppendLine("Additional entries:");
        foreach (var frame in additionalEntries)
        {
            message.AppendLine(string.Format(CultureInfo.InvariantCulture, "  + \"{0}\"", frame));
        }

        Assert.Fail(message.ToString());
    }

    private byte[] GenerateResponse(AgentToServer frame)
    {
        var content = "This is a mock server frame for testing purposes.";
        var responseFrame = new ServerToAgent
        {
            InstanceUid = frame.InstanceUid,
            Capabilities = (ulong)ServerCapabilities.AcceptsEffectiveConfig,
            CustomMessage = new CustomMessage
            {
                Data = ByteString.CopyFromUtf8(content),
                Type = "Utf8String",
            },
        };

        if (_remoteConfigResponse != null)
        {
            responseFrame.Capabilities |= (ulong)ServerCapabilities.OffersRemoteConfig;
            responseFrame.RemoteConfig = new AgentRemoteConfig
            {
                Config = new AgentConfigMap
                {
                    ConfigMap =
                    {
                        [_remoteConfigResponse.FileName] = new AgentConfigFile
                        {
                            Body = ByteString.CopyFromUtf8(_remoteConfigResponse.Body),
                            ContentType = _remoteConfigResponse.ContentType,
                        }
                    }
                },
                ConfigHash = ByteString.CopyFromUtf8(_remoteConfigResponse.ConfigHash)
            };
        }

        return responseFrame.ToByteArray();
    }

#if NETFRAMEWORK
    private void HandleHttpRequests(HttpListenerContext ctx)
    {
        var frame = AgentToServer.Parser.ParseFrom(ctx.Request.InputStream);
        RecordEffectiveConfigFrame(frame);
        _frames.Add(frame);

        var response = GenerateResponse(frame);

        ctx.Response.StatusCode = (int)HttpStatusCode.OK;
        ctx.Response.ContentType = "application/x-protobuf";
        ctx.Response.OutputStream.Write(response, 0, response.Length);
        ctx.Response.OutputStream.Close();
    }
#else
    private async Task<AgentToServer?> ProcessReceiveAsync(HttpRequest request)
    {
        var reader = request.BodyReader;
        var messageBuffer = new ArrayBufferWriter<byte>();

        while (true)
        {
            var result = await reader.ReadAsync().ConfigureAwait(false);
            var buffer = result.Buffer;

            if (result.IsCanceled)
            {
                reader.AdvanceTo(buffer.End);
                return null;
            }

            foreach (var segment in buffer)
            {
                messageBuffer.Write(segment.Span);
            }

            reader.AdvanceTo(buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

        return AgentToServer.Parser.ParseFrom(messageBuffer.WrittenSpan);
    }

    private async Task HandleHttpRequests(HttpContext ctx)
    {
        var frame = await ProcessReceiveAsync(ctx.Request).ConfigureAwait(false);
        if (frame == null)
        {
            return;
        }

        RecordEffectiveConfigFrame(frame);
        _frames.Add(frame);

        var response = GenerateResponse(frame);

        ctx.Response.StatusCode = (int)HttpStatusCode.OK;
        ctx.Response.ContentType = "application/x-protobuf";
        await ctx.Response.Body.WriteAsync(response).ConfigureAwait(false);
        await ctx.Response.CompleteAsync().ConfigureAwait(false);
    }
#endif

    private void RecordEffectiveConfigFrame(AgentToServer frame)
    {
        var effectiveConfig = frame.EffectiveConfig;
        if (effectiveConfig == null)
        {
            return;
        }

        var files = effectiveConfig.ConfigMap?.ConfigMap
            .Select(file => new EffectiveConfigFileSnapshot(file.Key, file.Value.ContentType, file.Value.Body.ToStringUtf8()))
            .ToArray() ?? [];

        lock (_effectiveConfigFramesLock)
        {
            _effectiveConfigFrames.Add(new EffectiveConfigFrameSnapshot(frame.SequenceNum, files));
        }
    }

    private IReadOnlyList<EffectiveConfigFrameSnapshot> GetEffectiveConfigFrames()
    {
        lock (_effectiveConfigFramesLock)
        {
            return _effectiveConfigFrames.ToArray();
        }
    }

    private void WriteOutput(string msg)
    {
        _output.WriteLine($"[{nameof(MockOpAmpServer)}]: {msg}");
    }

    private sealed class EffectiveConfigFrameSnapshot
    {
        public EffectiveConfigFrameSnapshot(ulong sequenceNum, IReadOnlyList<EffectiveConfigFileSnapshot> files)
        {
            SequenceNum = sequenceNum;
            Files = files;
        }

        public ulong SequenceNum { get; }

        public IReadOnlyList<EffectiveConfigFileSnapshot> Files { get; }
    }

    private sealed class EffectiveConfigFileSnapshot
    {
        public EffectiveConfigFileSnapshot(string name, string contentType, string body)
        {
            Name = name;
            ContentType = contentType;
            Body = body;
        }

        public string Name { get; }

        public string ContentType { get; }

        public string Body { get; }
    }

    private sealed class RemoteConfigResponse
    {
        public RemoteConfigResponse(string body, string contentType, string fileName, string configHash)
        {
            Body = body;
            ContentType = contentType;
            FileName = fileName;
            ConfigHash = configHash;
        }

        public string Body { get; }

        public string ContentType { get; }

        public string FileName { get; }

        public string ConfigHash { get; }
    }

    private sealed class Expectation
    {
        public Expectation(Func<AgentToServer, bool> predicate, string? description)
        {
            Predicate = predicate;
            Description = description;
        }

        public Func<AgentToServer, bool> Predicate { get; }

        public string? Description { get; }
    }
}
