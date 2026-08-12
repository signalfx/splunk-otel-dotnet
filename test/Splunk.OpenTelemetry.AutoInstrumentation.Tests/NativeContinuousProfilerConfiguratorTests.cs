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
    [Fact]
    public void NativeMethodContract_UsesAllFiveUpstreamArguments()
    {
        var method = NativeContinuousProfilerConfigurator.FindConfigureNativeContinuousProfilerMethod(typeof(NativeMethodsStub));
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
        var state = new ProfilerRemoteConfigState(cpuProfilerEnabled: true);

#if NET
        object?[] expectedArguments = [true, 156u, true, 123u, 78u];
#else
        object?[] expectedArguments = [true, 156u, false, 0u, 78u];
#endif

        Assert.NotNull(method);
        Assert.Equal(
            [typeof(bool), typeof(uint), typeof(bool), typeof(uint), typeof(uint)],
            method.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(
            expectedArguments,
            NativeContinuousProfilerConfigurator.CreateConfigureNativeContinuousProfilerArguments(startupSettings, state));
    }

    [Fact]
    public void NativeConfiguration_DisablesOnlyRuntimeConfigurableCpuProfiler()
    {
        var startupSettings = new PluginSettings(
            new NameValueConfigurationSource(
                new NameValueCollection
                {
                    [ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled] = "true",
                    [ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval] = "123"
                }));

        var arguments = NativeContinuousProfilerConfigurator.CreateConfigureNativeContinuousProfilerArguments(
            startupSettings,
            new ProfilerRemoteConfigState(cpuProfilerEnabled: false));

        Assert.False((bool)arguments[0]!);
        Assert.Equal(0u, arguments[1]);
    }

    private static class NativeMethodsStub
    {
        public static void ConfigureNativeContinuousProfiler(
            bool threadSamplingEnabled,
            uint threadSamplingInterval,
            bool allocationSamplingEnabled,
            uint maxMemorySamplesPerMinute,
            uint selectedThreadSamplingInterval)
        {
        }
    }
}
