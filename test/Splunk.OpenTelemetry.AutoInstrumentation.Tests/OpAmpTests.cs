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
using System.Text;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;
using OpenTelemetry.Trace;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class OpAmpTests
{
    [Fact]
    public void ConfigureOptions_EnablesRemoteConfigurationRuntimeModeWhenCpuProfilerIsInitiallyDisabled()
    {
        var pluginSettings = new PluginSettings(new NameValueConfigurationSource(
            new NameValueCollection
            {
                [ConfigurationKeys.Splunk.OpAmp.RemoteConfig] = "true"
            }));
        ProfilerRuntimeConfiguration.Initialize(pluginSettings);
        var settings = new OpAmpClientSettings();
        var opAmp = CreateOpAmp();

        opAmp.ConfigureOptions(settings, pluginSettings);

        Assert.True(settings.EffectiveConfigurationReporting.EnableReporting);
        Assert.True(settings.RemoteConfiguration.AcceptsRemoteConfig);
        Assert.True(settings.RemoteConfiguration.ReportsRemoteConfigStatus);
        Assert.True(ProfilerRuntimeConfiguration.RuntimeConfigurationEnabled);
        Assert.False(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal((uint)Constants.DefaultSamplingInterval, ProfilerRuntimeConfiguration.StartupCpuProfilerCallStackInterval);
    }

    [Fact]
    public void ConfigureOptions_RemoteConfigurationIsDisabledByDefault()
    {
        var pluginSettings = new PluginSettings(new NameValueConfigurationSource(new NameValueCollection()));
        ProfilerRuntimeConfiguration.Initialize(pluginSettings);
        var settings = new OpAmpClientSettings();

        CreateOpAmp().ConfigureOptions(settings, pluginSettings);

        Assert.True(settings.EffectiveConfigurationReporting.EnableReporting);
        Assert.False(settings.RemoteConfiguration.AcceptsRemoteConfig);
        Assert.False(settings.RemoteConfiguration.ReportsRemoteConfigStatus);
        Assert.False(ProfilerRuntimeConfiguration.RuntimeConfigurationEnabled);
    }

    [Fact]
    public void ConfigureClient_DoesNotSubscribeWhenRemoteConfigurationIsDisabled()
    {
        var opAmp = CreateConfiguredOpAmp();
        var client = new TestOpAmpClient();

        opAmp.ConfigureClient(client);

        Assert.Equal(0, client.SubscriptionCount);
        Assert.Null(opAmp.GetRemoteConfigStatus());
    }

    [Fact]
    public void ConfigureClient_SubscribesBeforeStartupOnlyOnceAndIgnoresReplacementClients()
    {
        var opAmp = CreateRemoteConfiguredOpAmp();
        var client = new TestOpAmpClient();
        var replacement = new TestOpAmpClient();

        opAmp.ConfigureClient(client);
        opAmp.ConfigureClient(client);
        opAmp.ConfigureClient(replacement);

        Assert.Equal(1, client.SubscriptionCount);
        Assert.NotNull(client.RemoteConfigurationListener);
        Assert.Equal(0, replacement.SubscriptionCount);
        Assert.Null(opAmp.GetRemoteConfigStatus());
    }

    [Fact]
    public void RemoteConfigurationStatusGetterCompletesDuringNotificationsAndKeepsStableSnapshots()
    {
        var opAmp = CreateRemoteConfiguredOpAmp();
        var reports = new List<RemoteConfigStatusReport>();
        var client = new TestOpAmpClient(onRemoteNotify: () =>
        {
            var getter = Task.Run(opAmp.GetRemoteConfigStatus);
            Assert.True(getter.Wait(TimeSpan.FromSeconds(5)), "The status getter was blocked by the notification caller.");
            reports.Add(Assert.IsType<RemoteConfigStatusReport>(getter.GetAwaiter().GetResult()));
        });
        opAmp.ConfigureClient(client);
        var configHash = new byte[] { 1, 2, 3 };

        Assert.IsType<OpAmpRemoteConfigurationListener>(client.RemoteConfigurationListener)
            .HandleRemoteConfiguration(configHash, contentType: null, body: null);

        Assert.Collection(
            reports,
            report => Assert.Equal(RemoteConfigStatusCode.Applying, report.Status),
            report => Assert.Equal(RemoteConfigStatusCode.Applied, report.Status));
        Assert.Equal(new byte[] { 1, 2, 3 }, reports[0].LastRemoteConfigHash.ToArray());
        Assert.Same(reports[1], opAmp.GetRemoteConfigStatus());
        Assert.Equal(2, client.RemoteNotificationCount);
    }

    [Fact]
    public void RemoteConfigurationCleanupUnsubscribesOnceAndPreventsLateBindingOrNotifications()
    {
        var opAmp = CreateRemoteConfiguredOpAmp();
        var client = new TestOpAmpClient();
        opAmp.ConfigureClient(client);
        var listener = Assert.IsType<OpAmpRemoteConfigurationListener>(client.RemoteConfigurationListener);
        listener.HandleRemoteConfiguration(new byte[] { 1 }, contentType: null, body: null);
        var statusBeforeStop = opAmp.GetRemoteConfigStatus();

        opAmp.StopClientReporting();
        opAmp.StopClientReporting();
        opAmp.ConfigureClient(client);
        listener.HandleRemoteConfiguration(new byte[] { 2 }, contentType: null, body: null);

        Assert.Equal(1, client.SubscriptionCount);
        Assert.Equal(1, client.UnsubscriptionCount);
        Assert.Equal(2, client.RemoteNotificationCount);
        Assert.NotNull(statusBeforeStop);
        Assert.Equal(RemoteConfigStatusCode.Applied, statusBeforeStop.Status);
        Assert.Equal(new byte[] { 1 }, statusBeforeStop.LastRemoteConfigHash.ToArray());
    }

    [Fact]
    public void SuccessfulPreflightEnablesEffectiveConfigReporting()
    {
        var settings = new OpAmpClientSettings();
        CreateOpAmp().ConfigureEffectiveConfigReporting(settings);

        Assert.True(settings.EffectiveConfigurationReporting.EnableReporting);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void DisabledOpAmpOrAutomaticSdkSetupDoesNotCreateRecorder(bool opAmpEnabled, bool sdkSetupEnabled)
    {
        var recorderCreated = false;
        var opAmp = CreateOpAmp(
            recorderFactory: () =>
            {
                recorderCreated = true;
                return CreateRecorder();
            },
            opAmpEnabled: opAmpEnabled,
            sdkSetupEnabled: sdkSetupEnabled);
        var settings = new OpAmpClientSettings();
        opAmp.ConfigureEffectiveConfigReporting(settings);

        Assert.False(settings.EffectiveConfigurationReporting.EnableReporting);
        Assert.False(recorderCreated);
        Assert.Throws<InvalidOperationException>(() => opAmp.GetEffectiveConfig());
    }

    [Fact]
    public void MarkOpenTelemetryLoggerConfigured_DoesNotCreateRecorder_WhenOpAmpIsDisabled()
    {
        AssertRecordingHookDoesNotCreateRecorder(opAmp => opAmp.MarkOpenTelemetryLoggerConfigured());
    }

    [Fact]
    public void RecordLogExporterOptions_DoesNotCreateRecorder_WhenOpAmpIsDisabled()
    {
        AssertRecordingHookDoesNotCreateRecorder(opAmp => opAmp.RecordLogExporterOptions(CreateLogOptions()));
    }

    [Fact]
    public void RecordTraceProviderEndpoints_DoesNotCreateRecorder_WhenOpAmpIsDisabled()
    {
        AssertRecordingHookDoesNotCreateRecorder(opAmp =>
        {
            using var traces = global::OpenTelemetry.Sdk.CreateTracerProviderBuilder().Build();
            opAmp.RecordTraceProviderEndpoints(traces);
        });
    }

    [Fact]
    public void RecordMetricProviderEndpoints_DoesNotCreateRecorder_WhenOpAmpIsDisabled()
    {
        AssertRecordingHookDoesNotCreateRecorder(opAmp =>
        {
            using var metrics = global::OpenTelemetry.Sdk.CreateMeterProviderBuilder().Build();
            opAmp.RecordMetricProviderEndpoints(metrics);
        });
    }

    [Fact]
    public void ProfilerStateResolutionFailureDisablesReporting()
    {
        var opAmp = CreateOpAmp(profilerStateResolver: () => throw new InvalidOperationException("Unavailable profiler state."));
        var settings = new OpAmpClientSettings();
        settings.EffectiveConfigurationReporting.EnableReporting = true;
        opAmp.ConfigureEffectiveConfigReporting(settings);

        Assert.False(settings.EffectiveConfigurationReporting.EnableReporting);
        Assert.Throws<InvalidOperationException>(() => opAmp.GetEffectiveConfig());
    }

    [Fact]
    public void InvalidProviderGraphDisablesReporting()
    {
        var resolverCalled = false;
        var recorder = CreateRecorder(() =>
        {
            resolverCalled = true;
            throw new InvalidOperationException("Unsupported provider graph.");
        });
        var opAmp = CreateOpAmp(recorderFactory: () => recorder);
        var settings = new OpAmpClientSettings();
        opAmp.ConfigureEffectiveConfigReporting(settings);

        Assert.True(resolverCalled);
        Assert.False(settings.EffectiveConfigurationReporting.EnableReporting);
    }

    [Fact]
    public void InvalidPayloadDisablesReporting()
    {
        var recorder = CreateRecorder(() =>
        [
            EffectiveOtlpEndpoint.Http("http://first:4318/v1/logs"),
            EffectiveOtlpEndpoint.Http("http://second:4318/v1/logs")
        ]);
        var opAmp = CreateOpAmp(recorderFactory: () => recorder);
        var settings = new OpAmpClientSettings();
        opAmp.ConfigureEffectiveConfigReporting(settings);

        Assert.False(settings.EffectiveConfigurationReporting.EnableReporting);
    }

    [Fact]
    public void DisabledSdkIsAValidConfiguration()
    {
        var recorder = new EffectiveConfigRecorder(CreateStaticSettings(), openTelemetrySdkDisabled: true);
        var opAmp = CreateOpAmp(recorderFactory: () => recorder);
        var settings = new OpAmpClientSettings();
        opAmp.ConfigureEffectiveConfigReporting(settings);

        Assert.True(settings.EffectiveConfigurationReporting.EnableReporting);
        Assert.Contains("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=none", GetPayload(opAmp));
    }

    [Fact]
    public void ChangesBeforeClientBindingAreAvailableWithoutAnInitialNotification()
    {
        var opAmp = CreateConfiguredOpAmp();
        opAmp.MarkOpenTelemetryLoggerConfigured();
        opAmp.RecordLogExporterOptions(CreateLogOptions());
        var client = new TestOpAmpClient();
        opAmp.ConfigureClient(client);

        Assert.Contains("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=http://logs-collector:4318/v1/logs", GetPayload(opAmp));
        Assert.Equal(0, client.NotificationCount);
    }

    [Fact]
    public void ChangedILoggerEndpointsNotifyImmediatelyAndUnchangedEndpointsDoNotNotify()
    {
        var opAmp = CreateConfiguredOpAmp();
        var client = new TestOpAmpClient();
        opAmp.ConfigureClient(client);
        opAmp.MarkOpenTelemetryLoggerConfigured();
        Assert.Equal(0, client.NotificationCount);

        opAmp.RecordLogExporterOptions(CreateLogOptions());
        Assert.Equal(1, client.NotificationCount);

        opAmp.RecordLogExporterOptions(CreateLogOptions());
        Assert.Equal(1, client.NotificationCount);
    }

    [Fact]
    public void ReturnedBuffersRemainStableAcrossLaterChangesAndReads()
    {
        var opAmp = CreateConfiguredOpAmp();
        var first = Assert.Single(opAmp.GetEffectiveConfig());
        var firstBytes = first.Content.ToArray();
        opAmp.MarkOpenTelemetryLoggerConfigured();
        opAmp.RecordLogExporterOptions(CreateLogOptions());
        var second = Assert.Single(opAmp.GetEffectiveConfig());
        var secondBytes = second.Content.ToArray();
        var third = Assert.Single(opAmp.GetEffectiveConfig());

        Assert.Equal(firstBytes, first.Content.ToArray());
        Assert.NotEqual(Encoding.UTF8.GetString(firstBytes), Encoding.UTF8.GetString(secondBytes));
        Assert.Equal(secondBytes, second.Content.ToArray());
        Assert.Equal(secondBytes, third.Content.ToArray());
    }

    [Fact]
    public void GetterReadsCurrentProfilerStateAndPropagatesLaterFailures()
    {
        var state = EffectiveProfilerState.None;
        var fail = false;
        var opAmp = CreateConfiguredOpAmp(() => fail ? throw new InvalidOperationException("Query failed.") : state);
        Assert.Contains("SPLUNK_PROFILER_ENABLED=false", GetPayload(opAmp));
        state = new EffectiveProfilerState(EffectiveProfilerFeatures.Cpu, 2500, 0);

        Assert.Contains("SPLUNK_PROFILER_ENABLED=true", GetPayload(opAmp));
        Assert.Contains("SPLUNK_PROFILER_CALL_STACK_INTERVAL=2500", GetPayload(opAmp));
        fail = true;
        Assert.Throws<InvalidOperationException>(() => opAmp.GetEffectiveConfig());
    }

    [Fact]
    public void NotificationFailureDoesNotEscapeTheConfigurationHook()
    {
        var opAmp = CreateConfiguredOpAmp();
        var client = new TestOpAmpClient(() => throw new ObjectDisposedException("client"));
        opAmp.ConfigureClient(client);
        opAmp.MarkOpenTelemetryLoggerConfigured();
        opAmp.RecordLogExporterOptions(CreateLogOptions());

        Assert.Equal(1, client.NotificationCount);
        Assert.Contains("http://logs-collector:4318/v1/logs", GetPayload(opAmp));
    }

    [Fact]
    public async Task NotificationAllowsAGetterOnAnotherThreadToCompleteBeforeReturning()
    {
        var opAmp = CreateConfiguredOpAmp();
        opAmp.MarkOpenTelemetryLoggerConfigured();
        Task<string>? getter = null;
        var completedDuringNotification = false;
        var client = new TestOpAmpClient(() =>
        {
            getter = Task.Run(() => GetPayload(opAmp));
            completedDuringNotification = getter.Wait(TimeSpan.FromSeconds(5));
        });
        opAmp.ConfigureClient(client);

        opAmp.RecordLogExporterOptions(CreateLogOptions());
        Assert.True(completedDuringNotification, "The getter was blocked by the notification caller.");
        Assert.NotNull(getter);
        Assert.Contains("http://logs-collector:4318/v1/logs", await getter);
    }

    [Fact]
    public async Task CleanupDoesNotWaitForAnInFlightGetterAndKeepsItsStateAvailable()
    {
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        var block = false;
        var opAmp = CreateConfiguredOpAmp(() =>
        {
            if (block)
            {
                entered.TrySetResult(true);
                Assert.True(release.Wait(TimeSpan.FromSeconds(5)));
            }

            return EffectiveProfilerState.None;
        });
        block = true;
        var getter = Task.Run(opAmp.GetEffectiveConfig);
        try
        {
            await CompletesAsync(entered.Task);
            await CompletesAsync(Task.Run(opAmp.StopClientReporting));
        }
        finally
        {
            release.Set();
        }

        await CompletesAsync(getter);
        Assert.Single(await getter);
    }

    [Fact]
    public async Task CleanupDoesNotWaitForAnInFlightNotification()
    {
        var opAmp = CreateConfiguredOpAmp();
        opAmp.MarkOpenTelemetryLoggerConfigured();
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        var releasedDuringNotification = false;
        var client = new TestOpAmpClient(() =>
        {
            entered.TrySetResult(true);
            releasedDuringNotification = release.Wait(TimeSpan.FromSeconds(5));
        });
        opAmp.ConfigureClient(client);
        var notification = Task.Run(() => opAmp.RecordLogExporterOptions(CreateLogOptions()));
        try
        {
            await CompletesAsync(entered.Task);
            await CompletesAsync(Task.Run(opAmp.StopClientReporting));
        }
        finally
        {
            release.Set();
        }

        await CompletesAsync(notification);
        Assert.True(releasedDuringNotification);
        opAmp.RecordLogExporterOptions(CreateLogOptions("http://changed-after-stop:4318/v1/logs"));
        Assert.Equal(1, client.NotificationCount);
    }

    [Fact]
    public void CleanupIsIdempotentAndPreventsLateBindingOrNotification()
    {
        var opAmp = CreateConfiguredOpAmp();
        var client = new TestOpAmpClient();
        opAmp.ConfigureClient(client);
        opAmp.MarkOpenTelemetryLoggerConfigured();
        opAmp.RecordLogExporterOptions(CreateLogOptions());
        Assert.Equal(1, client.NotificationCount);

        opAmp.StopClientReporting();
        opAmp.StopClientReporting();
        opAmp.ConfigureClient(client);
        opAmp.RecordLogExporterOptions(CreateLogOptions("http://changed-after-stop:4318/v1/logs"));

        Assert.Equal(1, client.NotificationCount);
    }

    private static void AssertRecordingHookDoesNotCreateRecorder(Action<OpAmp> invokeRecordingHook)
    {
        var recorderCreated = false;
        var opAmp = CreateOpAmp(
            recorderFactory: () =>
            {
                recorderCreated = true;
                return CreateRecorder();
            },
            opAmpEnabled: false);

        invokeRecordingHook(opAmp);

        Assert.False(recorderCreated);
    }

    private static OpAmp CreateConfiguredOpAmp(Func<EffectiveProfilerState>? profilerStateResolver = null)
    {
        var opAmp = CreateOpAmp(profilerStateResolver: profilerStateResolver);
        opAmp.ConfigureEffectiveConfigReporting(new OpAmpClientSettings());
        return opAmp;
    }

    private static OpAmp CreateRemoteConfiguredOpAmp()
    {
        var pluginSettings = new PluginSettings(new NameValueConfigurationSource(
            new NameValueCollection
            {
                [ConfigurationKeys.Splunk.OpAmp.RemoteConfig] = "true"
            }));
        ProfilerRuntimeConfiguration.Initialize(pluginSettings);
        var opAmp = CreateOpAmp();
        opAmp.ConfigureOptions(new OpAmpClientSettings(), pluginSettings);
        return opAmp;
    }

    private static OpAmp CreateOpAmp(
        Func<EffectiveConfigRecorder>? recorderFactory = null,
        bool opAmpEnabled = true,
        bool sdkSetupEnabled = true,
        Func<EffectiveProfilerState>? profilerStateResolver = null)
    {
        return new OpAmp(
            recorderFactory ?? (() => CreateRecorder()),
            () => opAmpEnabled,
            () => sdkSetupEnabled,
            profilerStateResolver ?? (() => EffectiveProfilerState.None));
    }

    private static EffectiveConfigRecorder CreateRecorder(Func<IReadOnlyList<EffectiveOtlpEndpoint>?>? bridgeResolver = null)
    {
        return new EffectiveConfigRecorder(CreateStaticSettings(), openTelemetrySdkDisabled: false, bridgeResolver ?? (() => null));
    }

    private static EffectiveConfigStaticSettings CreateStaticSettings()
    {
        return new EffectiveConfigStaticSettings(new PluginSettings(new NameValueConfigurationSource(new NameValueCollection())));
    }

    private static OtlpExporterOptions CreateLogOptions(string endpoint = "http://logs-collector:4318/v1/logs")
    {
        return new OtlpExporterOptions
        {
            Protocol = OtlpExportProtocol.HttpProtobuf,
            Endpoint = new Uri(endpoint)
        };
    }

    private static string GetPayload(OpAmp opAmp)
    {
        return Encoding.UTF8.GetString(Assert.Single(opAmp.GetEffectiveConfig()).Content.ToArray());
    }

    private static async Task CompletesAsync(Task task)
    {
        Assert.Same(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5))));
        await task;
    }

    private sealed class TestOpAmpClient : IOpAmpClient
    {
        private readonly Action? _onNotify;
        private readonly Action? _onRemoteNotify;
        private int _notificationCount;
        private int _remoteNotificationCount;

        public TestOpAmpClient(Action? onNotify = null, Action? onRemoteNotify = null)
        {
            _onNotify = onNotify;
            _onRemoteNotify = onRemoteNotify;
        }

        public int NotificationCount => Volatile.Read(ref _notificationCount);

        public int RemoteNotificationCount => Volatile.Read(ref _remoteNotificationCount);

        public int SubscriptionCount { get; private set; }

        public int UnsubscriptionCount { get; private set; }

        public OpAmpRemoteConfigurationListener? RemoteConfigurationListener { get; private set; }

        public void NotifyEffectiveConfigChanged()
        {
            Interlocked.Increment(ref _notificationCount);
            _onNotify?.Invoke();
        }

        public void NotifyRemoteConfigStatusChanged()
        {
            Interlocked.Increment(ref _remoteNotificationCount);
            _onRemoteNotify?.Invoke();
        }

        public void Subscribe<T>(IOpAmpListener<T> listener)
            where T : OpAmpMessage
        {
            SubscriptionCount++;
            RemoteConfigurationListener = Assert.IsType<OpAmpRemoteConfigurationListener>(listener);
        }

        public void Unsubscribe<T>(IOpAmpListener<T> listener)
            where T : OpAmpMessage
        {
            UnsubscriptionCount++;
            Assert.Same(RemoteConfigurationListener, listener);
        }
    }
}
