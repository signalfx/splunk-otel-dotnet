// <copyright file="OpAmpReportDispatcherTests.cs" company="Splunk Inc.">
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
using OpenTelemetry.OpAmp.Client;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using static Splunk.OpenTelemetry.AutoInstrumentation.Tests.OpAmpTestHelpers;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class OpAmpReportDispatcherTests
{
    [Fact]
    public async Task DispatchTimeoutDoesNotWaitForNonCooperativeTransport()
    {
        var releaseRequest = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestProbe = new OpAmpHttpRequestProbe(onRequest: async (_, _) =>
        {
            try
            {
                await releaseRequest.Task.ConfigureAwait(false);
            }
            finally
            {
                requestCompleted.TrySetResult(true);
            }
        });
        using var innerClient = new HttpClient(requestProbe);
        using var client = new OpAmpClient(settings =>
        {
            settings.HttpClientFactory = () => innerClient;
            settings.EffectiveConfigurationReporting.EnableReporting = true;
        });
        var recorder = new EffectiveConfigRecorder(
            new EffectiveConfigStaticSettings(
                new PluginSettings(new NameValueConfigurationSource(new NameValueCollection()))),
            openTelemetrySdkDisabled: false,
            () => null);
        var reporter = EffectiveConfigReporter.CreateValidated(recorder, EffectiveProfilerFeatures.None);
        var dispatcher = new OpAmpReportDispatcher(TimeSpan.FromMilliseconds(50));

        try
        {
            var dispatchTask = dispatcher.DispatchEffectiveConfigAsync(client, reporter, CancellationToken.None);
            await requestProbe.WaitForCountAsync(1);
            await WaitForCompletionAsync(dispatchTask);

            Assert.Equal(OpAmpDispatchResult.Failed, await dispatchTask);
            Assert.False(releaseRequest.Task.IsCompleted);
        }
        finally
        {
            releaseRequest.TrySetResult(true);
            await WaitForCompletionAsync(requestCompleted.Task);
        }
    }
}
#endif
