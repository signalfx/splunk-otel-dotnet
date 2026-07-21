// <copyright file="OpAmpReportingPump.cs" company="Splunk Inc.">
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
using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal sealed class OpAmpReportingPump : IOpAmpListener<FlagsMessage>
{
    private static readonly ILogger Log = new Logger();
    private static readonly TimeSpan FullStateReportCooldown = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ILoggerEffectiveConfigBatchDelay = TimeSpan.FromSeconds(1);

    private readonly object _lock = new();
    private readonly OpAmpClient _client;
    private readonly EffectiveConfigReporter? _effectiveConfigReporter;
    private readonly OpAmpReportDispatcher _reportDispatcher;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly SemaphoreSlim _wakeSignal = new(0, 1);
    private readonly CancellationTokenSource _reportingCancellation = new();
    private readonly CancellationToken _reportingCancellationToken;
    private bool _started;
    private bool _instrumentationInitialized;
    private bool _fullStateReportPending;
    private bool _iLoggerEffectiveConfigUpdatePending;
    private bool _wakeQueued;
    private bool _stopped;
    private Task _fullStateReportCooldownTask = Task.CompletedTask;

    public OpAmpReportingPump(
        OpAmpClient client,
        EffectiveConfigReporter? effectiveConfigReporter,
        bool instrumentationInitialized)
        : this(
            client,
            effectiveConfigReporter,
            new OpAmpReportDispatcher(),
            Task.Delay,
            instrumentationInitialized)
    {
    }

    internal OpAmpReportingPump(
        OpAmpClient client,
        EffectiveConfigReporter? effectiveConfigReporter,
        OpAmpReportDispatcher reportDispatcher,
        Func<TimeSpan, CancellationToken, Task> delayAsync,
        bool instrumentationInitialized)
    {
        _client = client;
        _effectiveConfigReporter = effectiveConfigReporter;
        _reportDispatcher = reportDispatcher;
        _delayAsync = delayAsync;
        _instrumentationInitialized = instrumentationInitialized;
        _fullStateReportPending = true;
        _reportingCancellationToken = _reportingCancellation.Token;
    }

    private enum ReportingWorkKind
    {
        None,
        FullStateReport,
        ILoggerEffectiveConfigUpdate
    }

    public bool IsForClient(OpAmpClient client) => ReferenceEquals(_client, client);

    public void Start()
    {
        lock (_lock)
        {
            if (_stopped || _started)
            {
                return;
            }

            _client.Subscribe<FlagsMessage>(this);
            _started = true;
            if (_instrumentationInitialized)
            {
                SignalWorkerLocked();
            }
        }

        _ = RunAsync();
    }

    public void HandleMessage(FlagsMessage message)
    {
        if ((message.Flags & ServerSentFlags.ReportFullState) != 0)
        {
            RequestFullStateReport();
        }
    }

    public void MarkInstrumentationInitialized()
    {
        lock (_lock)
        {
            if (_stopped || _instrumentationInitialized)
            {
                return;
            }

            _instrumentationInitialized = true;
            SignalWorkerLocked();
        }
    }

    public void NotifyILoggerEffectiveConfigChanged()
    {
        NotifyEffectiveConfigChanged();
    }

    public void NotifyEffectiveConfigChanged()
    {
        lock (_lock)
        {
            if (_stopped)
            {
                return;
            }

            _iLoggerEffectiveConfigUpdatePending = true;
            SignalWorkerLocked();
        }
    }

    public void Stop()
    {
        bool unsubscribe;
        lock (_lock)
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;
            _fullStateReportPending = false;
            _iLoggerEffectiveConfigUpdatePending = false;
            unsubscribe = _started;
            _started = false;
        }

        if (unsubscribe)
        {
            TryUnsubscribe();
        }

        CancelAndDispose(_reportingCancellation);
    }

    private static void CancelAndDispose(CancellationTokenSource cancellation)
    {
        try
        {
            cancellation.Cancel();
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to cancel stopped OpAMP reporting work: {ex.Message}");
        }
        finally
        {
            cancellation.Dispose();
        }
    }

    private void RequestFullStateReport()
    {
        lock (_lock)
        {
            if (_stopped)
            {
                return;
            }

            _fullStateReportPending = true;
            SignalWorkerLocked();
        }
    }

    private async Task RunAsync()
    {
        try
        {
            while (await WaitForSignalAsync().ConfigureAwait(false))
            {
                await ProcessNextWorkAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_reportingCancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Log.Error(ex, "The OpAMP reporting pump stopped unexpectedly.");
        }
    }

    private async Task<bool> WaitForSignalAsync()
    {
        try
        {
            await _wakeSignal.WaitAsync(_reportingCancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_reportingCancellationToken.IsCancellationRequested)
        {
            return false;
        }

        lock (_lock)
        {
            _wakeQueued = false;
            return !_stopped;
        }
    }

    private async Task ProcessNextWorkAsync()
    {
        ReportingWorkKind work;
        lock (_lock)
        {
            work = SelectNextWorkLocked();
        }

        switch (work)
        {
            case ReportingWorkKind.FullStateReport:
                await SendFullStateReportAsync().ConfigureAwait(false);
                break;
            case ReportingWorkKind.ILoggerEffectiveConfigUpdate:
                await SendBatchedILoggerEffectiveConfigUpdateAsync().ConfigureAwait(false);
                break;
        }
    }

    private ReportingWorkKind SelectNextWorkLocked()
    {
        if (_stopped || !_instrumentationInitialized)
        {
            return ReportingWorkKind.None;
        }

        if (_fullStateReportPending)
        {
            return ReportingWorkKind.FullStateReport;
        }

        if (_effectiveConfigReporter != null && _iLoggerEffectiveConfigUpdatePending)
        {
            return ReportingWorkKind.ILoggerEffectiveConfigUpdate;
        }

        return ReportingWorkKind.None;
    }

    private async Task SendFullStateReportAsync()
    {
        if (!await WaitForFullStateReportCooldownAsync().ConfigureAwait(false))
        {
            return;
        }

        bool claimedILoggerEffectiveConfigUpdate;
        lock (_lock)
        {
            if (_stopped)
            {
                return;
            }

            _fullStateReportPending = false;
            claimedILoggerEffectiveConfigUpdate = _effectiveConfigReporter != null &&
                ClaimPendingILoggerEffectiveConfigUpdateLocked();
        }

        var result = await _reportDispatcher.DispatchFullStateReportAsync(
            _client,
            _effectiveConfigReporter,
            _reportingCancellationToken).ConfigureAwait(false);
        var clientAccepted = result is OpAmpDispatchResult.ClientAccepted or
            OpAmpDispatchResult.ClientAcceptedWithoutEffectiveConfig;
        var cooldownTask = clientAccepted
            ? CreateFullStateReportCooldownTask()
            : Task.CompletedTask;

        lock (_lock)
        {
            if (_stopped)
            {
                return;
            }

            if (clientAccepted)
            {
                _fullStateReportCooldownTask = cooldownTask;
            }

            if (result != OpAmpDispatchResult.ClientAccepted)
            {
                RestorePendingILoggerEffectiveConfigUpdateLocked(claimedILoggerEffectiveConfigUpdate);
                if (result == OpAmpDispatchResult.Failed)
                {
                    _fullStateReportPending = true;
                }
            }
        }
    }

    private async Task SendBatchedILoggerEffectiveConfigUpdateAsync()
    {
        if (!await WaitForILoggerEffectiveConfigBatchDelayAsync().ConfigureAwait(false))
        {
            return;
        }

        EffectiveConfigReporter effectiveConfigReporter;
        bool claimedILoggerEffectiveConfigUpdate;
        lock (_lock)
        {
            if (_stopped ||
                _fullStateReportPending ||
                !_iLoggerEffectiveConfigUpdatePending ||
                _effectiveConfigReporter == null)
            {
                return;
            }

            effectiveConfigReporter = _effectiveConfigReporter;
            claimedILoggerEffectiveConfigUpdate = ClaimPendingILoggerEffectiveConfigUpdateLocked();
        }

        await DispatchClaimedEffectiveConfigAsync(
            effectiveConfigReporter,
            claimedILoggerEffectiveConfigUpdate).ConfigureAwait(false);
    }

    private async Task<OpAmpDispatchResult> DispatchClaimedEffectiveConfigAsync(
        EffectiveConfigReporter effectiveConfigReporter,
        bool claimedILoggerEffectiveConfigUpdate)
    {
        var result = await _reportDispatcher.DispatchEffectiveConfigAsync(
            _client,
            effectiveConfigReporter,
            _reportingCancellationToken).ConfigureAwait(false);

        lock (_lock)
        {
            if (!_stopped && result != OpAmpDispatchResult.ClientAccepted)
            {
                // Keep the update pending, but wait for another reporting event to wake the worker
                // instead of retrying a persistent failure in a hot loop.
                RestorePendingILoggerEffectiveConfigUpdateLocked(claimedILoggerEffectiveConfigUpdate);
            }
        }

        return result;
    }

    private async Task<bool> WaitForFullStateReportCooldownAsync()
    {
        Task cooldownTask;
        lock (_lock)
        {
            if (_stopped)
            {
                return false;
            }

            cooldownTask = _fullStateReportCooldownTask;
        }

        try
        {
            await cooldownTask.ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (_reportingCancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed while waiting to rate-limit OpAMP full-state reports: {ex.Message}");
            return true;
        }
    }

    private async Task<bool> WaitForILoggerEffectiveConfigBatchDelayAsync()
    {
        try
        {
            await _delayAsync(
                ILoggerEffectiveConfigBatchDelay,
                _reportingCancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (_reportingCancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed while batching ILogger effective configuration changes: {ex.Message}");
            return true;
        }
    }

    private Task CreateFullStateReportCooldownTask()
    {
        try
        {
            return _delayAsync(FullStateReportCooldown, _reportingCancellationToken);
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to schedule OpAMP full-state report cooldown: {ex.Message}");
            return Task.CompletedTask;
        }
    }

    private bool ClaimPendingILoggerEffectiveConfigUpdateLocked()
    {
        if (!_iLoggerEffectiveConfigUpdatePending)
        {
            return false;
        }

        // Transfer every change observed so far to the payload about to be built. A callback racing
        // with payload construction sets the flag again and therefore remains pending for a later send.
        _iLoggerEffectiveConfigUpdatePending = false;
        return true;
    }

    private void RestorePendingILoggerEffectiveConfigUpdateLocked(bool wasClaimed)
    {
        if (wasClaimed)
        {
            _iLoggerEffectiveConfigUpdatePending = true;
        }
    }

    private void SignalWorkerLocked()
    {
        if (_wakeQueued)
        {
            return;
        }

        _wakeQueued = true;
        _wakeSignal.Release();
    }

    private void TryUnsubscribe()
    {
        try
        {
            _client.Unsubscribe<FlagsMessage>(this);
        }
        catch (ObjectDisposedException)
        {
            // The owner may dispose the client before the shutdown callback stops reporting work.
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to unsubscribe from a stopped OpAMP client: {ex.Message}");
        }
    }
}
