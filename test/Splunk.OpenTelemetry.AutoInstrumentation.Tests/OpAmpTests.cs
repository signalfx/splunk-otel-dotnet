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
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class OpAmpTests
{
    [Fact]
    public void ConfigureEffectiveConfigReporting_EnablesEffectiveConfigReportingAfterSuccessfulPreflight()
    {
        var settings = new OpAmpClientSettings();
        var opAmp = CreateOpAmp();

        opAmp.ConfigureEffectiveConfigReporting(settings);

        Assert.True(settings.EffectiveConfigurationReporting.EnableReporting);
    }

    [Fact]
    public void ConfigureEffectiveConfigReporting_DoesNotEnableReporting_WhenAutomaticSdkSetupIsDisabled()
    {
        var settings = new OpAmpClientSettings();
        var reporterCreated = false;
        var opAmp = CreateOpAmp(
            effectiveConfigReporterFactory: () =>
            {
                reporterCreated = true;
                return CreateReporter();
            },
            sdkSetupEnabledResolver: () => false);

        opAmp.ConfigureEffectiveConfigReporting(settings);

        Assert.False(settings.EffectiveConfigurationReporting.EnableReporting);
        Assert.False(reporterCreated);
    }

    [Fact]
    public void ConfigureEffectiveConfigReporting_DoesNotEnableReporting_WhenProviderGraphPreflightFails()
    {
        var settings = new OpAmpClientSettings();
        var reporter = new EffectiveConfigReporter(
            CreateStaticSettings(),
            openTelemetrySdkDisabled: false,
            () => throw new MissingMemberException("provider graph"));
        var opAmp = CreateOpAmp(() => reporter);

        opAmp.ConfigureEffectiveConfigReporting(settings);

        Assert.False(settings.EffectiveConfigurationReporting.EnableReporting);
    }

    [Fact]
    public void ConfigureEffectiveConfigReporting_EnablesReporting_WhenSdkIsDisabled()
    {
        var settings = new OpAmpClientSettings();
        var reporter = new EffectiveConfigReporter(
            CreateStaticSettings(),
            openTelemetrySdkDisabled: true,
            () => throw new MissingMemberException("provider graph"));
        var opAmp = CreateOpAmp(() => reporter);

        opAmp.ConfigureEffectiveConfigReporting(settings);

        Assert.True(settings.EffectiveConfigurationReporting.EnableReporting);
    }

    private static OpAmp CreateOpAmp(
        Func<EffectiveConfigReporter>? effectiveConfigReporterFactory = null,
        Func<bool>? opAmpEnabledResolver = null,
        Func<bool>? sdkSetupEnabledResolver = null,
        Func<EffectiveProfilerFeatures>? profilerStateResolver = null)
    {
        return new OpAmp(
            effectiveConfigReporterFactory ?? CreateReporter,
            opAmpEnabledResolver ?? (() => true),
            sdkSetupEnabledResolver ?? (() => true),
            profilerStateResolver ?? (() => EffectiveProfilerFeatures.None));
    }

    private static EffectiveConfigReporter CreateReporter()
    {
        return new EffectiveConfigReporter(
            CreateStaticSettings(),
            openTelemetrySdkDisabled: false,
            () => null);
    }

    private static EffectiveConfigStaticSettings CreateStaticSettings()
    {
        return new EffectiveConfigStaticSettings(
            new PluginSettings(new NameValueConfigurationSource(new NameValueCollection())));
    }
}
