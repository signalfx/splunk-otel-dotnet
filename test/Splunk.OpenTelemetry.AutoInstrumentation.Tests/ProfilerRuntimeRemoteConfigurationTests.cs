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
    [Fact]
    public void Apply_MapsCpuProfilerRemoteConfigurationToRuntimeConfiguration()
    {
        ProfilerRuntimeConfiguration.Initialize(CreateSettings());

        ProfilerRuntimeConfiguration.Apply(
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
        Assert.Equal(1234u, ProfilerRuntimeConfiguration.Current.CpuProfilerCallStackInterval);
    }

    [Fact]
    public void Apply_UsesDefaultsForPresentEmptyCpuProfilerSection()
    {
        ProfilerRuntimeConfiguration.Initialize(CreateSettings());

        ProfilerRuntimeConfiguration.Apply(
            CreateConfiguration(
                new ProfilerConfiguration
                {
                    AlwaysOn = new AlwaysOn
                    {
                        CpuProfiler = new CpuProfiler()
                    }
                }));

        Assert.True(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal((uint)Constants.DefaultSamplingInterval, ProfilerRuntimeConfiguration.Current.CpuProfilerCallStackInterval);
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

        ProfilerRuntimeConfiguration.Apply(
            CreateConfiguration(
                new ProfilerConfiguration
                {
                    AlwaysOn = new AlwaysOn()
                }));

        Assert.False(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal(0u, ProfilerRuntimeConfiguration.Current.CpuProfilerCallStackInterval);
    }

    [Fact]
    public void Apply_IgnoresPayloadWithoutProfilingConfig()
    {
        ProfilerRuntimeConfiguration.Initialize(
            CreateSettings(
                new NameValueCollection
                {
                    { ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled, "true" },
                    { ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval, "1234" }
                }));

        ProfilerRuntimeConfiguration.Apply(new YamlRoot());

        Assert.True(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal(1234u, ProfilerRuntimeConfiguration.Current.CpuProfilerCallStackInterval);
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
        ProfilerRuntimeConfiguration.Initialize(CreateSettings(settings));

        ProfilerRuntimeConfiguration.Apply(
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
                    },
                    Callgraphs = new CallGraphsConfiguration
                    {
                        SamplingInterval = 300
                    }
                }));

        Assert.False(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
        Assert.Equal(0u, ProfilerRuntimeConfiguration.Current.CpuProfilerCallStackInterval);
        Assert.Equal(40u, ProfilerRuntimeConfiguration.Current.SelectedThreadSamplingInterval);
#if NET
        Assert.True(ProfilerRuntimeConfiguration.Current.AllocationSamplingEnabled);
        Assert.Equal(137u, ProfilerRuntimeConfiguration.Current.MaxMemorySamplesPerMinute);
#else
        Assert.False(ProfilerRuntimeConfiguration.Current.AllocationSamplingEnabled);
        Assert.Equal(0u, ProfilerRuntimeConfiguration.Current.MaxMemorySamplesPerMinute);
#endif
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
