// <copyright file="ContinuousProfilerTests.cs" company="Splunk Inc.">
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

using System.Diagnostics;
using System.IO.Compression;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Logs.V1;
using Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;
using Splunk.OpenTelemetry.AutoInstrumentation.Pprof.Proto.Profile;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests;

public class ContinuousProfilerTests : TestHelper
{
    public ContinuousProfilerTests(ITestOutputHelper output)
        : base("ContinuousProfiler", output)
    {
    }

#if NET
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubmitAllocationSamples(bool isFileBased)
    {
        EnableBytecodeInstrumentation();
        if (isFileBased)
        {
            EnableFileBasedConfig("configMemoryProfiller.yaml");
        }
        else
        {
            SetEnvironmentVariable("SPLUNK_PROFILER_MEMORY_ENABLED", "true");
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "TestApplication.ContinuousProfiler");
        }

        using var logsCollector = new MockContinuousProfilerCollector(Output);
        SetExporter(logsCollector);
        RunTestApplication();
        var logsData = logsCollector.GetAllLogs();

        Assert.True(logsData.Length > 0);

        await DumpLogRecords(logsData);

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

            ProfilerTestHelpers.AllShouldHaveBasicAttributes(logRecords, ProfilerTestHelpers.ConstantValuedAttributes("allocation"));
            ProfilerTestHelpers.ProfilesContainAllocationValue(profiles);
            ProfilerTestHelpers.RecordsContainFrameCountAttribute(logRecords);
            ProfilerTestHelpers.ResourceContainsExpectedAttributes(dataResourceLog.Resource, "TestApplication.ContinuousProfiler");
            ProfilerTestHelpers.HasNameAndVersionSet(instrumentationLibraryLogs.Scope);

            logRecords.Clear();
        }
    }
#endif

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubmitThreadSamples(bool isFileBased)
    {
        EnableBytecodeInstrumentation();
        if (isFileBased)
        {
            EnableFileBasedConfig("configCpuProfiller.yaml");
        }
        else
        {
            SetEnvironmentVariable("SPLUNK_PROFILER_ENABLED", "true");
            SetEnvironmentVariable("SPLUNK_PROFILER_CALL_STACK_INTERVAL", "1000");
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "TestApplication.ContinuousProfiler");
        }

        using var logsCollector = new MockContinuousProfilerCollector(Output);
        SetExporter(logsCollector);
        RunTestApplication();
        var logsData = logsCollector.GetAllLogs();
        // The application works for 6 seconds with debug logging enabled we expect at least 2 attempts of thread sampling in CI.
        // On a dev box it is typical to get at least 4 but the CI machines seem slower, using 2
        Assert.True(logsData.Length >= 2, "Expected to receive at least 2 messages with thread samples.");

        await DumpLogRecords(logsData);

        var stackTraceForClassHierarchyCount = 0;
        var expectedStackTrace = string.Join("\n", CreateExpectedStackTrace());

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

            ProfilerTestHelpers.AllShouldHaveBasicAttributes(logRecords, ProfilerTestHelpers.ConstantValuedAttributes("cpu"));
            ProfilerTestHelpers.ProfilesDoNotContainAnyValue(profiles);
            ProfilerTestHelpers.RecordsContainFrameCountAttribute(logRecords);
            ProfilerTestHelpers.ResourceContainsExpectedAttributes(dataResourceLog.Resource, "TestApplication.ContinuousProfiler");
            ProfilerTestHelpers.HasNameAndVersionSet(instrumentationLibraryLogs.Scope);

            logRecords.Clear();
        }

        Assert.True(stackTraceForClassHierarchyCount > 0, "At least one stack trace containing class hierarchy should be reported.");
    }

    private static IEnumerable<string> CreateExpectedStackTrace()
    {
        var stackTrace = new List<string>
        {
            "System.Threading.Thread.Sleep(System.TimeSpan)",
            "My.Custom.Test.Namespace.ClassA.MethodA()"
        };

        return stackTrace;
    }

