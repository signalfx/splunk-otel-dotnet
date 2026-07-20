// <copyright file="OpAmpTests.cs" company="Splunk Inc.">
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

using System.Collections.Specialized;
#if NET
using System.Net.Http;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.Trace;
using static Splunk.OpenTelemetry.AutoInstrumentation.Tests.OpAmpTestHelpers;
#endif
using OpenTelemetry.OpAmp.Client.Settings;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class OpAmpTests
{
    [Fact]
    public void ConfigureEffectiveConfigReporting_EnablesEffectiveConfigReportingAfterSuccessfulPreflight()
    {
        var settings = new OpAmpClientSettings();
        var opAmp = CreateOpAmp();

        opAmp.ConfigureEffectiveConfigReporting(settings);

        Assert.True(settings.EffectiveConfigurationReporting.EnableReporting);
    }

    [Fact]
    public void ConfigureEffectiveConfigReporting_DoesNotEnableReporting_WhenAutomaticSdkSetupIsDisabled()
    {
        var settings = new OpAmpClientSettings();
        var recorderCreated = false;
        var opAmp = CreateOpAmp(
            effectiveConfigRecorderFactory: () =>
            {
                recorderCreated = true;
                return CreateRecorder();
            },
            sdkSetupEnabledResolver: () => false);

        opAmp.ConfigureEffectiveConfigReporting(settings);

        Assert.False(settings.EffectiveConfigurationReporting.EnableReporting);
        Assert.False(recorderCreated);
    }

    [Fact]
    public void ConfigureEffectiveConfigReporting_DoesNotCreateRecorderOrEnableReporting_WhenOpAmpIsDisabled()
    {
        var recorderCreated = false;
        var settings = new OpAmpClientSettings();
        var opAmp = CreateOpAmp(
            effectiveConfigRecorderFactory: () =>
            {
                recorderCreated = true;
                return CreateRecorder();
            },
            opAmpEnabledResolver: () => false);

        opAmp.ConfigureEffectiveConfigReporting(settings);

        Assert.False(recorderCreated);
        Assert.False(settings.EffectiveConfigurationReporting.EnableReporting);
    }

    [Fact]
    public void ConfigureEffectiveConfigReporting_DoesNotEnableReporting_WhenProfilerStateResolutionFails()
    {
        var settings = new OpAmpClientSettings();
        var opAmp = CreateOpAmp(
            profilerStateResolver: () => throw new MissingMemberException("profiler state"));

        opAmp.ConfigureEffectiveConfigReporting(settings);

        Assert.False(settings.EffectiveConfigurationReporting.EnableReporting);
    }

    [Fact]
    public void ConfigureEffectiveConfigReporting_EnablesReporting_WhenSdkIsDisabled()
    {
        var settings = new OpAmpClientSettings();
        var recorder = new EffectiveConfigRecorder(
            CreateStaticSettings(),
            openTelemetrySdkDisabled: true,
            () => throw new MissingMemberException("provider graph"));
        var opAmp = CreateOpAmp(() => recorder);

        opAmp.ConfigureEffectiveConfigReporting(settings);

        Assert.True(settings.EffectiveConfigurationReporting.EnableReporting);
    }

#if NET
    [Fact]
    public void MarkOpenTelemetryLoggerConfigured_DoesNotCreateRecorder_WhenOpAmpIsDisabled()
    {
        AssertRecordingHookDoesNotCreateRecorder(static opAmp => opAmp.MarkOpenTelemetryLoggerConfigured());
    }

    [Fact]
    public void RecordLogExporterOptions_DoesNotCreateRecorder_WhenOpAmpIsDisabled()
    {
        AssertRecordingHookDoesNotCreateRecorder(static opAmp =>
            opAmp.RecordLogExporterOptions(new OtlpExporterOptions()));
    }

    [Fact]
    public void RecordTraceProviderEndpoints_DoesNotCreateRecorder_WhenOpAmpIsDisabled()
    {
        AssertRecordingHookDoesNotCreateRecorder(static opAmp =>
        {
            using var tracerProvider = global::OpenTelemetry.Sdk.CreateTracerProviderBuilder().Build();
            opAmp.RecordTraceProviderEndpoints(tracerProvider);
        });
    }

    [Fact]
    public void RecordMetricProviderEndpoints_DoesNotCreateRecorder_WhenOpAmpIsDisabled()
    {
        AssertRecordingHookDoesNotCreateRecorder(static opAmp =>
        {
            using var meterProvider = global::OpenTelemetry.Sdk.CreateMeterProviderBuilder().Build();
            opAmp.RecordMetricProviderEndpoints(meterProvider);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InitialEffectiveConfigReportIsSentOnce_WhenReadinessSignalsArriveInEitherOrder(
        bool instrumentationInitializedFirst)
    {
        var opAmp = CreateOpAmp();
        var requestProbe = new OpAmpHttpRequestProbe();
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient, opAmp.ConfigureEffectiveConfigReporting);

        if (instrumentationInitializedFirst)
        {
            opAmp.MarkInstrumentationInitialized();
            opAmp.OnClientStarted(client);
        }
        else
        {
            opAmp.OnClientStarted(client);
            opAmp.MarkInstrumentationInitialized();
        }

        await requestProbe.WaitForCountAsync(1);

        opAmp.StopClientReporting();

        Assert.Equal(1, requestProbe.Count);
    }

    [Fact]
    public async Task EffectiveConfigChangeBeforeInstrumentationInitializationIsIncludedInInitialReport()
    {
        var opAmp = CreateOpAmp();
        var requestProbe = new OpAmpHttpRequestProbe();
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient, opAmp.ConfigureEffectiveConfigReporting);
        opAmp.OnClientStarted(client);

        opAmp.MarkOpenTelemetryLoggerConfigured();
        const string logsEndpoint = "http://logs-collector:4318/v1/logs";
        opAmp.RecordLogExporterOptions(new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf,
            Endpoint = new Uri(logsEndpoint)
        });

        Assert.Equal(0, requestProbe.Count);

        opAmp.MarkInstrumentationInitialized();
        await requestProbe.WaitForCountAsync(1);

        opAmp.StopClientReporting();

        Assert.Equal(1, requestProbe.Count);
        var requestFrame = OpAmpRequestFrameInspector.Parse(requestProbe.GetRequestBody(1));
        Assert.Contains(
            $"OTEL_EXPORTER_OTLP_LOGS_ENDPOINT={logsEndpoint}",
            requestFrame.GetEffectiveConfigBody("environment"));
    }

    [Fact]
    public void ConfigureEffectiveConfigReporting_DoesNotAdvertiseOrSend_WhenProviderGraphPreflightFails()
    {
        var settings = new OpAmpClientSettings();
        var providerGraphResolverCalled = false;
        var recorder = new EffectiveConfigRecorder(
            CreateStaticSettings(),
            openTelemetrySdkDisabled: false,
            () =>
            {
                providerGraphResolverCalled = true;
                throw new MissingMemberException("provider graph");
            });
        var opAmp = CreateOpAmp(() => recorder);
        var requestProbe = new OpAmpHttpRequestProbe();
        using var innerClient = new HttpClient(requestProbe);

        opAmp.ConfigureEffectiveConfigReporting(settings);
        using var client = CreateClient(
            innerClient,
            clientSettings => clientSettings.EffectiveConfigurationReporting.EnableReporting = true);

        Assert.False(settings.EffectiveConfigurationReporting.EnableReporting);
        Assert.True(providerGraphResolverCalled);

        opAmp.OnClientStarted(client);
        opAmp.MarkInstrumentationInitialized();
        opAmp.MarkOpenTelemetryLoggerConfigured();
        opAmp.RecordLogExporterOptions(new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf,
            Endpoint = new Uri("http://logs-collector:4318/v1/logs")
        });
        opAmp.StopClientReporting();

        Assert.Equal(0, requestProbe.Count);
    }

    [Fact]
    public async Task DuplicateStartCallbackForSameClientIsNoOp()
    {
        var opAmp = CreateOpAmp();
        var requestProbe = new OpAmpHttpRequestProbe();
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient, opAmp.ConfigureEffectiveConfigReporting);

        opAmp.OnClientStarted(client);
        opAmp.OnClientStarted(client);

        opAmp.MarkInstrumentationInitialized();
        await requestProbe.WaitForCountAsync(1);
        opAmp.StopClientReporting();

        Assert.Equal(1, requestProbe.Count);
    }

    [Fact]
    public async Task StopClientReportingCancelsInFlightInitialReportWithoutWaiting()
    {
        var cancellationObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var opAmp = CreateOpAmp();
        var requestProbe = new OpAmpHttpRequestProbe(
            onRequest: (_, cancellationToken) =>
            {
                cancellationToken.Register(() => cancellationObserved.TrySetResult(true));
                return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            });
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient, opAmp.ConfigureEffectiveConfigReporting);
        opAmp.OnClientStarted(client);
        opAmp.MarkInstrumentationInitialized();
        await requestProbe.WaitForCountAsync(1);

        var stopTask = Task.Run(opAmp.StopClientReporting);
        var completedTask = await Task.WhenAny(stopTask, Task.Delay(TimeSpan.FromSeconds(1)));

        Assert.Same(stopTask, completedTask);
        await stopTask;
        await WaitForCompletionAsync(cancellationObserved.Task);
    }

    [Fact]
    public void StopClientReportingDoesNotThrow_WhenClientIsAlreadyDisposed()
    {
        var opAmp = CreateOpAmp();
        var requestProbe = new OpAmpHttpRequestProbe();
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);
        opAmp.OnClientStarted(client);
        client.Dispose();

        var exception = Record.Exception(opAmp.StopClientReporting);

        Assert.Null(exception);
    }

    [Fact]
    public void OnClientStartedDoesNotThrow_WhenClientIsAlreadyDisposed()
    {
        var opAmp = CreateOpAmp();
        var requestProbe = new OpAmpHttpRequestProbe();
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient);
        client.Dispose();

        var exception = Record.Exception(() => opAmp.OnClientStarted(client));

        Assert.Null(exception);
    }

    [Fact]
    public void StopClientReportingPreventsLateInitialReport()
    {
        var opAmp = CreateOpAmp();
        var requestProbe = new OpAmpHttpRequestProbe();
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient, opAmp.ConfigureEffectiveConfigReporting);
        opAmp.OnClientStarted(client);

        opAmp.StopClientReporting();
        opAmp.MarkInstrumentationInitialized();

        Assert.Equal(0, requestProbe.Count);
    }

    [Fact]
    public void StopClientReportingBeforeClientStartsPreventsLateStart()
    {
        var opAmp = CreateOpAmp();
        var requestProbe = new OpAmpHttpRequestProbe();
        using var innerClient = new HttpClient(requestProbe);
        using var client = CreateClient(innerClient, opAmp.ConfigureEffectiveConfigReporting);

        opAmp.StopClientReporting();
        opAmp.OnClientStarted(client);
        opAmp.MarkInstrumentationInitialized();

        Assert.Equal(0, requestProbe.Count);
    }
