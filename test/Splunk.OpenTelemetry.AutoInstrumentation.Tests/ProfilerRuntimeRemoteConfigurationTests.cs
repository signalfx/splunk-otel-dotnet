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
using Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class ProfilerRuntimeRemoteConfigurationTests
{
    [Fact]
    public void ParseValues_MapsSupportedRemoteConfigurationToRuntimeConfiguration()
    {
        var values = ProfilerRuntimeRemoteConfiguration.ParseValues(
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

        Assert.Equal("true", values[ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled]);
        Assert.Equal("1234", values[ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval]);
#if NET
        Assert.Equal("true", values[ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled]);
        Assert.Equal("123", values[ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples]);
#else
        Assert.DoesNotContain(values, value => value.Key == ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled);
        Assert.DoesNotContain(values, value => value.Key == ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples);
#endif
        Assert.Equal("true", values[ConfigurationKeys.Splunk.Snapshots.Enabled]);
        Assert.Equal("300", values[ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs]);
        Assert.Equal("0.5", values[ConfigurationKeys.Splunk.Snapshots.SelectionRate]);
        Assert.DoesNotContain(values, value => value.Key == ConfigurationKeys.Splunk.Snapshots.HighResolutionTimerEnabled);
        Assert.DoesNotContain(values, value => value.Key == ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerLogsEndpoint);
    }

    [Fact]
    public void ParseValues_UsesDefaultsForPresentEmptyProfilerSections()
    {
        var values = ProfilerRuntimeRemoteConfiguration.ParseValues(
            CreateConfiguration(
                new ProfilerConfiguration
                {
                    AlwaysOn = new AlwaysOn
                    {
                        CpuProfiler = new CpuProfiler(),
#if NET
                        MemoryProfiler = new MemoryProfiler()
#endif
                    },
                    Callgraphs = new CallGraphsConfiguration()
                }));

        Assert.Equal("true", values[ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled]);
        Assert.Equal(Constants.DefaultSamplingInterval.ToString(), values[ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval]);
#if NET
        Assert.Equal("true", values[ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled]);
        Assert.Equal(Constants.DefaultMaxMemorySamples.ToString(), values[ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples]);
#else
        Assert.DoesNotContain(values, value => value.Key == ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled);
        Assert.DoesNotContain(values, value => value.Key == ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples);
#endif
        Assert.Equal("true", values[ConfigurationKeys.Splunk.Snapshots.Enabled]);
        Assert.Equal(Constants.DefaultSnapshotSamplingIntervalMs.ToString(), values[ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs]);
        Assert.Equal(Constants.DefaultSnapshotSelectionRate.ToString(System.Globalization.CultureInfo.InvariantCulture), values[ConfigurationKeys.Splunk.Snapshots.SelectionRate]);
    }

    [Fact]
    public void ParseValues_DisablesProfilerSectionsOmittedFromProfilingConfig()
    {
        var values = ProfilerRuntimeRemoteConfiguration.ParseValues(
            CreateConfiguration(
                new ProfilerConfiguration
                {
                    AlwaysOn = new AlwaysOn
                    {
                        CpuProfiler = new CpuProfiler()
                    }
                }));

        Assert.Equal("true", values[ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled]);
#if NET
        Assert.Equal("false", values[ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled]);
#else
        Assert.DoesNotContain(values, value => value.Key == ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled);
#endif
        Assert.Equal("false", values[ConfigurationKeys.Splunk.Snapshots.Enabled]);
    }

    [Fact]
    public void ParseValues_IgnoresPayloadWithoutProfilingConfig()
    {
        var values = ProfilerRuntimeRemoteConfiguration.ParseValues(new YamlRoot());

        Assert.Empty(values);
    }

#if NET
    [Fact]
    public void Apply_UpdatesMemoryProfilerMaxSamples()
    {
        ProfilerRuntimeConfiguration.Initialize(new PluginSettings(new NameValueConfigurationSource(new NameValueCollection())));

        var result = ProfilerRuntimeConfiguration.Apply(
            new Dictionary<string, string?>
            {
                [ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled] = "true",
                [ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples] = "123"
            });

        Assert.Contains(ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples, result.Applied);
        Assert.True(ProfilerRuntimeConfiguration.Current.MemoryProfilerEnabled);
        Assert.Equal(123u, ProfilerRuntimeConfiguration.Current.MemoryProfilerMaxMemorySamplesPerMinute);
    }
#endif

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
