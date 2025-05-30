// <copyright file="TestHelper.cs" company="Splunk Inc.">
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

// <copyright file="TestHelper.cs" company="OpenTelemetry Authors">
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

#nullable disable

using System.Diagnostics;
using System.Reflection;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;

public abstract class TestHelper
{
    protected TestHelper(string testApplicationName, ITestOutputHelper output, string testApplicationType = "integrations")
    {
        Output = output;
        EnvironmentHelper = new EnvironmentHelper(testApplicationName, typeof(TestHelper), output, testApplicationType: testApplicationType);

        output.WriteLine($"Platform: {EnvironmentTools.GetPlatform()}");
        output.WriteLine($"Configuration: {EnvironmentTools.GetBuildConfiguration()}");
        output.WriteLine($"TargetFramework: {EnvironmentHelper.GetTargetFramework()}");
        output.WriteLine($".NET Core: {EnvironmentHelper.IsCoreClr()}");
        if (testApplicationType == "integrations")
        {
            output.WriteLine($"Profiler DLL: {EnvironmentHelper.GetProfilerPath()}");
        }
    }

    protected EnvironmentHelper EnvironmentHelper { get; }

    protected ITestOutputHelper Output { get; }

    /// <summary>
    /// Gets the path for the test assembly, not the shadow copy created by xunit.
    /// </summary>
    /// <returns>Path for the test assembly</returns>
    public string GetTestAssemblyPath()
    {
#if NETFRAMEWORK
        // CodeBase is deprecated outside .NET Framework, instead of suppressing the error
        // build the code as per recommendation for each runtime.
        var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
        var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
        var directory = Path.GetDirectoryName(codeBasePath);
        return Path.GetFullPath(directory);
#else
        return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#endif
    }

    public void SetEnvironmentVariable(string key, string value)
    {
        EnvironmentHelper.CustomEnvironmentVariables[key] = value;
    }

    public void RemoveEnvironmentVariable(string key)
    {
        EnvironmentHelper.CustomEnvironmentVariables.Remove(key);
    }

    public void SetExporter(MockSpansCollector collector)
    {
        SetEnvironmentVariable("OTEL_TRACES_EXPORTER", "otlp");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", $"http://localhost:{collector.Port}");
    }

    public void SetExporter(MockMetricsCollector collector)
    {
        SetEnvironmentVariable("OTEL_METRICS_EXPORTER", "otlp");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", $"http://localhost:{collector.Port}");
    }

    public void SetExporter(MockLogsCollector collector)
    {
        SetEnvironmentVariable("OTEL_LOGS_EXPORTER", "otlp");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", $"http://localhost:{collector.Port}");
    }

#if NET
    public void SetExporter(MockLContinuousProfilerCollector collector)
    {
        SetEnvironmentVariable("SPLUNK_PROFILER_LOGS_ENDPOINT", $"http://localhost:{collector.Port}/v1/logs");
    }
#endif

    public void EnableBytecodeInstrumentation()
    {
        SetEnvironmentVariable("CORECLR_ENABLE_PROFILING", "1");
    }

    public void EnableDefaultExporters()
    {
        RemoveEnvironmentVariable("OTEL_TRACES_EXPORTER");
        RemoveEnvironmentVariable("OTEL_METRICS_EXPORTER");
        RemoveEnvironmentVariable("OTEL_LOGS_EXPORTER");
    }

    /// <summary>
    /// RunTestApplication starts the test application, wait up to DefaultProcessTimeout.
    /// Assertion exceptions are thrown if it timed out or the exit code is non-zero.
    /// </summary>
    /// <param name="testSettings">Test settings</param>
    public void RunTestApplication(TestSettings testSettings = null)
    {
        testSettings ??= new();
        using var process = StartTestApplication(testSettings);
        Output.WriteLine($"ProcessName: " + process.ProcessName);
        using var helper = new ProcessHelper(process);

        bool processTimeout = !process.WaitForExit((int)Timeout.ProcessExit.TotalMilliseconds);
        if (processTimeout)
        {
            process.Kill();
        }

        Output.WriteLine("ProcessId: " + process.Id);
        Output.WriteLine("Exit Code: " + process.ExitCode);
        Output.WriteResult(helper);

        Assert.False(processTimeout, "Test application timed out");
        Assert.Equal(0, process.ExitCode);
    }

    /// <summary>
    /// StartTestApplication starts the test application
    /// and returns the Process instance for further interaction.
    /// </summary>
    /// <param name="testSettings">Test settings</param>
    /// <returns>Test application process</returns>
    public Process StartTestApplication(TestSettings testSettings = null)
    {
        testSettings ??= new();

        // get path to test application that the profiler will attach to
        string testApplicationPath = EnvironmentHelper.GetTestApplicationPath(testSettings.PackageVersion, testSettings.Framework);
        if (!File.Exists(testApplicationPath))
        {
            throw new Exception($"application not found: {testApplicationPath}");
        }

        Output.WriteLine($"Starting Application: {testApplicationPath}");
        var executable = EnvironmentHelper.IsCoreClr() ? EnvironmentHelper.GetTestApplicationExecutionSource() : testApplicationPath;
        var args = EnvironmentHelper.IsCoreClr() ? $"{testApplicationPath} {testSettings.Arguments ?? string.Empty}" : testSettings.Arguments;
        return InstrumentedProcessHelper.Start(executable, args, EnvironmentHelper);
    }
}
