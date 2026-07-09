// <copyright file="EffectiveConfigStaticSettingsTests.cs" company="Splunk Inc.">
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
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class EffectiveConfigStaticSettingsTests
{
    [Fact]
    public void Constructor_CapturesPluginSettingsAndIgnoresLaterMutations()
    {
        var configuration = new NameValueCollection
        {
            { ConfigurationKeys.Splunk.Snapshots.Enabled, "true" },
            { ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs, "5000" }
        };
        var settings = new PluginSettings(new NameValueConfigurationSource(configuration));
        var effectiveSettings = new EffectiveConfigStaticSettings(settings);

        settings.SnapshotsEnabled = false;
        settings.SnapshotsSamplingInterval = 1000;

        Assert.Null(effectiveSettings.FileBasedConfigFileName);
        Assert.Equal(10000U, effectiveSettings.CpuProfilerCallStackInterval);
        Assert.Equal(5000U, effectiveSettings.SnapshotSamplingInterval);
    }

    [Fact]
    public void Constructor_CapturesFileBasedConfigMetadata()
    {
        var settings = new PluginSettings(new YamlRoot(), "stable.yaml");
        var effectiveSettings = new EffectiveConfigStaticSettings(settings);

        Assert.Equal("stable.yaml", effectiveSettings.FileBasedConfigFileName);
    }
}
