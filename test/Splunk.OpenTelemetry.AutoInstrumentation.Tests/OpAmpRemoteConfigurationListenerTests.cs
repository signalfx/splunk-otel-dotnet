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

using System.Text;
using OpenTelemetry.OpAmp.Client.Messages;
using Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class OpAmpRemoteConfigurationListenerTests
{
    private const string CpuProfilerRemoteConfiguration = """
        file_format: "1.0-rc.1"

        distribution:
          splunk:
            profiling:
              always_on:
                cpu_profiler:
                  sampling_interval: 1234
        """;

    [Fact]
    public void HandleRemoteConfiguration_ReportsApplyingAndAppliedWhenConfigIsApplied()
    {
        var configHash = new byte[] { 1, 2, 3 };
        var reports = new List<RemoteConfigStatusReport>();
        var appliedCount = 0;
        string? appliedYaml = null;
        var listener = CreateListener(reports, () => appliedCount++, yaml => appliedYaml = yaml);

        listener.HandleRemoteConfiguration(
            configHash,
            RemoteConfiguration.RemoteConfigContentType,
            Encoding.UTF8.GetBytes(CpuProfilerRemoteConfiguration));

        Assert.Equal(1, appliedCount);
        Assert.NotNull(appliedYaml);
        Assert.Contains("sampling_interval: 1234", appliedYaml, StringComparison.Ordinal);
        Assert.Collection(
            reports,
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applied));
    }

    [Fact]
    public void HandleRemoteConfiguration_ReportsFailedWhenContentTypeIsUnsupported()
    {
        var configHash = new byte[] { 4, 5, 6 };
        var reports = new List<RemoteConfigStatusReport>();
        var appliedCount = 0;
        var listener = CreateListener(reports, () => appliedCount++);

        listener.HandleRemoteConfiguration(
            configHash,
            "application/json",
            Encoding.UTF8.GetBytes("{}"));

        Assert.Equal(0, appliedCount);
        Assert.Collection(
            reports,
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Failed, "Unsupported content type 'application/json'"));
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

    [Fact]
    public void HandleRemoteConfiguration_DoesNotReportStatusAgainForSameConfigHash()
    {
        var configHash = new byte[] { 10, 11, 12 };
        var reports = new List<RemoteConfigStatusReport>();
        var listener = CreateListener(reports);

        listener.HandleRemoteConfiguration(configHash, contentType: null, body: null);
        listener.HandleRemoteConfiguration(configHash, contentType: null, body: null);

        Assert.Collection(
            reports,
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applying),
            report => AssertReport(report, configHash, RemoteConfigStatusCode.Applied));
    }

    private static OpAmpRemoteConfigurationListener CreateListener(
        List<RemoteConfigStatusReport> reports,
        Action? onApplied = null,
        Action<string>? applyYaml = null)
    {
        return new OpAmpRemoteConfigurationListener(
            onApplied ?? (() => { }),
            report =>
            {
                reports.Add(report);
                return Task.CompletedTask;
            },
            applyYaml ?? (_ => { }));
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
