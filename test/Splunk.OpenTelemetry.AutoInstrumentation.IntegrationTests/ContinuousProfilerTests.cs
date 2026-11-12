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
using Google.Protobuf;
using OpAmp.Proto.V1;
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

    [Fact]
    public async Task RemoteConfigurationEnablesUpdatesAndDisablesCpuProfiling()
    {
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("SPLUNK_PROFILER_ENABLED", "false");
        SetEnvironmentVariable("SPLUNK_OPAMP_REMOTE_CONFIG", "true");
        SetEnvironmentVariable("SPLUNK_PROFILER_CALL_STACK_INTERVAL", "3000");
        SetEnvironmentVariable("SPLUNK_PROFILER_EXPORT_INTERVAL", "1000");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "TestApplication.ContinuousProfiler");
        SetEnvironmentVariable("LONG_RUNNING", "true");

        using var logsCollector = new MockContinuousProfilerCollector(Output);
        SetExporter(logsCollector);
        var enabledConfig = CreateRemoteCpuConfiguration(enabled: true, hash: 1, samplingInterval: 1000);
        var updatedConfig = CreateRemoteCpuConfiguration(enabled: true, hash: 2, samplingInterval: 2000);
        var disabledConfig = CreateRemoteCpuConfiguration(enabled: false, hash: 3);
        var updateSent = false;
        var disableSent = false;
        var lastAppliedConfigHash = ByteString.Empty;
        string[] effectiveConfigProperties = [];
        using var opAmpServer = new MockOpAmpServer(Output, remoteConfiguration: frame =>
        {
            if (frame.RemoteConfigStatus?.Status == RemoteConfigStatuses.Applied)
            {
                lastAppliedConfigHash = frame.RemoteConfigStatus.LastRemoteConfigHash;
            }

            if (frame.EffectiveConfig?.ConfigMap?.ConfigMap.TryGetValue("environment", out var file) == true)
            {
                effectiveConfigProperties = file.Body.ToStringUtf8().Split('\n');
            }

            if (!updateSent &&
                lastAppliedConfigHash.Equals(enabledConfig.ConfigHash) &&
                effectiveConfigProperties.Contains("SPLUNK_PROFILER_ENABLED=true") &&
                effectiveConfigProperties.Contains("SPLUNK_PROFILER_CALL_STACK_INTERVAL=1000"))
            {
                // Effective configuration reporting starts after OpAMP startup. Wait for its report and native samples before advancing.
                updateSent = SpinWait.SpinUntil(() => GetCpuSamplingIntervals(logsCollector.GetAllLogs()).Contains(1000), TestTimeout.Expectation);
            }

            if (!disableSent &&
                lastAppliedConfigHash.Equals(updatedConfig.ConfigHash) &&
                effectiveConfigProperties.Contains("SPLUNK_PROFILER_ENABLED=true") &&
                effectiveConfigProperties.Contains("SPLUNK_PROFILER_CALL_STACK_INTERVAL=2000"))
            {
                disableSent = SpinWait.SpinUntil(() => GetCpuSamplingIntervals(logsCollector.GetAllLogs()).Contains(2000), TestTimeout.Expectation);
            }

            return disableSent ? disabledConfig : updateSent ? updatedConfig : enabledConfig;
        });
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", $"http://localhost:{opAmpServer.Port}/v1/opamp");

        var enabledReported = false;
        var updatedReported = false;
        var disabledReported = false;
        var enabledEffectiveConfigReported = false;
        var updatedEffectiveConfigReported = false;
        var disabledEffectiveConfigReported = false;
        opAmpServer.Expect(
            frame =>
            {
                if (frame.RemoteConfigStatus?.Status == RemoteConfigStatuses.Applied)
                {
                    enabledReported |= frame.RemoteConfigStatus.LastRemoteConfigHash.Equals(enabledConfig.ConfigHash);
                    updatedReported |= frame.RemoteConfigStatus.LastRemoteConfigHash.Equals(updatedConfig.ConfigHash);
                    disabledReported |= frame.RemoteConfigStatus.LastRemoteConfigHash.Equals(disabledConfig.ConfigHash);
                }

                if (frame.EffectiveConfig?.ConfigMap?.ConfigMap.TryGetValue("environment", out var file) == true)
                {
                    var properties = file.Body.ToStringUtf8().Split('\n');
                    enabledEffectiveConfigReported |= properties.Contains("SPLUNK_PROFILER_ENABLED=true") && properties.Contains("SPLUNK_PROFILER_CALL_STACK_INTERVAL=1000");
                    updatedEffectiveConfigReported |= properties.Contains("SPLUNK_PROFILER_ENABLED=true") && properties.Contains("SPLUNK_PROFILER_CALL_STACK_INTERVAL=2000");
                    disabledEffectiveConfigReported |= updatedEffectiveConfigReported && properties.Contains("SPLUNK_PROFILER_ENABLED=false");
                }

                return enabledReported && updatedReported && disabledReported && enabledEffectiveConfigReported && updatedEffectiveConfigReported && disabledEffectiveConfigReported;
            },
            "Remote CPU enable, sampling interval update, and disable are applied and reflected in effective configuration");

        using var process = StartTestApplication();
        Assert.NotNull(process);
        using var helper = new ProcessHelper(process);
        try
        {
            opAmpServer.AssertExpectations(TimeSpan.FromTicks(3 * TestTimeout.Expectation.Ticks));
            Assert.False(process.HasExited, "The application exited before all remote CPU transitions completed.");
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
            }

            Assert.True(process.WaitForExit((int)TestTimeout.ProcessExit.TotalMilliseconds));
            helper.Drain(TimeSpan.FromSeconds(5));
            Output.WriteResult(helper);
        }

        var logsData = logsCollector.GetAllLogs();
        Assert.NotEmpty(logsData);
        await DumpLogRecords(logsData);
        var samplingIntervals = GetCpuSamplingIntervals(logsData).ToArray();
        Assert.Contains(1000, samplingIntervals);
        Assert.Contains(2000, samplingIntervals);
        Assert.All(samplingIntervals, interval => Assert.Contains(interval, new long[] { 1000, 2000 }));
    }

