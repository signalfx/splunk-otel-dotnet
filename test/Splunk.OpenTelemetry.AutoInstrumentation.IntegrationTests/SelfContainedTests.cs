// <copyright file="SelfContainedTests.cs" company="Splunk Inc.">
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

// <copyright file="SelfContainedTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
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

using FluentAssertions;
using Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests;

[Trait("Category", "NuGetPackage")]
public sealed class SelfContainedTests : TestHelper
{
    private readonly string _selfContainedAppDir;

    public SelfContainedTests(ITestOutputHelper output)
        : base("SelfContained", output, "nuget-package")
    {
        var nonSelfContainedOutputDir = EnvironmentHelper
            .GetTestApplicationApplicationOutputDirectory();

        // The self-contained app is going to have an extra folder before it: the one
        // with a RID like "win-x64", "linux-x64", etc.
        var childrenDirs = Directory.GetDirectories(nonSelfContainedOutputDir);
        childrenDirs.Should().ContainSingle();

        _selfContainedAppDir = childrenDirs[0];
    }

    [Fact]
    public void InstrumentExecutable()
    {
        RunAndAssertHttpSpans(() =>
        {
            var instrumentationTarget = Path.Combine(_selfContainedAppDir, EnvironmentHelper.FullTestApplicationName);
            instrumentationTarget = EnvironmentTools.IsWindows() ? instrumentationTarget + ".exe" : instrumentationTarget;
            RunInstrumentationTarget(instrumentationTarget);
        });
    }

#if NET
    [Fact]
    public void InstrumentDll()
    {
        RunAndAssertHttpSpans(() =>
        {
            var dllName = EnvironmentHelper.FullTestApplicationName.EndsWith(".exe")
                ? EnvironmentHelper.FullTestApplicationName.Replace(".exe", ".dll")
                : EnvironmentHelper.FullTestApplicationName + ".dll";
            var dllPath = Path.Combine(_selfContainedAppDir, dllName);
            RunInstrumentationTarget($"dotnet {dllPath}");
        });
    }
#endif

    private void RunInstrumentationTarget(string instrumentationTarget)
    {
        var instrumentationScriptExtension = EnvironmentTools.IsWindows() ? ".cmd" : ".sh";
        var instrumentationScriptPath =
            Path.Combine(_selfContainedAppDir, "splunk-launch") + instrumentationScriptExtension;

        Output.WriteLine($"Running: {instrumentationScriptPath} {instrumentationTarget}");

        using var process = InstrumentedProcessHelper.Start(instrumentationScriptPath, instrumentationTarget, EnvironmentHelper);
        using var helper = new ProcessHelper(process);

        process.Should().NotBeNull();

        bool processTimeout = !process!.WaitForExit((int)Helpers.Timeout.ProcessExit.TotalMilliseconds);
        if (processTimeout)
        {
            process.Kill();
        }

        Output.WriteLine("ProcessId: " + process.Id);
        Output.WriteLine("Exit Code: " + process.ExitCode);
        Output.WriteResult(helper);

        processTimeout.Should().BeFalse("test application should NOT have timed out");
        process.ExitCode.Should().Be(0, "test application should NOT have non-zero exit code");
    }

    private void RunAndAssertHttpSpans(Action appLauncherAction)
    {
        var collector = new MockSpansCollector(Output);
        SetExporter(collector);

#if NETFRAMEWORK
        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpWebRequest");
#else
        collector.Expect("System.Net.Http");
#endif

        collector.ResourceExpector.ExpectDistributionResources(serviceName: EnvironmentHelper.FullTestApplicationName);

        appLauncherAction();

        collector.AssertExpectations();
    }
}
