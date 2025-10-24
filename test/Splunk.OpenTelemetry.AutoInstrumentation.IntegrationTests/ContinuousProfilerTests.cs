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

// Continuous Profiler is not supported by .NET Framework.

#if NET

using System.IO.Compression;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Common.V1;
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

    [Fact]
    public async Task SubmitAllocationSamples()
    {
        SetEnvironmentVariable("CORECLR_ENABLE_PROFILING", "1");
        SetEnvironmentVariable("SPLUNK_PROFILER_MEMORY_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "TestApplication.ContinuousProfiler");
        using var logsCollector = new MockLContinuousProfilerCollector(Output);
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
                await using var memoryStream = new MemoryStream(gzip);
                await using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
                var profile = Vendors.ProtoBuf.Serializer.Deserialize<Profile>(gzipStream);
                profiles.Add(profile);
            }

            AllShouldHaveBasicAttributes(logRecords, ConstantValuedAttributes("allocation"));
            ProfilesContainAllocationValue(profiles);
            RecordsContainFrameCountAttribute(logRecords);
            ResourceContainsExpectedAttributes(dataResourceLog.Resource);
            HasNameAndVersionSet(instrumentationLibraryLogs.Scope);

            logRecords.Clear();
        }
    }

    [Fact]
    public async Task SubmitThreadSamples()
    {
        SetEnvironmentVariable("CORECLR_ENABLE_PROFILING", "1");
        SetEnvironmentVariable("SPLUNK_PROFILER_ENABLED", "true");
        SetEnvironmentVariable("SPLUNK_PROFILER_CALL_STACK_INTERVAL", "1000");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "TestApplication.ContinuousProfiler");
        using var logsCollector = new MockLContinuousProfilerCollector(Output);
        SetExporter(logsCollector);
        RunTestApplication();
        var logsData = logsCollector.GetAllLogs();
        // The application works for 6 seconds with debug logging enabled we expect at least 2 attempts of thread sampling in CI.
        // On a dev box it is typical to get at least 4 but the CI machines seem slower, using 2
        Assert.True(logsData.Length >= 2, "Expected to receive at least 2 messages with thread samples.");

        await DumpLogRecords(logsData);

        var containStackTraceForClassHierarchy = false;
        var expectedStackTrace = string.Join("\n", CreateExpectedStackTrace());

        foreach (var data in logsData)
        {
            var profiles = new List<Profile>();
            var dataResourceLog = data.ResourceLogs[0];
            var instrumentationLibraryLogs = dataResourceLog.ScopeLogs[0];
            var logRecords = instrumentationLibraryLogs.LogRecords;

            foreach (var gzip in logRecords.Select(record => record.Body.StringValue).Select(Convert.FromBase64String))
            {
                await using var memoryStream = new MemoryStream(gzip);
                await using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
                var profile = Vendors.ProtoBuf.Serializer.Deserialize<Profile>(gzipStream);
                profiles.Add(profile);
            }

            containStackTraceForClassHierarchy |= profiles.Any(profile => ContainsStackTrace(profile, expectedStackTrace));

            AllShouldHaveBasicAttributes(logRecords, ConstantValuedAttributes("cpu"));
            ProfilesDoNotContainAnyValue(profiles);
            RecordsContainFrameCountAttribute(logRecords);
            ResourceContainsExpectedAttributes(dataResourceLog.Resource);
            HasNameAndVersionSet(instrumentationLibraryLogs.Scope);

            logRecords.Clear();
        }

        Assert.True(containStackTraceForClassHierarchy, "At least one stack trace containing class hierarchy should be reported.");
    }

    private static void ProfilesContainAllocationValue(List<Profile> profiles)
    {
        foreach (var profile in profiles)
        {
            Assert.All(profile.Samples, x => Assert.Single(x.Values));
        }
    }

    private static void ProfilesDoNotContainAnyValue(List<Profile> profiles)
    {
        foreach (var profile in profiles)
        {
            Assert.All(profile.Samples, x => Assert.Empty(x.Values));
        }
    }

    private static void RecordsContainFrameCountAttribute(RepeatedField<LogRecord> logRecords)
    {
        foreach (var logRecord in logRecords)
        {
            Assert.Single(logRecord.Attributes, attr => attr.Key == "profiling.data.total.frame.count");
        }
    }

    private static List<KeyValue> ConstantValuedAttributes(string dataType)
    {
        return
        [
            new()
            {
                Key = "com.splunk.sourcetype",
                Value = new AnyValue { StringValue = "otel.profiling" }
            },

            new()
            {
                Key = "profiling.data.format",
                Value = new AnyValue { StringValue = "pprof-gzip-base64" }
            },

            new()
            {
                Key = "profiling.data.type",
                Value = new AnyValue { StringValue = dataType }
            }
        ];
    }

    private static void AllShouldHaveBasicAttributes(RepeatedField<LogRecord> logRecords, List<KeyValue> attributes)
    {
        foreach (var logRecord in logRecords)
        {
            foreach (var attribute in attributes)
            {
                Assert.Contains(attribute, logRecord.Attributes);
            }
        }
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

    private static void ResourceContainsExpectedAttributes(global::OpenTelemetry.Proto.Resource.V1.Resource resource)
    {
        ResourceExpectorExtensions.AssertProfileResources(resource);
    }

    private static void HasNameAndVersionSet(InstrumentationScope instrumentationScope)
    {
        Assert.Equal("otel.profiling", instrumentationScope.Name);
        Assert.Equal("0.1.0", instrumentationScope.Version);
    }

    private static bool ContainsStackTrace(Profile profile, string expectedStackTrace)
    {
        var frames = profile.Locations
            .SelectMany(location => location.Lines)
            .Select(line => line.FunctionId)
            .Select(functionId => profile.Functions[(int)functionId - 1])
            .Select(function => profile.StringTables[(int)function.Name]);

        var stackTrace = string.Join("\n", frames);
        return stackTrace.Contains(expectedStackTrace);
    }

    private async Task DumpLogRecords(ExportLogsServiceRequest[] logsData)
    {
        foreach (var data in logsData)
        {
            await using var memoryStream = new MemoryStream();
            await System.Text.Json.JsonSerializer.SerializeAsync(memoryStream, data);
            memoryStream.Position = 0;
            using var sr = new StreamReader(memoryStream);
            var readToEnd = await sr.ReadToEndAsync();

            Output.WriteLine(readToEnd);
        }
    }
}
#endif