#if NET
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RemoteConfigurationEnablesUpdatesAndDisablesMemoryProfiling(bool isFileBased)
    {
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("SPLUNK_PROFILER_ENABLED", "false");
        SetEnvironmentVariable("SPLUNK_PROFILER_MEMORY_ENABLED", "true");
        SetEnvironmentVariable("SPLUNK_PROFILER_MAX_MEMORY_SAMPLES", "0");
        SetEnvironmentVariable("SPLUNK_OPAMP_REMOTE_CONFIG", "true");
        SetEnvironmentVariable("SPLUNK_PROFILER_EXPORT_INTERVAL", "1000");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "TestApplication.ContinuousProfiler");
        SetEnvironmentVariable("LONG_RUNNING", "true");
        if (isFileBased)
        {
            EnableFileBasedConfig("configRemoteMemoryProfiler.yaml");
        }

        using var logsCollector = new MockContinuousProfilerCollector(Output);
        SetExporter(logsCollector);
        var configurations = new[]
        {
            (Config: CreateRemoteMemoryConfiguration(enabled: true, hash: 1, maxMemorySamples: 100), Enabled: true, Limit: 100u),
            (Config: CreateRemoteMemoryConfiguration(enabled: true, hash: 2, maxMemorySamples: 50), Enabled: true, Limit: 50u),
            (Config: CreateRemoteMemoryConfiguration(enabled: true, hash: 3, maxMemorySamples: 0), Enabled: true, Limit: 200u),
            (Config: CreateRemoteMemoryConfiguration(enabled: true, hash: 4, maxMemorySamples: -1), Enabled: true, Limit: 200u),
            (Config: CreateRemoteMemoryConfiguration(enabled: true, hash: 5), Enabled: true, Limit: 200u),
            (Config: CreateRemoteMemoryConfiguration(enabled: false, hash: 6), Enabled: false, Limit: 0u)
        };
        var completedConfigurations = 0;
        var lastAppliedConfigHash = ByteString.Empty;
        var effectiveConfigBody = string.Empty;
        var previousLogCount = 0;
        using var opAmpServer = new MockOpAmpServer(Output, remoteConfiguration: frame =>
        {
            if (frame.RemoteConfigStatus?.Status == RemoteConfigStatuses.Applied)
            {
                lastAppliedConfigHash = frame.RemoteConfigStatus.LastRemoteConfigHash;
            }

            if (frame.EffectiveConfig?.ConfigMap?.ConfigMap.Values.SingleOrDefault() is { } file)
            {
                effectiveConfigBody = file.Body.ToStringUtf8();
            }

            if (completedConfigurations < configurations.Length)
            {
                var current = configurations[completedConfigurations];
                if (lastAppliedConfigHash.Equals(current.Config.ConfigHash) &&
                    ReportsMemoryConfiguration(effectiveConfigBody, isFileBased, current.Enabled, current.Limit) &&
                    (!current.Enabled || SpinWait.SpinUntil(() => logsCollector.GetAllLogs().Length > previousLogCount, TestTimeout.Expectation)))
                {
                    previousLogCount = logsCollector.GetAllLogs().Length;
                    Volatile.Write(ref completedConfigurations, completedConfigurations + 1);
                }
            }

            return configurations[Math.Min(completedConfigurations, configurations.Length - 1)].Config;
        });
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", $"http://localhost:{opAmpServer.Port}/v1/opamp");
        opAmpServer.Expect(
            _ => Volatile.Read(ref completedConfigurations) == configurations.Length,
            "Remote memory enable, limit update, zero and negative limits, default limit, and disable are applied, exported, and reported");

        using var process = StartTestApplication();
        Assert.NotNull(process);
        using var helper = new ProcessHelper(process);
        try
        {
            opAmpServer.AssertExpectations(TimeSpan.FromTicks(configurations.Length * TestTimeout.Expectation.Ticks));
            Assert.False(process.HasExited, "The application exited before all remote memory transitions completed.");
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
            }

            Assert.True(process.WaitForExit((int)TestTimeout.ProcessExit.TotalMilliseconds));
            helper.Drain(TimeSpan.FromSeconds(5));
            Output.WriteResult(helper);
        }

        var logsData = logsCollector.GetAllLogs();
        Assert.NotEmpty(logsData);
        var records = logsData.SelectMany(data => data.ResourceLogs).SelectMany(resource => resource.ScopeLogs).SelectMany(scope => scope.LogRecords);
        Assert.All(records, record => Assert.Contains(record.Attributes, attribute => attribute.Key == "profiling.data.type" && attribute.Value.StringValue == "allocation"));
        await DumpLogRecords(logsData);
    }

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
            Assert.All(profiles, profile => Assert.All(profile.Samples, sample =>
                Assert.Equal(1000, Assert.Single(sample.Labels, label => profile.StringTables[(int)label.Key] == "source.event.period").Num)));
            ProfilerTestHelpers.RecordsContainFrameCountAttribute(logRecords);
            ProfilerTestHelpers.ResourceContainsExpectedAttributes(dataResourceLog.Resource, "TestApplication.ContinuousProfiler");
            ProfilerTestHelpers.HasNameAndVersionSet(instrumentationLibraryLogs.Scope);

            logRecords.Clear();
        }

        Assert.True(stackTraceForClassHierarchyCount > 0, "At least one stack trace containing class hierarchy should be reported.");
    }

