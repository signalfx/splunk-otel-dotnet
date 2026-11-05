// <copyright file="NativeContinuousProfilerConfiguratorTests.cs" company="Splunk Inc.">
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
using Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class NativeContinuousProfilerConfiguratorTests
{
    public enum ApplyResult
    {
        /// <summary>The runtime configuration was committed.</summary>
        Applied,

        /// <summary>The requested configuration was already active.</summary>
        NoChange,

        /// <summary>The runtime rejected the requested configuration.</summary>
        RejectedInvalidConfiguration,

        /// <summary>The runtime could not activate profiling.</summary>
        ActivationFailed,

        /// <summary>The runtime is shutting down.</summary>
        ShuttingDown
    }

    private enum Authority
    {
        Seed = 1,
        ControlPlane = 2
    }

    [Fact]
    public void NativeConfiguration_UsesControlPlaneAndPreservesStartupNonCpuSettings()
    {
        var binding = new NativeContinuousProfilerConfigurator.RuntimeConfigurationBinding(typeof(NativeMethodsStub));
        var startupSettings = new PluginSettings(
            new NameValueConfigurationSource(
                new NameValueCollection
                {
                    [ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled] = "true",
                    [ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval] = "156",
                    [ConfigurationKeys.Splunk.Snapshots.Enabled] = "true",
                    [ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs] = "78",
#if NET
                    [ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled] = "true",
                    [ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples] = "123"
#endif
                }));
        NativeMethodsStub.Result = ApplyResult.Applied;

        binding.Configure(startupSettings, new ProfilerRemoteConfigState(cpuProfilerEnabled: true, cpuProfilerCallStackInterval: 312));

        Assert.Equal(Authority.ControlPlane, NativeMethodsStub.LastAuthority);
        Assert.Equal(312u, NativeMethodsStub.LastConfiguration.CpuSamplingInterval);
        Assert.Equal(78u, NativeMethodsStub.LastConfiguration.SnapshotSamplingInterval);
#if NET
        Assert.Equal(123u, NativeMethodsStub.LastConfiguration.MaxMemorySamplesPerMinute);
#else
        Assert.Equal(0u, NativeMethodsStub.LastConfiguration.MaxMemorySamplesPerMinute);
#endif
    }

    [Fact]
    public void NativeConfiguration_DisablesOnlyRuntimeConfigurableCpuProfiler()
    {
        var startupSettings = new PluginSettings(
            new NameValueConfigurationSource(
                new NameValueCollection
                {
                    [ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled] = "true",
                    [ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval] = "156",
                    [ConfigurationKeys.Splunk.Snapshots.Enabled] = "true",
                    [ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs] = "78",
#if NET
                    [ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled] = "true",
                    [ConfigurationKeys.Splunk.AlwaysOnProfiler.ProfilerMaxMemorySamples] = "123"
#endif
                }));
        var binding = new NativeContinuousProfilerConfigurator.RuntimeConfigurationBinding(typeof(NativeMethodsStub));
        NativeMethodsStub.Result = ApplyResult.Applied;

        binding.Configure(startupSettings, new ProfilerRemoteConfigState(cpuProfilerEnabled: false, cpuProfilerCallStackInterval: 156));

        Assert.Equal(0u, NativeMethodsStub.LastConfiguration.CpuSamplingInterval);
        Assert.Equal(78u, NativeMethodsStub.LastConfiguration.SnapshotSamplingInterval);
#if NET
        Assert.Equal(123u, NativeMethodsStub.LastConfiguration.MaxMemorySamplesPerMinute);
#else
        Assert.Equal(0u, NativeMethodsStub.LastConfiguration.MaxMemorySamplesPerMinute);
#endif
    }

    [Fact]
    public void NativeConfiguration_AcceptsUnchangedConfiguration()
    {
        var binding = new NativeContinuousProfilerConfigurator.RuntimeConfigurationBinding(typeof(NativeMethodsStub));
        var startupSettings = new PluginSettings(new NameValueConfigurationSource(new NameValueCollection()));
        NativeMethodsStub.Result = ApplyResult.NoChange;

        binding.Configure(startupSettings, new ProfilerRemoteConfigState(cpuProfilerEnabled: false, cpuProfilerCallStackInterval: 0));

        Assert.Equal(0u, NativeMethodsStub.LastConfiguration.CpuSamplingInterval);
        Assert.Equal(0u, NativeMethodsStub.LastConfiguration.MaxMemorySamplesPerMinute);
    }

    [Theory]
    [InlineData(ApplyResult.RejectedInvalidConfiguration)]
    [InlineData(ApplyResult.ActivationFailed)]
    [InlineData(ApplyResult.ShuttingDown)]
    public void NativeConfiguration_RejectsFailedApplyResults(ApplyResult result)
    {
        var binding = new NativeContinuousProfilerConfigurator.RuntimeConfigurationBinding(typeof(NativeMethodsStub));
        var startupSettings = new PluginSettings(new NameValueConfigurationSource(new NameValueCollection()));
        NativeMethodsStub.Result = result;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            binding.Configure(startupSettings, new ProfilerRemoteConfigState(cpuProfilerEnabled: true, cpuProfilerCallStackInterval: 1234)));

        Assert.Contains(result.ToString(), exception.Message, StringComparison.Ordinal);
    }

    private readonly struct RuntimeConfiguration
    {
        public RuntimeConfiguration(uint cpuSamplingInterval, uint snapshotSamplingInterval, uint maxMemorySamplesPerMinute)
        {
            CpuSamplingInterval = cpuSamplingInterval;
            SnapshotSamplingInterval = snapshotSamplingInterval;
            MaxMemorySamplesPerMinute = maxMemorySamplesPerMinute;
        }

        public uint CpuSamplingInterval { get; }

        public uint SnapshotSamplingInterval { get; }

        public uint MaxMemorySamplesPerMinute { get; }
    }

    private readonly struct RuntimeState
    {
    }

    private static class NativeMethodsStub
    {
        public static ApplyResult Result { get; set; }

        public static RuntimeConfiguration LastConfiguration { get; private set; }

        public static Authority LastAuthority { get; private set; }

        public static ApplyResult ApplyContinuousProfilerConfiguration(
            RuntimeConfiguration configuration,
            Authority authority,
            out RuntimeState state)
        {
            LastConfiguration = configuration;
            LastAuthority = authority;
            state = default;
            return Result;
        }
    }
}
