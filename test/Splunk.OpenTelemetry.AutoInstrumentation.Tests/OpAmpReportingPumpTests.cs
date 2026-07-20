// <copyright file="OpAmpReportingPumpTests.cs" company="Splunk Inc.">
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
using System.Collections.Specialized;
using System.Net.Http;
using System.Text;
using OpenTelemetry.Exporter;
using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Messages;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using static Splunk.OpenTelemetry.AutoInstrumentation.Tests.OpAmpTestHelpers;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class OpAmpReportingPumpTests
{
    private static readonly TimeSpan DefaultDispatchTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task PendingFullStateReportTakesPriorityOverInitialEffectiveConfig()
    {
        var requestProbe = new OpAmpHttpRequestProbe();
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);
        var reportingPump = StartReporting(
            client,
            CreateReporter(CreateRecorder()));
        reportingPump.HandleMessage(CreateFlagsMessage(ServerSentFlags.ReportFullState));
        reportingPump.MarkInstrumentationInitialized();

        await requestProbe.WaitForCountAsync(1);
        reportingPump.Stop();

        Assert.Equal(1, requestProbe.Count);
        var fullStateFrame = OpAmpRequestFrameInspector.Parse(requestProbe.GetRequestBody(1));
        Assert.True(fullStateFrame.IsFullStateReport);
        Assert.Contains("OTEL_CONFIG_FILE=null", fullStateFrame.GetEffectiveConfigBody("environment"));
    }

    [Fact]
    public async Task ILoggerEffectiveConfigChangesAfterInitialDeliveryAreBatchedIntoOneUpdate()
    {
        var batchDelay = ManuallyReleasedDelay.ForILoggerBatching();
        var recorder = CreateRecorder(
            () => [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")]);
        var requestProbe = new OpAmpHttpRequestProbe();
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);
        var reportingPump = StartReporting(
            client,
            CreateReporter(recorder),
            batchDelay.DelayAsync);
        reportingPump.MarkInstrumentationInitialized();
        await requestProbe.WaitForCountAsync(1);

        MarkOpenTelemetryLoggerConfigured(recorder, reportingPump);
        RecordLogExporterOptions(
            recorder,
            reportingPump,
            new Uri("http://logs-collector-1:4318/v1/logs"));

        Assert.Equal(1, requestProbe.Count);

        await batchDelay.ReleaseNextAsync();
        await requestProbe.WaitForCountAsync(2);
        reportingPump.Stop();

        Assert.Equal(2, requestProbe.Count);
        var updateFrame = OpAmpRequestFrameInspector.Parse(requestProbe.GetRequestBody(2));
        var updateBody = updateFrame.GetEffectiveConfigBody("environment");
        Assert.Contains("http://logs-collector-1:4318/v1/logs", updateBody);
    }

    [Fact]
    public async Task FailedILoggerEffectiveConfigUpdateWaitsForAnotherChangeBeforeRetrying()
    {
        var reportDispatcher = new ScriptedOpAmpReportDispatcher(
            OpAmpDispatchResult.ClientAccepted,
            OpAmpDispatchResult.Failed,
            OpAmpDispatchResult.ClientAccepted);
        var requestProbe = new OpAmpHttpRequestProbe();
        var recorder = CreateRecorder(
            () => [EffectiveOtlpEndpoint.Http("http://bridge-collector:4318/v1/logs")]);
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);
        var reportingPump = StartReporting(
            client,
            CreateReporter(recorder),
            static (_, _) => Task.CompletedTask,
            reportDispatcher: reportDispatcher);
        reportingPump.MarkInstrumentationInitialized();
        await reportDispatcher.WaitForEffectiveConfigDispatchCountAsync(1);

        MarkOpenTelemetryLoggerConfigured(recorder, reportingPump);
        await reportDispatcher.WaitForEffectiveConfigDispatchCountAsync(2);

        var unexpectedRetry = reportDispatcher.WaitForEffectiveConfigDispatchCountAsync(3);
        var completedTask = await Task.WhenAny(unexpectedRetry, Task.Delay(TimeSpan.FromMilliseconds(100)));
        Assert.NotSame(unexpectedRetry, completedTask);

        RecordLogExporterOptions(
            recorder,
            reportingPump,
            new Uri("http://logs-collector:4318/v1/logs"));
        await WaitForCompletionAsync(unexpectedRetry);
        reportingPump.Stop();

        Assert.Equal(3, reportDispatcher.EffectiveConfigDispatchCount);
        Assert.Contains(
            "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=http://logs-collector:4318/v1/logs",
            reportDispatcher.GetEffectiveConfigBody(3));
    }

    [Fact]
    public async Task FullStateReportConsumesILoggerChangeWaitingForBatchUpdate()
    {
        var batchDelay = ManuallyReleasedDelay.ForILoggerBatching();
        var recorder = CreateRecorder();
        var requestProbe = new OpAmpHttpRequestProbe();
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);
        var reportingPump = StartReporting(
            client,
            CreateReporter(recorder),
            batchDelay.DelayAsync);
        reportingPump.MarkInstrumentationInitialized();
        await requestProbe.WaitForCountAsync(1);

        MarkOpenTelemetryLoggerConfigured(recorder, reportingPump);
        RecordLogExporterOptions(
            recorder,
            reportingPump,
            new Uri("http://logs-collector:4318/v1/logs"));
        await batchDelay.WaitUntilScheduledAsync();
        reportingPump.HandleMessage(CreateFlagsMessage(ServerSentFlags.ReportFullState));

        Assert.Equal(1, requestProbe.Count);
        await batchDelay.ReleaseNextAsync();
        await requestProbe.WaitForCountAsync(2);
        reportingPump.Stop();

        Assert.Equal(2, requestProbe.Count);
        var fullStateFrame = OpAmpRequestFrameInspector.Parse(requestProbe.GetRequestBody(2));
        Assert.True(fullStateFrame.IsFullStateReport);
        Assert.Contains(
            "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=http://logs-collector:4318/v1/logs",
            fullStateFrame.GetEffectiveConfigBody("environment"));
    }

    [Fact]
    public async Task ILoggerChangeAfterFullStatePayloadWasBuiltTriggersLaterUpdate()
    {
        var releaseFullStateReport = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var batchDelay = ManuallyReleasedDelay.ForILoggerBatching();
        var recorder = CreateRecorder();
        var requestProbe = new OpAmpHttpRequestProbe(
            onRequest: (requestNumber, _) => requestNumber == 2
                ? releaseFullStateReport.Task
                : Task.CompletedTask);
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);
        var reportingPump = StartReporting(
            client,
            CreateReporter(recorder),
            batchDelay.DelayAsync);
        reportingPump.MarkInstrumentationInitialized();
        await requestProbe.WaitForCountAsync(1);

        reportingPump.HandleMessage(CreateFlagsMessage(ServerSentFlags.ReportFullState));
        await requestProbe.WaitForCountAsync(2);

        MarkOpenTelemetryLoggerConfigured(recorder, reportingPump);
        RecordLogExporterOptions(
            recorder,
            reportingPump,
            new Uri("http://logs-collector:4318/v1/logs"));
        Assert.Equal(2, requestProbe.Count);

        releaseFullStateReport.TrySetResult(true);
        await batchDelay.ReleaseNextAsync();
        await requestProbe.WaitForCountAsync(3);
        reportingPump.Stop();

        Assert.Equal(3, requestProbe.Count);
        var updateFrame = OpAmpRequestFrameInspector.Parse(requestProbe.GetRequestBody(3));
        Assert.Contains(
            "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=http://logs-collector:4318/v1/logs",
            updateFrame.GetEffectiveConfigBody("environment"));
    }

    [Fact]
    public async Task ILoggerChangeWhileInitialPayloadIsInFlightTriggersLaterUpdate()
    {
        var batchDelay = ManuallyReleasedDelay.ForILoggerBatching();
        var recorder = CreateRecorder();
        var requestProbe = new OpAmpHttpRequestProbe(blockFirstRequest: true);
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);
        var reportingPump = StartReporting(
            client,
            CreateReporter(recorder),
            batchDelay.DelayAsync);
        reportingPump.MarkInstrumentationInitialized();
        await requestProbe.WaitForCountAsync(1);

        MarkOpenTelemetryLoggerConfigured(recorder, reportingPump);
        RecordLogExporterOptions(
            recorder,
            reportingPump,
            new Uri("http://logs-collector:4318/v1/logs"));

        requestProbe.ReleaseFirstRequest();
        await batchDelay.ReleaseNextAsync();
        await requestProbe.WaitForCountAsync(2);
        reportingPump.Stop();

        Assert.Equal(2, requestProbe.Count);
        var updateFrame = OpAmpRequestFrameInspector.Parse(requestProbe.GetRequestBody(2));
        Assert.Contains(
            "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=http://logs-collector:4318/v1/logs",
            updateFrame.GetEffectiveConfigBody("environment"));
    }

    [Fact]
    public async Task StopDropsPendingILoggerEffectiveConfigUpdate()
    {
        var batchDelay = ManuallyReleasedDelay.ForILoggerBatching();
        var recorder = CreateRecorder();
        var requestProbe = new OpAmpHttpRequestProbe();
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);
        var reportingPump = StartReporting(
            client,
            CreateReporter(recorder),
            batchDelay.DelayAsync);
        reportingPump.MarkInstrumentationInitialized();
        await requestProbe.WaitForCountAsync(1);

        MarkOpenTelemetryLoggerConfigured(recorder, reportingPump);
        RecordLogExporterOptions(
            recorder,
            reportingPump,
            new Uri("http://logs-collector:4318/v1/logs"));
        await batchDelay.WaitUntilScheduledAsync();

        reportingPump.Stop();

        Assert.Equal(1, requestProbe.Count);
    }

    [Fact]
    public async Task TimedOutInitialEffectiveConfigDeliveryIsRetriedByAFullStateRequest()
    {
        var failedRequestCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestProbe = new OpAmpHttpRequestProbe(onRequest: async (requestNumber, cancellationToken) =>
        {
            if (requestNumber != 1)
            {
                return;
            }

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            finally
            {
                failedRequestCompleted.TrySetResult(true);
            }
        });
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);
        var reportingPump = StartReporting(
            client,
            CreateReporter(CreateRecorder()),
            dispatchTimeout: TimeSpan.FromMilliseconds(50));
        reportingPump.MarkInstrumentationInitialized();

        await requestProbe.WaitForCountAsync(1);
        await WaitForCompletionAsync(failedRequestCompleted.Task);

        reportingPump.HandleMessage(CreateFlagsMessage(ServerSentFlags.ReportFullState));
        await requestProbe.WaitForCountAsync(2);
        reportingPump.Stop();
    }

    [Fact]
    public async Task EffectiveConfigBuildFailureDoesNotSuppressFullStateReport()
    {
        var recorder = CreateRecorder();
        var reporter = CreateReporter(recorder);
        var requestProbe = new OpAmpHttpRequestProbe();
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);

        recorder.MarkOpenTelemetryLoggerConfigured();
        recorder.CaptureLogExporterOptions(new OtlpExporterOptions
        {
            Protocol = (OtlpExportProtocol)42,
            Endpoint = new Uri("http://unsupported-collector:4318/v1/logs")
        });
        var reportingPump = StartReporting(client, reporter);
        reportingPump.HandleMessage(CreateFlagsMessage(ServerSentFlags.ReportFullState));
        reportingPump.MarkInstrumentationInitialized();
        await requestProbe.WaitForCountAsync(1);

        reportingPump.Stop();

        Assert.Equal(1, requestProbe.Count);
    }

    [Fact]
    public async Task ReportFullStateFlagsAreCoalescedUntilInstrumentationIsInitialized()
    {
        var requestProbe = new OpAmpHttpRequestProbe();
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);
        var reportingPump = StartReporting(client);

        reportingPump.HandleMessage(CreateFlagsMessage(ServerSentFlags.ReportFullState));
        reportingPump.HandleMessage(CreateFlagsMessage(ServerSentFlags.ReportFullState));

        Assert.Equal(0, requestProbe.Count);

        reportingPump.MarkInstrumentationInitialized();
        await requestProbe.WaitForCountAsync(1);
        reportingPump.Stop();

        Assert.Equal(1, requestProbe.Count);
    }

    [Fact]
    public async Task ReportFullStateFlagsAreCoalescedWhileAReportIsInProgress()
    {
        var cooldown = new ManuallyReleasedDelay();
        var requestProbe = new OpAmpHttpRequestProbe(blockFirstRequest: true);
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);
        var reportingPump = StartReporting(client, delayAsync: cooldown.DelayAsync);
        reportingPump.MarkInstrumentationInitialized();
        reportingPump.HandleMessage(CreateFlagsMessage(ServerSentFlags.ReportFullState));

        await requestProbe.WaitForCountAsync(1);

        for (var i = 0; i < 2; i++)
        {
            reportingPump.HandleMessage(CreateFlagsMessage(ServerSentFlags.ReportFullState));
        }

        Assert.Equal(1, requestProbe.Count);

        requestProbe.ReleaseFirstRequest();
        Assert.Equal(1, requestProbe.Count);
        await cooldown.ReleaseNextAsync();
        await requestProbe.WaitForCountAsync(2);

        reportingPump.Stop();

        Assert.Equal(2, requestProbe.Count);
    }

    [Fact]
    public async Task TimedOutFullStateReportIsRetriedWhenTheServerRequestsItAgain()
    {
        var failedRequestCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestProbe = new OpAmpHttpRequestProbe(onRequest: async (requestNumber, cancellationToken) =>
        {
            if (requestNumber != 1)
            {
                return;
            }

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            finally
            {
                failedRequestCompleted.TrySetResult(true);
            }
        });
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);
        var reportingPump = StartReporting(
            client,
            dispatchTimeout: TimeSpan.FromMilliseconds(50));
        reportingPump.MarkInstrumentationInitialized();
        reportingPump.HandleMessage(CreateFlagsMessage(ServerSentFlags.ReportFullState));

        await requestProbe.WaitForCountAsync(1);
        await WaitForCompletionAsync(failedRequestCompleted.Task);

        reportingPump.HandleMessage(CreateFlagsMessage(ServerSentFlags.ReportFullState));
        await requestProbe.WaitForCountAsync(2);
        reportingPump.Stop();
    }

    [Fact]
    public void ReportAvailableComponentsFlagDoesNotTriggerFullStateReport()
    {
        var requestProbe = new OpAmpHttpRequestProbe();
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);
        var reportingPump = StartReporting(client);
        reportingPump.MarkInstrumentationInitialized();

        reportingPump.HandleMessage(CreateFlagsMessage(ServerSentFlags.ReportAvailableComponents));
        reportingPump.Stop();

        Assert.Equal(0, requestProbe.Count);
    }

    [Fact]
    public async Task StopCancelsInFlightFullStateReportWithoutWaiting()
    {
        var cancellationObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestProbe = new OpAmpHttpRequestProbe(
            onRequest: (_, cancellationToken) =>
            {
                cancellationToken.Register(() => cancellationObserved.TrySetResult(true));
                return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            });
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);
        var reportingPump = StartReporting(client);
        reportingPump.HandleMessage(CreateFlagsMessage(ServerSentFlags.ReportFullState));
        reportingPump.MarkInstrumentationInitialized();
        await requestProbe.WaitForCountAsync(1);

        var stopTask = Task.Run(reportingPump.Stop);
        var completedTask = await Task.WhenAny(stopTask, Task.Delay(TimeSpan.FromSeconds(1)));

        Assert.Same(stopTask, completedTask);
        await stopTask;
        await WaitForCompletionAsync(cancellationObserved.Task);
    }

    private static OpAmpReportingPump StartReporting(
        OpAmpClient client,
        EffectiveConfigReporter? reporter = null,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        TimeSpan? dispatchTimeout = null,
        IOpAmpReportDispatcher? reportDispatcher = null)
    {
        var reportingPump = new OpAmpReportingPump(
            client,
            reporter,
            reportDispatcher ?? new OpAmpReportDispatcher(dispatchTimeout ?? DefaultDispatchTimeout),
            delayAsync ?? Task.Delay,
            instrumentationInitialized: false);
        reportingPump.Start();
        return reportingPump;
    }

    private static EffectiveConfigReporter CreateReporter(EffectiveConfigRecorder recorder)
    {
        return EffectiveConfigReporter.CreateValidated(recorder, EffectiveProfilerFeatures.None);
    }

    private static EffectiveConfigRecorder CreateRecorder(
        Func<IReadOnlyList<EffectiveOtlpEndpoint>?>? bridgeLogEndpointResolver = null)
    {
        return new EffectiveConfigRecorder(
            new EffectiveConfigStaticSettings(
                new PluginSettings(new NameValueConfigurationSource(new NameValueCollection()))),
            openTelemetrySdkDisabled: false,
            bridgeLogEndpointResolver ?? (() => null));
    }

    private static OpAmpClient CreateClient(HttpClient innerClient)
    {
        return new OpAmpClient(settings =>
        {
            settings.HttpClientFactory = () => innerClient;
            settings.EffectiveConfigurationReporting.EnableReporting = true;
        });
    }

    private static void MarkOpenTelemetryLoggerConfigured(
        EffectiveConfigRecorder recorder,
        OpAmpReportingPump reportingPump)
    {
        if (recorder.MarkOpenTelemetryLoggerConfigured())
        {
            reportingPump.NotifyILoggerEffectiveConfigChanged();
        }
    }

    private static void RecordLogExporterOptions(
        EffectiveConfigRecorder recorder,
        OpAmpReportingPump reportingPump,
        Uri endpoint)
    {
        if (recorder.CaptureLogExporterOptions(new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf,
            Endpoint = endpoint
        }))
        {
            reportingPump.NotifyILoggerEffectiveConfigChanged();
        }
    }

    private sealed class ScriptedOpAmpReportDispatcher : IOpAmpReportDispatcher
    {
        private readonly object _lock = new();
        private readonly Queue<OpAmpDispatchResult> _effectiveConfigResults;
        private readonly List<string> _effectiveConfigBodies = [];
        private readonly SemaphoreSlim _effectiveConfigDispatchObserved = new(0);

        public ScriptedOpAmpReportDispatcher(params OpAmpDispatchResult[] effectiveConfigResults)
        {
            _effectiveConfigResults = new Queue<OpAmpDispatchResult>(effectiveConfigResults);
        }

        public int EffectiveConfigDispatchCount
        {
            get
            {
                lock (_lock)
                {
                    return _effectiveConfigBodies.Count;
                }
            }
        }

        public Task<OpAmpDispatchResult> DispatchEffectiveConfigAsync(
            OpAmpClient client,
            EffectiveConfigReporter effectiveConfigReporter,
            CancellationToken sessionCancellationToken)
        {
            var payload = effectiveConfigReporter.BuildCurrentPayload();
            OpAmpDispatchResult result;
            lock (_lock)
            {
                if (_effectiveConfigResults.Count == 0)
                {
                    throw new InvalidOperationException("No scripted effective-config dispatch result remains.");
                }

                result = _effectiveConfigResults.Dequeue();
                _effectiveConfigBodies.Add(Encoding.UTF8.GetString(payload.Content.Span));
            }

            _effectiveConfigDispatchObserved.Release();
            return Task.FromResult(result);
        }

        public Task<OpAmpDispatchResult> DispatchFullStateReportAsync(
            OpAmpClient client,
            EffectiveConfigReporter? effectiveConfigReporter,
            CancellationToken sessionCancellationToken)
        {
            throw new InvalidOperationException("The test did not expect a full-state report.");
        }

        public string GetEffectiveConfigBody(int dispatchNumber)
        {
            lock (_lock)
            {
                Assert.InRange(dispatchNumber, 1, _effectiveConfigBodies.Count);
                return _effectiveConfigBodies[dispatchNumber - 1];
            }
        }

        public async Task WaitForEffectiveConfigDispatchCountAsync(int expectedCount)
        {
            while (EffectiveConfigDispatchCount < expectedCount)
            {
                Assert.True(await _effectiveConfigDispatchObserved.WaitAsync(TimeSpan.FromSeconds(5)));
            }
        }
    }
}
#endif