#endif

    private static OpAmp CreateOpAmp(
        Func<EffectiveConfigRecorder>? effectiveConfigRecorderFactory = null,
        Func<bool>? opAmpEnabledResolver = null,
        Func<bool>? sdkSetupEnabledResolver = null,
        Func<EffectiveProfilerFeatures>? profilerStateResolver = null)
    {
        return new OpAmp(
            effectiveConfigRecorderFactory ?? CreateRecorder,
            opAmpEnabledResolver ?? (() => true),
            sdkSetupEnabledResolver ?? (() => true),
            profilerStateResolver ?? (() => EffectiveProfilerFeatures.None));
    }

    private static EffectiveConfigRecorder CreateRecorder()
    {
        return new EffectiveConfigRecorder(
            CreateStaticSettings(),
            openTelemetrySdkDisabled: false,
            () => null);
    }

    private static EffectiveConfigStaticSettings CreateStaticSettings()
    {
        return new EffectiveConfigStaticSettings(
            new PluginSettings(new NameValueConfigurationSource(new NameValueCollection())));
    }

#if NET
    private static void AssertRecordingHookDoesNotCreateRecorder(Action<OpAmp> invokeRecordingHook)
    {
        var recorderCreated = false;
        var opAmp = CreateOpAmp(
            effectiveConfigRecorderFactory: () =>
            {
                recorderCreated = true;
                return CreateRecorder();
            },
            opAmpEnabledResolver: () => false);

        invokeRecordingHook(opAmp);

        Assert.False(recorderCreated);
    }

    private static OpAmpClient CreateClient(
        HttpClient innerClient,
        Action<OpAmpClientSettings>? configure = null)
    {
        return new OpAmpClient(settings =>
        {
            settings.HttpClientFactory = () => innerClient;
            configure?.Invoke(settings);
        });
    }
#endif
}
