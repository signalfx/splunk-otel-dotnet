// <copyright file="ProfilerRuntimeRemoteConfigurationTests.cs" company="Splunk Inc.">
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
using System.Diagnostics;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration;
using Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.Snapshots;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class ProfilerRuntimeRemoteConfigurationTests
{
    [Theory]
    [InlineData(false, 0u)]
    [InlineData(true, (uint)Constants.DefaultSamplingInterval)]
    public void StartupSamplingInterval_PreparesCpuExportsOnlyWhenRemoteConfigurationIsEnabledForYamlWithoutProfiling(
        bool remoteConfigurationEnabled,
        uint expectedSamplingInterval)
    {
        ProfilerRuntimeConfiguration.Initialize(new PluginSettings(new YamlRoot
        {
            OpampDevelopment = new OpampDevelopment
            {
                Features = new Features
                {
                    RemoteConfig = remoteConfigurationEnabled ? new object() : null
                }
            }
        }));

        Assert.False(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal(expectedSamplingInterval, ProfilerRuntimeConfiguration.StartupCpuProfilerCallStackInterval);
    }

    [Theory]
    [InlineData(false, false, 0u)]
    [InlineData(false, true, 1234u)]
    [InlineData(true, false, 1234u)]
    [InlineData(true, true, 1234u)]
    public void StartupSamplingInterval_PreparesCpuExportsWhenCpuProfilingOrRemoteConfigurationIsEnabled(
        bool cpuProfilerEnabled,
        bool remoteConfigurationEnabled,
        uint expectedSamplingInterval)
    {
        ProfilerRuntimeConfiguration.Initialize(CreateSettings(new NameValueCollection
        {
            [ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled] = cpuProfilerEnabled.ToString(),
            [ConfigurationKeys.Splunk.OpAmp.RemoteConfig] = remoteConfigurationEnabled.ToString(),
            [ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval] = "1234"
        }));

        Assert.Equal(expectedSamplingInterval, ProfilerRuntimeConfiguration.StartupCpuProfilerCallStackInterval);
    }

    [Fact]
    public void PluginSettingsHelper_UsesExporterDefaultsWhenYamlOmitsProfiling()
    {
        var pluginSettings = new PluginSettings(
            new YamlRoot
            {
                OpampDevelopment = new OpampDevelopment
                {
                    Features = new Features
                    {
                        RemoteConfig = new object()
                    }
                }
            });

        Assert.Equal(0u, pluginSettings.ProfilerExportInterval);
        Assert.Equal(0u, pluginSettings.ProfilerHttpClientTimeout);
        Assert.Equal(
            (uint)Constants.DefaultProfilerExportInterval,
            PluginSettingsHelper.GetFinalExportInterval(pluginSettings.ProfilerExportInterval));
        Assert.Equal(
            (uint)Constants.DefaultProfilerExportTimeout,
            PluginSettingsHelper.GetFinalExportTimeout(pluginSettings.ProfilerHttpClientTimeout));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Apply_UsesRemoteSamplingInterval(bool initiallyEnabled)
    {
        ProfilerRuntimeConfiguration.Initialize(
            CreateSettings(
                new NameValueCollection
                {
                    { ConfigurationKeys.Splunk.OpAmp.RemoteConfig, "true" },
                    { ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled, initiallyEnabled.ToString() },
                    { ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval, "4321" }
                }));

        ApplyConfiguration(
            CreateConfiguration(
                new ProfilerConfiguration
                {
                    Exporter = new ExporterConfig
                    {
                        OtlpLogHttp = new OtlpLogHttpConfig
                        {
                            Endpoint = "http://ignored:4318/v1/logs"
                        }
                    },
                    AlwaysOn = new AlwaysOn
                    {
                        CpuProfiler = new CpuProfiler
                        {
                            SamplingInterval = 1234
                        },
#if NET
                        MemoryProfiler = new MemoryProfiler
                        {
                            MaxMemorySamples = 123
                        }
#endif
                    },
                    Callgraphs = new CallGraphsConfiguration
                    {
                        SamplingInterval = 300,
                        SelectionProbability = 0.5,
                        HighResolutionTimerEnabled = true
                    }
                }));

        Assert.True(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal(1200u, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);
        Assert.Equal(4321u, ProfilerRuntimeConfiguration.StartupCpuProfilerCallStackInterval);
        Assert.True(ProfilerRuntimeConfiguration.Current.SnapshotsEnabled);
        Assert.Equal(40u, ProfilerRuntimeConfiguration.Current.SnapshotsSamplingInterval);
        Assert.Equal(0.5, ProfilerRuntimeConfiguration.Current.SnapshotsSelectionRate);
    }

    [Theory]
    [InlineData("300", 1200u)]
    [InlineData("6000", 12000u)]
    public void Apply_AlignsRemoteCpuIntervalWithStartupSnapshots(
        string snapshotSamplingInterval,
        uint expectedCpuSamplingInterval)
    {
        ProfilerRuntimeConfiguration.Initialize(
            CreateSettings(
                new NameValueCollection
                {
                    { ConfigurationKeys.Splunk.OpAmp.RemoteConfig, "true" },
                    { ConfigurationKeys.Splunk.Snapshots.Enabled, "true" },
                    { ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs, snapshotSamplingInterval }
                }));

        ApplyConfiguration(
            CreateConfiguration(
                new ProfilerConfiguration
                {
                    Callgraphs = new CallGraphsConfiguration
                    {
                        SamplingInterval = 80
                    },
                    AlwaysOn = new AlwaysOn
                    {
                        CpuProfiler = new CpuProfiler
                        {
                            SamplingInterval = 1234
                        }
                    }
                }));

        Assert.True(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal(expectedCpuSamplingInterval, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);
        Assert.Equal(uint.Parse(snapshotSamplingInterval), ProfilerRuntimeConfiguration.Current.SnapshotsSamplingInterval);
        Assert.True(
            ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval >
            uint.Parse(snapshotSamplingInterval));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Apply_UsesDefaultSamplingIntervalWhenRemoteIntervalIsOmittedOrZero(bool zeroInterval)
    {
        ProfilerRuntimeConfiguration.Initialize(CreateSettings(new NameValueCollection
        {
            [ConfigurationKeys.Splunk.OpAmp.RemoteConfig] = "true",
            [ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval] = "4321"
        }));

        var cpuProfiler = new CpuProfiler();
        if (zeroInterval)
        {
            cpuProfiler.SamplingInterval = 0;
        }

        ApplyConfiguration(CreateConfiguration(new ProfilerConfiguration
        {
            AlwaysOn = new AlwaysOn { CpuProfiler = cpuProfiler }
        }));

        Assert.True(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal((uint)Constants.DefaultSamplingInterval, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);
    }

    [Fact]
    public void Apply_UsesDefaultInsteadOfPreviousRemoteSamplingIntervalWhenOmitted()
    {
        ProfilerRuntimeConfiguration.Initialize(CreateSettings());
        var configuration = CreateConfiguration(new ProfilerConfiguration
        {
            AlwaysOn = new AlwaysOn { CpuProfiler = new CpuProfiler { SamplingInterval = 1234 } }
        });
        ApplyConfiguration(configuration);
        Assert.Equal(1234u, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);

        ApplyConfiguration(CreateConfiguration(new ProfilerConfiguration
        {
            AlwaysOn = new AlwaysOn { CpuProfiler = new CpuProfiler() }
        }));

        Assert.True(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal((uint)Constants.DefaultSamplingInterval, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);
    }

    [Fact]
    public void Apply_DisablesCpuProfilerWhenCpuProfilerSectionIsOmitted()
    {
        ProfilerRuntimeConfiguration.Initialize(
            CreateSettings(
                new NameValueCollection
                {
                    { ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled, "true" },
                    { ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval, "1234" }
                }));

        ApplyConfiguration(
            CreateConfiguration(
                new ProfilerConfiguration
                {
                    AlwaysOn = new AlwaysOn()
                }));

        Assert.False(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal(0u, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);
    }

    [Fact]
    public void Apply_DisablesCpuProfilerWhenProfilingSectionIsOmitted()
    {
        ProfilerRuntimeConfiguration.Initialize(
            CreateSettings(
                new NameValueCollection
                {
                    { ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled, "true" },
                    { ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval, "1234" }
                }));

        ApplyConfiguration(new YamlRoot());

        Assert.False(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal(0u, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);
    }

    [Fact]
    public void Apply_IgnoresUnsupportedProfilerSettings()
    {
        var settings = new NameValueCollection
        {
            { ConfigurationKeys.Splunk.Snapshots.Enabled, "true" },
            { ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs, "40" }
        };
#if NET
        settings.Add(ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled, "true");
        settings.Add(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples, "137");
#endif
        var startupSettings = CreateSettings(settings);
        ProfilerRuntimeConfiguration.Initialize(startupSettings);

        ApplyConfiguration(
            CreateConfiguration(
                new ProfilerConfiguration
                {
                    AlwaysOn = new AlwaysOn
                    {
#if NET
                        MemoryProfiler = new MemoryProfiler
                        {
                            MaxMemorySamples = 123
                        }
#endif
                    }
                }));

        Assert.False(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal(0u, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);
        Assert.False(ProfilerRuntimeConfiguration.Current.SnapshotsEnabled);
        Assert.True(startupSettings.SnapshotsEnabled);
        Assert.Equal(40u, startupSettings.SnapshotsSamplingInterval);
#if NET
        Assert.True(startupSettings.MemoryProfilerEnabled);
        Assert.Equal(137u, startupSettings.MemoryProfilerMaxMemorySamplesPerMinute);
#endif
    }

    [Theory]
    [InlineData(false, 0u)]
    [InlineData(true, 4321u)]
    public void Apply_KeepsPreviousStateWhenNativeConfigurationFails(bool initiallyEnabled, uint expectedSamplingInterval)
    {
        ProfilerRuntimeConfiguration.Initialize(CreateSettings(new NameValueCollection
        {
            [ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled] = initiallyEnabled.ToString(),
            [ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval] = "4321"
        }));
        var configuration = CreateConfiguration(new ProfilerConfiguration
        {
            AlwaysOn = new AlwaysOn { CpuProfiler = new CpuProfiler { SamplingInterval = 1234 } }
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProfilerRuntimeConfiguration.Apply(configuration, (_, _) => throw new InvalidOperationException("ActivationFailed")));

        Assert.Equal("ActivationFailed", exception.Message);
        Assert.Equal(initiallyEnabled, ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal(expectedSamplingInterval, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);
    }

    [Fact]
    public void Apply_CommitsStateAfterNativeConfigurationSucceeds()
    {
        var settings = CreateSettings();
        ProfilerRuntimeConfiguration.Initialize(settings);
        var configuration = CreateConfiguration(new ProfilerConfiguration
        {
            AlwaysOn = new AlwaysOn { CpuProfiler = new CpuProfiler { SamplingInterval = 1234 } }
        });
        var nativeConfigurationApplied = false;

        ProfilerRuntimeConfiguration.Apply(configuration, (startupSettings, state) =>
        {
            Assert.Same(settings, startupSettings);
            Assert.True(state.CpuProfilerEnabled);
            Assert.Equal(1234u, state.CpuProfilerCallStackInterval);
            Assert.False(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
            Assert.Equal(0u, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);
            nativeConfigurationApplied = true;
        });

        Assert.True(nativeConfigurationApplied);
        Assert.True(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal(1234u, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CurrentReads_DoNotWaitForNativeConfiguration(bool nativeSucceeds)
    {
        ProfilerRuntimeConfiguration.Initialize(CreateSettings());
        var previous = ProfilerRuntimeConfiguration.Current;
        var selector = new RuntimeSnapshotSelector();
        var context = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        var configuration = CreateConfiguration(new ProfilerConfiguration
        {
            AlwaysOn = new AlwaysOn { CpuProfiler = new CpuProfiler { SamplingInterval = 1200 } },
            Callgraphs = new CallGraphsConfiguration { SelectionProbability = 1.0 }
        });
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        var apply = Task.Run(() => ProfilerRuntimeConfiguration.Apply(configuration, (_, _) =>
        {
            entered.TrySetResult(true);
            Assert.True(release.Wait(TimeSpan.FromSeconds(10)));
            if (!nativeSucceeds)
            {
                throw new InvalidOperationException("ActivationFailed");
            }
        }));

        try
        {
            await CompletesAsync(entered.Task);
            await CompletesAsync(Task.Run(() =>
            {
                Assert.Same(previous, ProfilerRuntimeConfiguration.Current);
                Assert.Equal(0u, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);
                Assert.False(selector.Select(context));
            }));
        }
        finally
        {
            release.Set();
            if (nativeSucceeds)
            {
                await CompletesAsync(apply);
            }
            else
            {
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => apply);
                Assert.Equal("ActivationFailed", exception.Message);
            }
        }

        Assert.Equal(nativeSucceeds, ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal(nativeSucceeds ? 1200u : 0u, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);
        Assert.Equal(nativeSucceeds, selector.Select(context));
        if (!nativeSucceeds)
        {
            Assert.Same(previous, ProfilerRuntimeConfiguration.Current);
        }
    }

    [Fact]
    public async Task ReadCurrent_WaitsForNativeConfigurationAndManagedPublication()
    {
        ProfilerRuntimeConfiguration.Initialize(CreateSettings());
        var configuration = CreateConfiguration(new ProfilerConfiguration
        {
            AlwaysOn = new AlwaysOn { CpuProfiler = new CpuProfiler { SamplingInterval = 1234 } }
        });
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var readStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        var nativeInterval = 0u;
        var apply = Task.Run(() => ProfilerRuntimeConfiguration.Apply(configuration, (_, state) =>
        {
            nativeInterval = state.CpuProfilerCallStackInterval;
            entered.TrySetResult(true);
            Assert.True(release.Wait(TimeSpan.FromSeconds(10)));
        }));
        var read = Task.Run(async () =>
        {
            await entered.Task;
            readStarted.TrySetResult(true);
            return ProfilerRuntimeConfiguration.ReadCurrent(state => (nativeInterval, state.CpuProfilerCallStackInterval));
        });

        try
        {
            await CompletesAsync(readStarted.Task);
            Assert.NotSame(read, await Task.WhenAny(read, Task.Delay(TimeSpan.FromMilliseconds(100))));
        }
        finally
        {
            release.Set();
            await CompletesAsync(apply);
            await CompletesAsync(read);
        }

        Assert.Equal((1234u, 1234u), await read);
    }

    [Theory]
    [InlineData(0u, 0.0, 0.01)]
    [InlineData(uint.MaxValue, double.NaN, 0.01)]
    [InlineData(80u, 2.0, 1.0)]
    [InlineData(300u, 0.75, 0.75)]
    public void Apply_NormalizesCallgraphProbabilityAndIgnoresRemoteSamplingInterval(
        uint interval,
        double probability,
        double expectedProbability)
    {
        ProfilerRuntimeConfiguration.Initialize(CreateSettings(new NameValueCollection
        {
            [ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs] = "80"
        }));

        ApplyConfiguration(CreateConfiguration(new ProfilerConfiguration
        {
            Callgraphs = new CallGraphsConfiguration { SamplingInterval = interval, SelectionProbability = probability }
        }));

        var state = ProfilerRuntimeConfiguration.Current;
        Assert.False(state.CpuProfilerEnabled);
        Assert.True(state.SnapshotsEnabled);
        Assert.Equal(80u, state.SnapshotsSamplingInterval);
        Assert.Equal(expectedProbability, state.SnapshotsSelectionRate);
    }

    [Fact]
    public void Apply_UpdatesTraceSelectionOnlyAfterNativeCommitAndKeepsStartupSamplingPeriod()
    {
        ProfilerRuntimeConfiguration.Initialize(CreateSettings(new NameValueCollection
        {
            [ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs] = "80"
        }));
        var selector = new RuntimeSnapshotSelector();
        var context = new ActivityContext(
            ActivityTraceId.CreateFromString("00000000000000000000000008000000".AsSpan()),
            ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.Recorded);
        Assert.False(selector.Select(context));

        ApplyConfiguration(CreateConfiguration(new ProfilerConfiguration
        {
            Callgraphs = new CallGraphsConfiguration { SamplingInterval = 80, SelectionProbability = 0.25 }
        }));
        Assert.False(selector.Select(context));
        Assert.Equal(80u, ProfilerRuntimeConfiguration.Current.SnapshotsSamplingInterval);

        ApplyConfiguration(CreateConfiguration(new ProfilerConfiguration
        {
            Callgraphs = new CallGraphsConfiguration { SamplingInterval = 300, SelectionProbability = 0.75 }
        }));
        Assert.True(selector.Select(context));
        Assert.Equal(80u, ProfilerRuntimeConfiguration.Current.SnapshotsSamplingInterval);
        var committed = ProfilerRuntimeConfiguration.Current;

        var rejected = CreateConfiguration(new ProfilerConfiguration
        {
            Callgraphs = new CallGraphsConfiguration { SamplingInterval = 40, SelectionProbability = 0.01 }
        });
        Assert.Throws<InvalidOperationException>(() =>
            ProfilerRuntimeConfiguration.Apply(rejected, (_, _) => throw new InvalidOperationException("ActivationFailed")));

        Assert.Same(committed, ProfilerRuntimeConfiguration.Current);
        Assert.True(selector.Select(context));
        Assert.Equal(80u, ProfilerRuntimeConfiguration.Current.SnapshotsSamplingInterval);

        ApplyConfiguration(CreateConfiguration(new ProfilerConfiguration { Callgraphs = new CallGraphsConfiguration() }));
        Assert.Equal(80u, ProfilerRuntimeConfiguration.Current.SnapshotsSamplingInterval);
        Assert.Equal(0.01, ProfilerRuntimeConfiguration.Current.SnapshotsSelectionRate);

        ApplyConfiguration(new YamlRoot());
        Assert.False(ProfilerRuntimeConfiguration.Current.SnapshotsEnabled);
        Assert.False(selector.Select(context));
        Assert.Equal(80u, ProfilerRuntimeConfiguration.Current.SnapshotsSamplingInterval);
    }

    private static async Task CompletesAsync(Task task)
    {
        Assert.Same(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5))));
        await task;
    }

    private static void ApplyConfiguration(YamlRoot configuration)
    {
        ProfilerRuntimeConfiguration.Apply(configuration, (_, _) => { });
    }

    private static PluginSettings CreateSettings(NameValueCollection? configuration = null)
    {
        return new PluginSettings(new NameValueConfigurationSource(configuration ?? new NameValueCollection()));
    }

    private static YamlRoot CreateConfiguration(ProfilerConfiguration profiling)
    {
        return new YamlRoot
        {
            Distribution = new Distribution
            {
                Splunk = new SplunkConfiguration
                {
                    Profiling = profiling
                }
            }
        };
    }
}
