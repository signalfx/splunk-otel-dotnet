// <copyright file="OpAmpReportDispatcher.cs" company="Splunk Inc.">
// Copyright Splunk Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Messages;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal enum OpAmpDispatchResult
{
    Canceled,
    Failed,
    ClientAcceptedWithoutEffectiveConfig,
    ClientAccepted
}

internal sealed class OpAmpReportDispatcher
{
    private static readonly ILogger Log = new Logger();
    private static readonly TimeSpan DefaultDispatchTimeout = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _dispatchTimeout;

    public OpAmpReportDispatcher()
        : this(DefaultDispatchTimeout)
    {
    }

    internal OpAmpReportDispatcher(TimeSpan dispatchTimeout)
    {
        if (dispatchTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(dispatchTimeout), "The OpAMP dispatch timeout must be positive.");
        }

        _dispatchTimeout = dispatchTimeout;
    }

    public async Task<OpAmpDispatchResult> DispatchEffectiveConfigAsync(
        OpAmpClient client,
        EffectiveConfigReporter effectiveConfigReporter,
        CancellationToken sessionCancellationToken)
    {
        try
        {
            sessionCancellationToken.ThrowIfCancellationRequested();
            var effectiveConfigFile = effectiveConfigReporter.BuildCurrentPayload();

            // Completion means the pinned client accepted the dispatch call. It does not acknowledge server receipt.
            await DispatchWithTimeoutAsync(
                cancellationToken => client.SendEffectiveConfigAsync(
                [effectiveConfigFile],
                cancellationToken),
                sessionCancellationToken).ConfigureAwait(false);
            return OpAmpDispatchResult.ClientAccepted;
        }
        catch (OperationCanceledException) when (sessionCancellationToken.IsCancellationRequested)
        {
            return OpAmpDispatchResult.Canceled;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to dispatch effective configuration through the OpAMP client.");
            return OpAmpDispatchResult.Failed;
        }
    }

    public async Task<OpAmpDispatchResult> DispatchFullStateReportAsync(
        OpAmpClient client,
        EffectiveConfigReporter? effectiveConfigReporter,
        RemoteConfigStatusReport? remoteConfigStatus,
        CancellationToken sessionCancellationToken)
    {
        if (sessionCancellationToken.IsCancellationRequested)
        {
            return OpAmpDispatchResult.Canceled;
        }

        var report = new FullStateReport
        {
            RemoteConfigStatus = remoteConfigStatus
        };
        var result = OpAmpDispatchResult.ClientAccepted;
        if (effectiveConfigReporter != null)
        {
            result = OpAmpDispatchResult.ClientAcceptedWithoutEffectiveConfig;
            try
            {
                report.EffectiveConfigFiles = [effectiveConfigReporter.BuildCurrentPayload()];
                result = OpAmpDispatchResult.ClientAccepted;
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to build effective configuration for full-state report: {ex.Message}");
            }
        }

        try
        {
            // The pinned client reports completion of the public dispatch call, not server acknowledgement.
            // Ordinary transport failures are handled internally and recover through OpAMP sequence/full-state flow.
            await DispatchWithTimeoutAsync(cancellationToken => client.SendFullStateReportAsync(report, cancellationToken), sessionCancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (OperationCanceledException) when (sessionCancellationToken.IsCancellationRequested)
        {
            return OpAmpDispatchResult.Canceled;
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to dispatch full state report through the OpAMP client: {ex.Message}");
            return OpAmpDispatchResult.Failed;
        }
    }

    private static void ObserveDetachedDispatch(Task dispatchTask)
    {
        _ = dispatchTask.ContinueWith(
            static completedTask => _ = completedTask.Exception,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    private async Task DispatchWithTimeoutAsync(
        Func<CancellationToken, Task> dispatchAsync,
        CancellationToken sessionCancellationToken)
    {
        using var dispatchCancellation = CancellationTokenSource.CreateLinkedTokenSource(sessionCancellationToken);
        using var deadlineCancellation = CancellationTokenSource.CreateLinkedTokenSource(sessionCancellationToken);
        var deadlineTask = Task.Delay(_dispatchTimeout, deadlineCancellation.Token);
        var dispatchTask = dispatchAsync(dispatchCancellation.Token);
        var completedTask = await Task.WhenAny(dispatchTask, deadlineTask).ConfigureAwait(false);

        if (ReferenceEquals(completedTask, dispatchTask))
        {
            deadlineCancellation.Cancel();

            await dispatchTask.ConfigureAwait(false);
            sessionCancellationToken.ThrowIfCancellationRequested();
            return;
        }

        try
        {
            dispatchCancellation.Cancel();
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to cancel an OpAMP dispatch after its deadline: {ex.Message}");
        }

        ObserveDetachedDispatch(dispatchTask);
        sessionCancellationToken.ThrowIfCancellationRequested();
        throw new TimeoutException($"The OpAMP dispatch exceeded the {_dispatchTimeout} deadline.");
    }
}
