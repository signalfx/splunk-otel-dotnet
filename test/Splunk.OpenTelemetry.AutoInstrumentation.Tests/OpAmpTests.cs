// <copyright file="OpAmpTests.cs" company="Splunk Inc.">
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
using OpenTelemetry.OpAmp.Client.Settings;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;
using Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class OpAmpTests
{
    [Fact]
    public void ConfigureOptions_EnablesRemoteConfigurationRuntimeModeWhenCpuProfilerIsInitiallyDisabled()
    {
        var pluginSettings = new PluginSettings(new NameValueConfigurationSource(
            new NameValueCollection
            {
                [ConfigurationKeys.Splunk.OpAmp.RemoteConfig] = "true"
            }));
        ProfilerRuntimeConfiguration.Initialize(pluginSettings);
        var settings = new OpAmpClientSettings();

        new OpAmp().ConfigureOptions(settings, pluginSettings);

        Assert.True(settings.EffectiveConfigurationReporting.EnableReporting);
        Assert.True(settings.RemoteConfiguration.AcceptsRemoteConfig);
        Assert.True(settings.RemoteConfiguration.ReportsRemoteConfigStatus);
        Assert.True(ProfilerRuntimeConfiguration.RuntimeConfigurationEnabled);
        Assert.False(ProfilerRuntimeConfiguration.Current.CpuProfilerEnabled);
    }

    [Fact]
    public void ConfigureOptions_RemoteConfigurationIsDisabledByDefault()
    {
        var pluginSettings = new PluginSettings(new NameValueConfigurationSource(new NameValueCollection()));
        ProfilerRuntimeConfiguration.Initialize(pluginSettings);
        var settings = new OpAmpClientSettings();

        new OpAmp().ConfigureOptions(settings, pluginSettings);

        Assert.True(settings.EffectiveConfigurationReporting.EnableReporting);
        Assert.False(settings.RemoteConfiguration.AcceptsRemoteConfig);
        Assert.False(settings.RemoteConfiguration.ReportsRemoteConfigStatus);
        Assert.False(ProfilerRuntimeConfiguration.RuntimeConfigurationEnabled);
    }
}
