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

using System.IO.Compression;
using OpenTelemetry.Proto.Collector.Logs.V1;
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
