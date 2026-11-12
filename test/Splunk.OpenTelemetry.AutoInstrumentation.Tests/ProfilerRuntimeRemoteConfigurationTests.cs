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
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration;
using Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

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
        Assert.Equal(1234u, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);
        Assert.Equal(4321u, ProfilerRuntimeConfiguration.StartupCpuProfilerCallStackInterval);
    }

    [Theory]
    [InlineData("300", 1200u)]
    [InlineData("6000", 12000u)]
    public void Apply_AlignsRemoteSamplingIntervalWithStartupSnapshots(
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
        var startupSettings = CreateSettings(settings);
        ProfilerRuntimeConfiguration.Initialize(startupSettings);

        ApplyConfiguration(
            CreateConfiguration(
                new ProfilerConfiguration
                {
                    AlwaysOn = new AlwaysOn(),
                    Callgraphs = new CallGraphsConfiguration
                    {
                        SamplingInterval = 300
                    }
                }));

        Assert.False(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal(0u, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);
        Assert.True(startupSettings.SnapshotsEnabled);
        Assert.Equal(40u, startupSettings.SnapshotsSamplingInterval);
    }

#if NET
    [Theory]
    [InlineData(false, 0u)]
    [InlineData(true, (uint)Constants.DefaultMaxMemorySamples)]
    public void StartupMemorySamples_PreparesExportsForZeroStartupLimitOnlyWhenRemoteConfigurationIsEnabled(bool remoteConfigurationEnabled, uint expectedMaxMemorySamples)
    {
        var startupSettings = new PluginSettings(new YamlRoot
        {
            OpampDevelopment = new OpampDevelopment
            {
                Features = new Features { RemoteConfig = remoteConfigurationEnabled ? new object() : null }
            }
        });
        ProfilerRuntimeConfiguration.Initialize(startupSettings);

        Assert.Equal(0u, startupSettings.MemoryProfilerMaxMemorySamplesPerMinute);
        Assert.False(ProfilerRuntimeConfiguration.Current.MemoryProfilerEnabled);
        Assert.Equal(expectedMaxMemorySamples, ProfilerRuntimeConfiguration.StartupMemoryProfilerMaxMemorySamplesPerMinute);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(201u)]
    [InlineData(uint.MaxValue)]
    public void Apply_UsesDefaultMemoryLimitInsteadOfPreviousRemoteValue(uint? limit)
    {
        ProfilerRuntimeConfiguration.Initialize(CreateSettings());
        ApplyConfiguration(CreateConfiguration(new ProfilerConfiguration
        {
            AlwaysOn = new AlwaysOn { MemoryProfiler = new MemoryProfiler { MaxMemorySamples = 123 } }
        }));
        var memoryProfiler = new MemoryProfiler();
        if (limit.HasValue)
        {
            memoryProfiler.MaxMemorySamples = limit.Value;
        }

        ApplyConfiguration(CreateConfiguration(new ProfilerConfiguration
        {
            AlwaysOn = new AlwaysOn { MemoryProfiler = memoryProfiler }
        }));

        Assert.True(ProfilerRuntimeConfiguration.Current.MemoryProfilerEnabled);
        Assert.Equal((uint)Constants.DefaultMaxMemorySamples, ProfilerRuntimeConfiguration.Current.MemoryProfilerMaxMemorySamplesPerMinute);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Apply_DisablesMemoryProfilingWhenSectionOrConfigurationIsOmitted(bool configurationOmitted)
    {
        ProfilerRuntimeConfiguration.Initialize(CreateSettings(new NameValueCollection
        {
            [ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled] = "true",
            [ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples] = "137"
        }));

        var configuration = configurationOmitted ? null : CreateConfiguration(new ProfilerConfiguration
        {
            AlwaysOn = new AlwaysOn { CpuProfiler = new CpuProfiler() }
        });
        ProfilerRuntimeConfiguration.Apply(configuration, (_, _) => { });

        Assert.False(ProfilerRuntimeConfiguration.Current.MemoryProfilerEnabled);
        Assert.Equal(0u, ProfilerRuntimeConfiguration.Current.MemoryProfilerMaxMemorySamplesPerMinute);
        Assert.Equal(!configurationOmitted, ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
    }

#else
    [Fact]
    public void Apply_IgnoresMemoryProfilingOnNetFramework()
    {
        ProfilerRuntimeConfiguration.Initialize(CreateSettings());
        ApplyConfiguration(CreateConfiguration(new ProfilerConfiguration
        {
            AlwaysOn = new AlwaysOn { MemoryProfiler = new MemoryProfiler { MaxMemorySamples = 123 } }
        }));

        Assert.False(ProfilerRuntimeConfiguration.Current.MemoryProfilerEnabled);
        Assert.Equal(0u, ProfilerRuntimeConfiguration.Current.MemoryProfilerMaxMemorySamplesPerMinute);
    }
#endif

    [Theory]
    [InlineData(false, 0u)]
    [InlineData(true, 4321u)]
    public void Apply_KeepsPreviousStateWhenNativeConfigurationFails(bool initiallyEnabled, uint expectedSamplingInterval)
    {
        ProfilerRuntimeConfiguration.Initialize(CreateSettings(new NameValueCollection
        {
            [ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled] = initiallyEnabled.ToString(),
            [ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval] = "4321",
#if NET
            [ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled] = initiallyEnabled.ToString(),
            [ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples] = "137"
#endif
        }));
        var previous = ProfilerRuntimeConfiguration.Current;
        var configuration = CreateConfiguration(new ProfilerConfiguration
        {
            AlwaysOn = new AlwaysOn
            {
                CpuProfiler = new CpuProfiler { SamplingInterval = 1234 },
                MemoryProfiler = new MemoryProfiler { MaxMemorySamples = 123 }
            }
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProfilerRuntimeConfiguration.Apply(configuration, (_, _) => throw new InvalidOperationException("ActivationFailed")));

        Assert.Equal("ActivationFailed", exception.Message);
        Assert.Same(previous, ProfilerRuntimeConfiguration.Current);
        Assert.Equal(initiallyEnabled, ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal(expectedSamplingInterval, ProfilerRuntimeConfiguration.CurrentCpuProfilerCallStackInterval);
#if NET
        Assert.Equal(initiallyEnabled, ProfilerRuntimeConfiguration.Current.MemoryProfilerEnabled);
        Assert.Equal(137u, ProfilerRuntimeConfiguration.Current.MemoryProfilerMaxMemorySamplesPerMinute);
#else
        Assert.False(ProfilerRuntimeConfiguration.Current.MemoryProfilerEnabled);
        Assert.Equal(0u, ProfilerRuntimeConfiguration.Current.MemoryProfilerMaxMemorySamplesPerMinute);
#endif
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
