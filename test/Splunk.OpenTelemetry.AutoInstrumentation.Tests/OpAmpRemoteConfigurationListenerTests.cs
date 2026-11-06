// <copyright file="OpAmpRemoteConfigurationListenerTests.cs" company="Splunk Inc.">
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

using System.Diagnostics;
using System.Text;
using OpenTelemetry.OpAmp.Client.Messages;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration.FileBasedConfiguration;
using Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class OpAmpRemoteConfigurationListenerTests
{
    private const string CpuProfilerRemoteConfiguration = """
        distribution:
          splunk:
            profiling:
              always_on:
                cpu_profiler:
        """;

    [Fact]
    public void HandleRemoteConfiguration_ReportsApplyingAndAppliedWhenConfigIsApplied()
    {
        var configHash = new byte[] { 1, 2, 3 };
        var reports = new List<RemoteConfigStatusReport>();
        var appliedCount = 0;
        var configuration = new YamlRoot();
        YamlRoot? appliedConfiguration = null;
        string? parsedYaml = null;
        var listener = CreateListener(
            reports,
            () => appliedCount++,
            parsedConfiguration => appliedConfiguration = parsedConfiguration,
            parseYaml: yaml =>
            {
                parsedYaml = yaml;
                return configuration;
            });

        listener.HandleRemoteConfiguration(
            configHash,
            RemoteConfiguration.RemoteConfigContentType,
            Encoding.UTF8.GetBytes(CpuProfilerRemoteConfiguration));

        Assert.Equal(1, appliedCount);
        Assert.Equal(CpuProfilerRemoteConfiguration, parsedYaml);
        Assert.Same(configuration, appliedConfiguration);
        Assert.Collection(
            reports,
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applied));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HandleRemoteConfiguration_IgnoresConfigurationWhenHashIsEmpty(bool enableCpuProfiler)
    {
        var reports = new List<RemoteConfigStatusReport>();
        var appliedCount = 0;
        YamlRoot? appliedConfiguration = null;
        var listener = CreateListener(reports, () => appliedCount++, configuration => appliedConfiguration = configuration);
        var yaml = enableCpuProfiler
            ? CpuProfilerRemoteConfiguration
            : "distribution:\n  splunk:\n    profiling: {}\n";

        listener.HandleRemoteConfiguration(
            Array.Empty<byte>(),
            RemoteConfiguration.RemoteConfigContentType,
            Encoding.UTF8.GetBytes(yaml));

        Assert.Null(appliedConfiguration);
        Assert.Equal(0, appliedCount);
        Assert.Empty(reports);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("")]
    [InlineData(null)]
    public void HandleRemoteConfiguration_DoesNotRetryWhenContentTypeIsUnsupported(string? contentType)
    {
        var configHash = new byte[] { 4, 5, 6 };
        var reports = new List<RemoteConfigStatusReport>();
        var appliedCount = 0;
        var parseAttempts = 0;
        var applicationAttempts = 0;
        var timestamp = 0L;
        var listener = CreateListener(
            reports,
            () => appliedCount++,
            _ => applicationAttempts++,
            () => timestamp,
            _ =>
            {
                parseAttempts++;
                return new YamlRoot();
            });

        foreach (var nextTimestamp in new[] { 0L, 30 * Stopwatch.Frequency, 60 * Stopwatch.Frequency, 3600 * Stopwatch.Frequency })
        {
            timestamp = nextTimestamp;
            listener.HandleRemoteConfiguration(configHash.ToArray(), contentType, Encoding.UTF8.GetBytes("{}"));
        }

        Assert.Equal(0, appliedCount);
        Assert.Equal(0, parseAttempts);
        Assert.Equal(0, applicationAttempts);
        Assert.Collection(
            reports,
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Failed, $"Unsupported content type '{contentType}'"));
    }

    [Fact]
    public void HandleRemoteConfiguration_DoesNotRetryWhenUtf8IsInvalid()
    {
        var configHash = new byte[] { 1, 2, 3 };
        var reports = new List<RemoteConfigStatusReport>();
        var appliedCount = 0;
        var parseAttempts = 0;
        var applicationAttempts = 0;
        var timestamp = 0L;
        var listener = CreateListener(
            reports,
            () => appliedCount++,
            _ => applicationAttempts++,
            () => timestamp,
            _ =>
            {
                parseAttempts++;
                return new YamlRoot();
            });

        foreach (var nextTimestamp in new[] { 0L, 30 * Stopwatch.Frequency, 60 * Stopwatch.Frequency, 3600 * Stopwatch.Frequency })
        {
            timestamp = nextTimestamp;
            listener.HandleRemoteConfiguration(configHash.ToArray(), RemoteConfiguration.RemoteConfigContentType, [0xFF]);
        }

        Assert.Equal(0, appliedCount);
        Assert.Equal(0, parseAttempts);
        Assert.Equal(0, applicationAttempts);
        Assert.Collection(
            reports,
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Failed, string.Empty));
        Assert.False(string.IsNullOrEmpty(reports[1].ErrorMessage));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HandleRemoteConfiguration_DoesNotRetryWhenYamlParsingFails(bool previousConfigFailedTransiently)
    {
        var configHash = new byte[] { 4, 5, 6 };
        var body = Encoding.UTF8.GetBytes(CpuProfilerRemoteConfiguration);
        var reports = new List<RemoteConfigStatusReport>();
        var appliedCount = 0;
        var parseAttempts = 0;
        var applicationAttempts = 0;
        var timestamp = 0L;
        var failValidation = false;
        var listener = CreateListener(
            reports,
            () => appliedCount++,
            _ =>
            {
                applicationAttempts++;
                throw new InvalidOperationException("Temporary application failure.");
            },
            () => timestamp,
            _ =>
            {
                parseAttempts++;
                if (failValidation)
                {
                    throw new FormatException("Invalid YAML.");
                }

                return new YamlRoot();
            });

        if (previousConfigFailedTransiently)
        {
            listener.HandleRemoteConfiguration([1, 2, 3], RemoteConfiguration.RemoteConfigContentType, body);
        }

        var previousReportCount = reports.Count;
        failValidation = true;
        foreach (var nextTimestamp in new[] { 0L, 30 * Stopwatch.Frequency, 60 * Stopwatch.Frequency, 3600 * Stopwatch.Frequency })
        {
            timestamp = nextTimestamp;
            listener.HandleRemoteConfiguration(configHash.ToArray(), RemoteConfiguration.RemoteConfigContentType, body);
        }

        Assert.Equal(previousConfigFailedTransiently ? 2 : 1, parseAttempts);
        Assert.Equal(previousConfigFailedTransiently ? 1 : 0, applicationAttempts);
        Assert.Equal(0, appliedCount);
        Assert.Collection(
            reports.Skip(previousReportCount),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Failed, "Invalid YAML."));

        var nextConfigHash = new byte[] { 7, 8, 9 };
        failValidation = false;
        listener.HandleRemoteConfiguration(nextConfigHash, RemoteConfiguration.RemoteConfigContentType, body);
        timestamp += 30 * Stopwatch.Frequency;
        listener.HandleRemoteConfiguration(nextConfigHash, RemoteConfiguration.RemoteConfigContentType, body);

        Assert.Equal(previousConfigFailedTransiently ? 4 : 3, parseAttempts);
        Assert.Equal(previousConfigFailedTransiently ? 3 : 2, applicationAttempts);
        Assert.Collection(
            reports.Skip(previousReportCount + 2),
            report => AssertReport(report, nextConfigHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, nextConfigHash, RemoteConfigStatusCode.Failed, "Temporary application failure."),
            report => AssertReport(report, nextConfigHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, nextConfigHash, RemoteConfigStatusCode.Failed, "Temporary application failure."));
    }

    [Fact]
    public void HandleRemoteConfiguration_ReportsAppliedWhenSplunkConfigFileIsMissing()
    {
        var configHash = new byte[] { 7, 8, 9 };
        var reports = new List<RemoteConfigStatusReport>();
        var appliedCount = 0;
        var listener = CreateListener(reports, () => appliedCount++);

        listener.HandleRemoteConfiguration(configHash, contentType: null, body: null);

        Assert.Equal(0, appliedCount);
        Assert.Collection(
            reports,
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applied));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HandleRemoteConfiguration_DoesNotReportStatusAgainForAppliedConfigHash(bool hasConfigFile)
    {
        var configHash = new byte[] { 10, 11, 12 };
        var reports = new List<RemoteConfigStatusReport>();
        var applicationAttempts = 0;
        var appliedCount = 0;
        var timestamp = 0L;
        var body = hasConfigFile ? Encoding.UTF8.GetBytes(CpuProfilerRemoteConfiguration) : null;
        var listener = CreateListener(
            reports,
            () => appliedCount++,
            _ => applicationAttempts++,
            () => timestamp);

        listener.HandleRemoteConfiguration(configHash, RemoteConfiguration.RemoteConfigContentType, body);

        foreach (var nextTimestamp in new[] { 0L, (30 * Stopwatch.Frequency) - 1, 30 * Stopwatch.Frequency, 60 * Stopwatch.Frequency, 3600 * Stopwatch.Frequency })
        {
            timestamp = nextTimestamp;
            listener.HandleRemoteConfiguration(configHash.ToArray(), RemoteConfiguration.RemoteConfigContentType, body);
        }

        Assert.Equal(hasConfigFile ? 1 : 0, applicationAttempts);
        Assert.Equal(hasConfigFile ? 1 : 0, appliedCount);
        Assert.Collection(
            reports,
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applied));
    }

    [Fact]
    public void HandleRemoteConfiguration_RetriesFailedConfigurationAtThirtySecondBoundary()
    {
        var configHash = new byte[] { 1, 2, 3 };
        var body = Encoding.UTF8.GetBytes(CpuProfilerRemoteConfiguration);
        var reports = new List<RemoteConfigStatusReport>();
        var applicationAttempts = 0;
        var appliedCount = 0;
        var timestamp = 0L;
        var listener = CreateListener(
            reports,
            () => appliedCount++,
            _ =>
            {
                if (++applicationAttempts == 1)
                {
                    throw new InvalidOperationException("Temporary application failure.");
                }
            },
            () => timestamp);

        listener.HandleRemoteConfiguration(configHash, RemoteConfiguration.RemoteConfigContentType, body);
        listener.HandleRemoteConfiguration(configHash, RemoteConfiguration.RemoteConfigContentType, body);
        timestamp = (30 * Stopwatch.Frequency) - 1;
        listener.HandleRemoteConfiguration(configHash, RemoteConfiguration.RemoteConfigContentType, body);

        Assert.Equal(1, applicationAttempts);
        Assert.Equal(0, appliedCount);
        Assert.Collection(
            reports,
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Failed, "Temporary application failure."));

        timestamp++;
        listener.HandleRemoteConfiguration(configHash, RemoteConfiguration.RemoteConfigContentType, body);

        Assert.Equal(2, applicationAttempts);
        Assert.Equal(1, appliedCount);
        Assert.Collection(
            reports,
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Failed, "Temporary application failure."),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applied));
    }

    [Fact]
    public void HandleRemoteConfiguration_StopsAfterThreeFailedApplicationsWithoutReplayingStatus()
    {
        var configHash = new byte[] { 4, 5, 6 };
        var body = Encoding.UTF8.GetBytes(CpuProfilerRemoteConfiguration);
        var reports = new List<RemoteConfigStatusReport>();
        var applicationAttempts = 0;
        var appliedCount = 0;
        var timestamp = 0L;
        var listener = CreateListener(
            reports,
            () => appliedCount++,
            _ =>
            {
                applicationAttempts++;
                throw new InvalidOperationException("Application failure.");
            },
            () => timestamp);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            listener.HandleRemoteConfiguration(configHash, RemoteConfiguration.RemoteConfigContentType, body);
            Assert.Equal(attempt, applicationAttempts);
            timestamp += 30 * Stopwatch.Frequency;
        }

        foreach (var nextTimestamp in new[] { timestamp, timestamp + (30 * Stopwatch.Frequency), 3600 * Stopwatch.Frequency })
        {
            timestamp = nextTimestamp;
            listener.HandleRemoteConfiguration(configHash, RemoteConfiguration.RemoteConfigContentType, body);
        }

        Assert.Equal(3, applicationAttempts);
        Assert.Equal(0, appliedCount);
        Assert.Collection(
            reports,
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Failed, "Application failure."),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Failed, "Application failure."),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Failed, "Application failure."));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HandleRemoteConfiguration_ResetsRetryStateWhenConfigHashChanges(bool previousConfigApplied)
    {
        var previousConfigHash = new byte[] { 7, 8, 9 };
        var configHash = new byte[] { 10, 11, 12 };
        var body = Encoding.UTF8.GetBytes(CpuProfilerRemoteConfiguration);
        var reports = new List<RemoteConfigStatusReport>();
        var applicationAttempts = 0;
        var applySuccessfully = previousConfigApplied;
        var timestamp = 0L;
        var listener = CreateListener(
            reports,
            applyConfiguration: _ =>
            {
                applicationAttempts++;
                if (!applySuccessfully)
                {
                    throw new InvalidOperationException("Application failure.");
                }
            },
            getTimestamp: () => timestamp);

        var previousApplicationAttempts = previousConfigApplied ? 1 : 3;
        for (var attempt = 0; attempt < previousApplicationAttempts; attempt++)
        {
            timestamp = attempt * 30 * Stopwatch.Frequency;
            listener.HandleRemoteConfiguration(previousConfigHash, RemoteConfiguration.RemoteConfigContentType, body);
        }

        Assert.Equal(previousApplicationAttempts, applicationAttempts);
        var previousReportCount = reports.Count;
        applySuccessfully = false;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            listener.HandleRemoteConfiguration(configHash, RemoteConfiguration.RemoteConfigContentType, body);
            Assert.Equal(previousApplicationAttempts + attempt, applicationAttempts);
            timestamp += 30 * Stopwatch.Frequency;
        }

        listener.HandleRemoteConfiguration(configHash, RemoteConfiguration.RemoteConfigContentType, body);

        Assert.Equal(previousApplicationAttempts + 3, applicationAttempts);
        Assert.Collection(
            reports.Skip(previousReportCount),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Failed, "Application failure."),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Failed, "Application failure."),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Failed, "Application failure."));
    }

    private static OpAmpRemoteConfigurationListener CreateListener(
        List<RemoteConfigStatusReport> reports,
        Action? onApplied = null,
        Action<YamlRoot?>? applyConfiguration = null,
        Func<long>? getTimestamp = null,
        Func<string, YamlRoot?>? parseYaml = null)
    {
        return new OpAmpRemoteConfigurationListener(
            onApplied ?? (() => { }),
            report =>
            {
                reports.Add(report);
                return Task.CompletedTask;
            },
            applyConfiguration ?? (_ => { }),
            getTimestamp,
            parseYaml ?? (_ => new YamlRoot()));
    }

    private static void AssertReport(
        RemoteConfigStatusReport report,
        byte[] expectedConfigHash,
        RemoteConfigStatusCode expectedStatus,
        string? expectedErrorMessage = null)
    {
        Assert.Equal(expectedConfigHash, report.LastRemoteConfigHash.ToArray());
        Assert.Equal(expectedStatus, report.Status);
        if (expectedErrorMessage == null)
        {
            Assert.Null(report.ErrorMessage);
        }
        else
        {
            Assert.Contains(expectedErrorMessage, report.ErrorMessage, StringComparison.Ordinal);
        }
    }
}
