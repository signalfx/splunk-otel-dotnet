// <copyright file="PluginEffectiveConfigTests.cs" company="Splunk Inc.">
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

using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.OpAmp.Client.Settings;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class PluginEffectiveConfigTests
{
    [Fact]
    public void EffectiveConfigCaptureHooks_DoNotCreateReporter_WhenOpAmpIsDisabled()
    {
        var reporterCreated = false;
        var plugin = CreatePlugin(false, () => reporterCreated = true);

        plugin.ConfigureLogsOptions(new OpenTelemetryLoggerOptions());
        plugin.ConfigureLogsOptions(new OtlpExporterOptions());
        plugin.Initialized();

        Assert.False(reporterCreated);
    }

    [Fact]
    public void EffectiveConfigCaptureHooks_CreateReporter_WhenOpAmpIsEnabled()
    {
        var reporterCreated = false;
        var plugin = CreatePlugin(true, () => reporterCreated = true);

        plugin.ConfigureLogsOptions(new OpenTelemetryLoggerOptions());

        Assert.True(reporterCreated);
    }

    [Fact]
    public void ConfigureOpAmpOptions_EnablesEffectiveConfigReporting()
    {
        var plugin = CreatePlugin(false, () => { });
        var settings = new OpAmpClientSettings();

        plugin.ConfigureOpAmpOptions(settings);

        Assert.True(settings.EffectiveConfigurationReporting.EnableReporting);
    }

    private static Plugin CreatePlugin(bool opAmpEnabled, Action onReporterCreated)
    {
        return new Plugin(() =>
        {
            if (!opAmpEnabled)
            {
                return null;
            }

            onReporterCreated();
            return new EffectiveConfigReporter(() => null);
        });
    }
}
