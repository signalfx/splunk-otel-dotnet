// <copyright file="SnapshotTests.cs" company="Splunk Inc.">
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

using System.IO.Compression;
#if NETFRAMEWORK
using System.Net.Http;
#endif
using OpenTelemetry.Proto.Common.V1;
using Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;
using Splunk.OpenTelemetry.AutoInstrumentation.Pprof.Proto.Profile;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests
{
    public class SnapshotTests : TestHelper
    {
#if NETFRAMEWORK
        private static readonly HttpClient Client = new();
#endif

        public SnapshotTests(ITestOutputHelper output)
#if NET
            : base("Snapshots", output)
#else
            : base("Snapshots.NetFramework", output)
#endif
        {
        }

#if NET
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SubmitSnapshots(bool isFileBased)
#else
        // [WindowsAdministratorFact]
        [Fact(Skip = "Test is passing locally. Fails in CI due to unknown reasons.")]
        public async Task SubmitSnapshots()
#endif
        {
#if NET
            using var continuousProfilerCollector = new MockContinuousProfilerCollector(Output);
            EnableBytecodeInstrumentation();
            if (isFileBased)
            {
                EnableFileBasedConfig();
            }
            else
            {
                SetEnvironmentVariable("SPLUNK_SNAPSHOT_PROFILER_ENABLED", "true");
                SetEnvironmentVariable("SPLUNK_SNAPSHOT_SAMPLING_INTERVAL", "300");
                // Disable to ensure trace starts on the server side.
                SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_HTTPCLIENT_INSTRUMENTATION_ENABLED", "false");
            }

            SetExporter(continuousProfilerCollector);
            RunTestApplication();
#else
            using var continuousProfilerCollector = new MockContinuousProfilerCollector(Output, "*");
            using var fwPort = FirewallHelper.OpenWinPort(continuousProfilerCollector.Port, Output);

            var collectorUrl = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{continuousProfilerCollector.Port}/v1/logs";

            Dictionary<string, string> environmentVariables = new()
            {
                ["SPLUNK_PROFILER_LOGS_ENDPOINT"] = collectorUrl,
                ["SPLUNK_SNAPSHOT_PROFILER_ENABLED"] = "true",
                ["SPLUNK_SNAPSHOT_SAMPLING_INTERVAL"] = "300",
            };

            var webPort = TcpPortProvider.GetOpenPort();
            var imageName = "testapplication-snapshots-netframework";
            await using var container = await IISContainerTestHelper.StartContainerAsync(imageName, webPort, environmentVariables, Output);

            var requestUri = $"http://127.0.0.1:{webPort}/weatherforecast";
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
            httpRequestMessage.Headers.Add("baggage", "splunk.trace.snapshot.volume=highest");

            _ = await Client.SendAsync(httpRequestMessage);

            Thread.Sleep(TimeSpan.FromSeconds(10));
#endif
            var logsData = continuousProfilerCollector.GetAllLogs();

            Assert.True(logsData.Length > 0);

            var expectedAttributes = GetExpectedLogRecordAttributes();

            var stackTraceForClassHierarchyCount = 0;
            var expectedStackTrace = string.Join("\n", CreateExpectedStackTrace());

            // Thread is blocked for 100ms in test application, we sample every 30ms, so we expect at most 4 samples.
            const int maxMatchCount = 4;

            foreach (var data in logsData)
            {
                var profiles = new List<Profile>();
                var dataResourceLog = data.ResourceLogs[0];
                var instrumentationLibraryLogs = dataResourceLog.ScopeLogs[0];
                var logRecords = instrumentationLibraryLogs.LogRecords;

                foreach (var gzip in logRecords.Select(record => record.Body.StringValue).Select(Convert.FromBase64String))
                {
                    using var memoryStream = new MemoryStream(gzip);
                    using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
                    var profile = Vendors.ProtoBuf.Serializer.Deserialize<Profile>(gzipStream);
                    profiles.Add(profile);
                }

                stackTraceForClassHierarchyCount += profiles.Sum(profile => ProfilerTestHelpers.GetContainsStackTraceCount(profile, expectedStackTrace));

                ProfilerTestHelpers.AllShouldHaveBasicAttributes(logRecords, expectedAttributes);
                ProfilerTestHelpers.RecordsContainFrameCountAttribute(logRecords);
#if NET
                ProfilerTestHelpers.ResourceContainsExpectedAttributes(dataResourceLog.Resource, "TestApplication.Snapshots");
#else
                ProfilerTestHelpers.ResourceContainsExpectedAttributes(dataResourceLog.Resource, "Default Web Site");
#endif
                ProfilerTestHelpers.HasNameAndVersionSet(instrumentationLibraryLogs.Scope);

                logRecords.Clear();
            }

            Assert.InRange(stackTraceForClassHierarchyCount, 1, maxMatchCount);
        }

        private static List<KeyValue> GetExpectedLogRecordAttributes()
        {
            return ProfilerTestHelpers.ConstantValuedAttributes("cpu")
                .Concat(
                [
                    new KeyValue
                    {
                        Key = "profiling.instrumentation.source",
                        Value = new AnyValue { StringValue = "snapshot" }
                    }
                ])
                .ToList();
        }

        private static IEnumerable<string> CreateExpectedStackTrace()
        {
            var stackTrace = new List<string>
            {
#if NET
                "System.Threading.Thread.Sleep(System.Int32)",
                "TestApplication.Snapshots.Controllers.WeatherForecastController.Get()"
#else
                "TestApplication.Snapshots.NetFramework.Controllers.WeatherForecastController.Get()"
#endif
            };

            return stackTrace;
        }
    }
}
