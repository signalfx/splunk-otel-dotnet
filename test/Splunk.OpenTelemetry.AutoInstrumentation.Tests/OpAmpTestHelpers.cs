// <copyright file="OpAmpTestHelpers.cs" company="Splunk Inc.">
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

#if NET
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using OpenTelemetry.OpAmp.Client.Messages;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

internal static class OpAmpTestHelpers
{
    private static readonly ConstructorInfo FlagsMessageConstructor = typeof(FlagsMessage)
        .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
        .Single();

    public static FlagsMessage CreateFlagsMessage(ServerSentFlags flags)
    {
        var constructorParameterType = FlagsMessageConstructor.GetParameters()[0].ParameterType;
        var constructorArgument = Enum.ToObject(constructorParameterType, Convert.ToUInt64(flags));
        return (FlagsMessage)FlagsMessageConstructor.Invoke([constructorArgument]);
    }

    public static async Task WaitForCompletionAsync(Task task)
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(task, completedTask);
        await task;
    }

    internal sealed class ManuallyReleasedDelay
    {
        private readonly object _lock = new();
        private readonly Queue<TaskCompletionSource<bool>> _pendingDelays = new();
        private readonly SemaphoreSlim _scheduledDelays = new(0);
        private readonly bool _completeFullStateCooldownImmediately;

        public ManuallyReleasedDelay()
            : this(completeFullStateCooldownImmediately: false)
        {
        }

        private ManuallyReleasedDelay(bool completeFullStateCooldownImmediately)
        {
            _completeFullStateCooldownImmediately = completeFullStateCooldownImmediately;
        }

        public static ManuallyReleasedDelay ForILoggerBatching()
        {
            return new ManuallyReleasedDelay(completeFullStateCooldownImmediately: true);
        }

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            if (_completeFullStateCooldownImmediately && delay == TimeSpan.FromSeconds(5))
            {
                return Task.CompletedTask;
            }

            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));

            lock (_lock)
            {
                _pendingDelays.Enqueue(completion);
            }

            _scheduledDelays.Release();
            return completion.Task;
        }

        public async Task ReleaseNextAsync()
        {
            Assert.True(await _scheduledDelays.WaitAsync(TimeSpan.FromSeconds(5)));

            TaskCompletionSource<bool> completion;
            lock (_lock)
            {
                completion = _pendingDelays.Dequeue();
            }

            completion.TrySetResult(true);
        }

        public async Task WaitUntilScheduledAsync()
        {
            Assert.True(await _scheduledDelays.WaitAsync(TimeSpan.FromSeconds(5)));
            _scheduledDelays.Release();
        }
    }

    internal sealed class OpAmpHttpRequestProbe : HttpMessageHandler
    {
        private readonly SemaphoreSlim _requestObserved = new(0);
        private readonly TaskCompletionSource<bool> _releaseFirstRequest = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConcurrentQueue<byte[]> _requestBodies = new();
        private readonly bool _blockFirstRequest;
        private readonly Func<int, CancellationToken, Task>? _onRequest;
        private int _requestCount;

        public OpAmpHttpRequestProbe(
            bool blockFirstRequest = false,
            Func<int, CancellationToken, Task>? onRequest = null)
        {
            _blockFirstRequest = blockFirstRequest;
            _onRequest = onRequest;
        }

        public int Count => Volatile.Read(ref _requestCount);

        public byte[] GetRequestBody(int requestNumber)
        {
            var requestBodies = _requestBodies.ToArray();
            Assert.InRange(requestNumber, 1, requestBodies.Length);
            return requestBodies[requestNumber - 1];
        }

        public void ReleaseFirstRequest()
        {
            _releaseFirstRequest.TrySetResult(true);
        }

        public async Task WaitForCountAsync(int expectedCount)
        {
            while (Count < expectedCount)
            {
                Assert.True(await _requestObserved.WaitAsync(TimeSpan.FromSeconds(5)));
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content == null)
            {
                throw new InvalidOperationException("The OpAMP request did not contain a protobuf frame.");
            }

            var requestBody = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            var requestNumber = Interlocked.Increment(ref _requestCount);
            _requestBodies.Enqueue(requestBody);
            _requestObserved.Release();

            if (_blockFirstRequest && requestNumber == 1)
            {
                await _releaseFirstRequest.Task.ConfigureAwait(false);
            }

            if (_onRequest != null)
            {
                await _onRequest(requestNumber, cancellationToken).ConfigureAwait(false);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _requestObserved.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
#endif