#if NET
    private static bool ReportsMemoryConfiguration(string body, bool isFileBased, bool enabled, uint limit)
    {
        if (isFileBased)
        {
            return enabled
                ? body.Contains("memory_profiler:") && body.Contains($"max_memory_samples: {limit}")
                : !body.Contains("memory_profiler:");
        }

        var properties = body.Split('\n');
        return properties.Contains($"SPLUNK_PROFILER_MEMORY_ENABLED={enabled.ToString().ToLowerInvariant()}") &&
               properties.Contains($"SPLUNK_PROFILER_MAX_MEMORY_SAMPLES={limit}");
    }

    private static AgentRemoteConfig CreateRemoteMemoryConfiguration(bool enabled, byte hash, long? maxMemorySamples = null)
    {
        var yaml = enabled
            ? "distribution:\n  splunk:\n    profiling:\n      always_on:\n        memory_profiler:\n" +
              (maxMemorySamples.HasValue ? $"          max_memory_samples: {maxMemorySamples.Value}\n" : string.Empty)
            : "distribution:\n  splunk:\n    profiling: {}\n";
        var configuration = new AgentRemoteConfig
        {
            ConfigHash = ByteString.CopyFrom([hash]),
            Config = new AgentConfigMap()
        };
        configuration.Config.ConfigMap.Add("splunk.remote.config", new AgentConfigFile
        {
            ContentType = "application/yaml",
            Body = ByteString.CopyFromUtf8(yaml)
        });
        return configuration;
    }
#endif

    private static IEnumerable<long> GetCpuSamplingIntervals(ExportLogsServiceRequest[] logsData)
    {
        foreach (var record in logsData.SelectMany(data => data.ResourceLogs).SelectMany(resource => resource.ScopeLogs).SelectMany(scope => scope.LogRecords))
        {
            using var memoryStream = new MemoryStream(Convert.FromBase64String(record.Body.StringValue));
            using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            var profile = Vendors.ProtoBuf.Serializer.Deserialize<Profile>(gzipStream);
            Assert.NotEmpty(profile.Samples);
            foreach (var sample in profile.Samples)
            {
                yield return Assert.Single(sample.Labels, label => profile.StringTables[(int)label.Key] == "source.event.period").Num;
            }
        }
    }

    private static AgentRemoteConfig CreateRemoteCpuConfiguration(bool enabled, byte hash, uint samplingInterval = 10000)
    {
        var yaml = enabled
            ? $"distribution:\n  splunk:\n    profiling:\n      always_on:\n        cpu_profiler:\n          sampling_interval: {samplingInterval}\n"
            : "distribution:\n  splunk:\n    profiling: {}\n";
        var configuration = new AgentRemoteConfig
        {
            ConfigHash = ByteString.CopyFrom([hash]),
            Config = new AgentConfigMap()
        };
        configuration.Config.ConfigMap.Add("splunk.remote.config", new AgentConfigFile
        {
            ContentType = "application/yaml",
            Body = ByteString.CopyFromUtf8(yaml)
        });
        return configuration;
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