#if NET
    private static async Task<string> ConfigureRuntime(HttpClient client, string url, Dictionary<string, string?> configuration)
    {
        using var content = new FormUrlEncodedContent(
            configuration.Select(setting => new KeyValuePair<string, string>(setting.Key, setting.Value ?? string.Empty)));
        using var response = await client.PostAsync($"{url}/configure", content);
        Assert.True(response.IsSuccessStatusCode, $"Runtime configuration endpoint returned {(int)response.StatusCode}.");
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task RunProfilerWork(HttpClient client, string url)
    {
        using var response = await client.GetAsync($"{url}/work");
        Assert.True(response.IsSuccessStatusCode, $"Profiler work endpoint returned {(int)response.StatusCode}.");
    }

    private static async Task<ExportLogsServiceRequest[]> WaitForLogs(
        MockContinuousProfilerCollector collector,
        Func<ExportLogsServiceRequest[], bool> predicate,
        string expectation,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TestTimeout.Expectation);
        ExportLogsServiceRequest[] logs;
        do
        {
            logs = collector.GetAllLogs();
            if (predicate(logs))
            {
                return logs;
            }

            await Task.Delay(250);
        }
        while (DateTime.UtcNow < deadline);

        logs = collector.GetAllLogs();
        Assert.True(predicate(logs), $"Expected {expectation}. Received {logs.Length} profiler export requests.");
        return logs;
    }

    private static async Task<int> WaitForSettledLogCount(MockContinuousProfilerCollector collector)
    {
        var previousCount = -1;
        for (var i = 0; i < 6; i++)
        {
            await Task.Delay(500);
            var count = collector.GetAllLogs().Length;
            if (count == previousCount)
            {
                return count;
            }

            previousCount = count;
        }

        return collector.GetAllLogs().Length;
    }

    private static bool ContainsContinuousCpuRecord(ExportLogsServiceRequest[] logs)
    {
        return GetContinuousCpuRecords(logs).Any();
    }

    private static bool ContainsAllocationRecord(ExportLogsServiceRequest[] logs)
    {
        return GetAllocationRecords(logs).Any();
    }

    private static bool ContainsSnapshotRecord(ExportLogsServiceRequest[] logs)
    {
        return GetSnapshotRecords(logs).Any();
    }

    private static List<LogRecord> GetContinuousCpuRecords(ExportLogsServiceRequest[] logs)
    {
        return GetLogRecords(logs)
            .Where(record => HasStringAttribute(record, "profiling.data.type", "cpu") &&
                             !HasAttribute(record, "profiling.instrumentation.source"))
            .ToList();
    }

    private static List<LogRecord> GetAllocationRecords(ExportLogsServiceRequest[] logs)
    {
        return GetLogRecords(logs)
            .Where(record => HasStringAttribute(record, "profiling.data.type", "allocation"))
            .ToList();
    }

    private static List<LogRecord> GetSnapshotRecords(ExportLogsServiceRequest[] logs)
    {
        return GetLogRecords(logs)
            .Where(record => HasStringAttribute(record, "profiling.data.type", "cpu") &&
                             HasStringAttribute(record, "profiling.instrumentation.source", "snapshot"))
            .ToList();
    }

    private static IEnumerable<LogRecord> GetLogRecords(IEnumerable<ExportLogsServiceRequest> logs)
    {
        return logs
            .SelectMany(data => data.ResourceLogs)
            .SelectMany(resourceLogs => resourceLogs.ScopeLogs)
            .SelectMany(scopeLogs => scopeLogs.LogRecords);
    }

    private static bool HasAttribute(LogRecord record, string key)
    {
        return record.Attributes.Any(attribute => attribute.Key == key);
    }

    private static bool HasStringAttribute(LogRecord record, string key, string value)
    {
        return record.Attributes.Any(attribute => attribute.Key == key && attribute.Value.StringValue == value);
    }

    private static void AssertProfilePeriods(IEnumerable<LogRecord> records, long expectedPeriod)
    {
        var periods = records
            .Select(DecodeProfile)
            .SelectMany(GetSourceEventPeriods)
            .ToList();

        Assert.NotEmpty(periods);
        Assert.All(periods, period => Assert.Equal(expectedPeriod, period));
    }

    private static Profile DecodeProfile(LogRecord record)
    {
        var gzip = Convert.FromBase64String(record.Body.StringValue);
        using var memoryStream = new MemoryStream(gzip);
        using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
        return Vendors.ProtoBuf.Serializer.Deserialize<Profile>(gzipStream);
    }

    private static IEnumerable<long> GetSourceEventPeriods(Profile profile)
    {
        foreach (var sample in profile.Samples)
        {
            foreach (var label in sample.Labels)
            {
                var key = profile.StringTables[(int)label.Key];
                if (key == "source.event.period")
                {
                    yield return label.Num;
                }
            }
        }
    }

    private async Task StopRuntimeConfigServer(HttpClient client, string url, Process process, ProcessHelper helper)
    {
        if (!process.HasExited)
        {
            try
            {
                using var response = await client.GetAsync($"{url}/shutdown");
                Output.WriteLine($"Runtime config server shutdown returned {(int)response.StatusCode}.");
            }
            catch (HttpRequestException ex)
            {
                Output.WriteLine($"Runtime config server shutdown request failed: {ex.Message}");
            }
        }

        var processTimeout = !process.WaitForExit((int)TestTimeout.ProcessExit.TotalMilliseconds);
        if (processTimeout)
        {
            process.Kill();
            process.WaitForExit((int)TestTimeout.ProcessExit.TotalMilliseconds);
        }

        helper.Drain(TimeSpan.FromSeconds(5));
        Output.WriteLine("ProcessId: " + process.Id);
        Output.WriteLine(process.HasExited ? "Exit Code: " + process.ExitCode : "Exit Code: <process still running>");
        if (process.HasExited)
        {
            Output.WriteResult(helper);
        }
    }
#endif

    private async Task DumpLogRecords(ExportLogsServiceRequest[] logsData)
    {
        foreach (var data in logsData)
        {
            using var memoryStream = new MemoryStream();
            await System.Text.Json.JsonSerializer.SerializeAsync(memoryStream, data);
            memoryStream.Position = 0;
            using var sr = new StreamReader(memoryStream);
            var readToEnd = await sr.ReadToEndAsync();

            Output.WriteLine(readToEnd);
        }
    }
}
