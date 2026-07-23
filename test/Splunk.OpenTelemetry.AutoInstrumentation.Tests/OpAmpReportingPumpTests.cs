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
    public async Task InitialReportIsFullStateAndIncludesEffectiveConfig()
    {
        var requestProbe = new OpAmpHttpRequestProbe();
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);
        var reportingPump = StartReporting(
            client,
            CreateReporter(CreateRecorder()));
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
    public async Task ILoggerChangeWhileInitialFullStateReportIsInFlightTriggersLaterUpdate()
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
    public async Task TimedOutInitialFullStateReportIsRetriedByAFullStateRequest()
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

        var reportingPump = StartReporting(client, reporter);
        MarkOpenTelemetryLoggerConfigured(recorder, reportingPump);
        RecordLogExporterOptions(recorder, reportingPump, new Uri("http://logs-collector-1:4318"));
        RecordLogExporterOptions(recorder, reportingPump, new Uri("http://logs-collector-2:4318"));
        reportingPump.MarkInstrumentationInitialized();
        await requestProbe.WaitForCountAsync(1);

        reportingPump.Stop();

        Assert.Equal(1, requestProbe.Count);
        var requestFrame = OpAmpRequestFrameInspector.Parse(requestProbe.GetRequestBody(1));
        Assert.True(requestFrame.IsFullStateReport);
        Assert.False(requestFrame.HasEffectiveConfig);
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
        TimeSpan? dispatchTimeout = null)
    {
        var reportingPump = new OpAmpReportingPump(
            client,
            reporter,
            new OpAmpReportDispatcher(dispatchTimeout ?? DefaultDispatchTimeout),
            delayAsync ?? Task.Delay,
            remoteConfigStatusResolver: null,
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
}
#endif
