// <copyright file="EnvironmentHelper.cs" company="Splunk Inc.">
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

// <copyright file="EnvironmentHelper.cs" company="OpenTelemetry Authors">
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

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;

public class EnvironmentHelper
{
    private static readonly string RuntimeFrameworkDescription = RuntimeInformation.FrameworkDescription.ToLower();
    private static string? _nukeOutputLocation;

    private readonly ITestOutputHelper _output;
    private readonly int? _major;
    private readonly int? _minor;
    private readonly string? _patch;

    private readonly string _appNamePrepend;
    private readonly bool? _isCoreClr;
    private readonly string _testApplicationDirectory;

    private string? _integrationsFileLocation;
    private string? _profilerFileLocation;

    public EnvironmentHelper(
        string testApplicationName,
        Type anchorType,
        ITestOutputHelper output,
        string? testApplicationDirectory = null,
        bool prependTestApplicationToAppName = true)
    {
        TestApplicationName = testApplicationName;
        _testApplicationDirectory = testApplicationDirectory ?? Path.Combine("test", "test-applications", "integrations");
        var targetFramework = Assembly.GetAssembly(anchorType)?.GetCustomAttribute<TargetFrameworkAttribute>();
        _output = output;

        var parts = targetFramework?.FrameworkName.Split(',');
        var runtime = parts?[0];
        _isCoreClr = runtime?.Equals(EnvironmentTools.CoreFramework);

        var versionParts = parts?[1].Replace("Version=v", string.Empty).Split('.');
        _major = int.Parse(versionParts == null ? string.Empty : versionParts[0]);
        _minor = int.Parse(versionParts == null ? string.Empty : versionParts[1]);

        if (versionParts?.Length == 3)
        {
            _patch = versionParts[2];
        }

        _appNamePrepend = prependTestApplicationToAppName
            ? "TestApplication."
            : string.Empty;
    }

    public bool DebugModeEnabled { get; set; } = true;

    public Dictionary<string, string> CustomEnvironmentVariables { get; set; } = new();

    public string TestApplicationName { get; }

    public string FullTestApplicationName => $"{_appNamePrepend}{TestApplicationName}";

    public static bool IsCoreClr()
    {
        return RuntimeFrameworkDescription.Contains("core") || Environment.Version.Major >= 5;
    }

    public static string GetNukeBuildOutput()
    {
        var solutionDirectory = EnvironmentTools.GetSolutionDirectory();

        var nukeOutputPath = solutionDirectory != null
            ? Path.Combine(solutionDirectory, @"OpenTelemetryDistribution") : null;

        if (nukeOutputPath != null && Directory.Exists(nukeOutputPath))
        {
            _nukeOutputLocation = nukeOutputPath;

            return _nukeOutputLocation;
        }

        throw new Exception($"Unable to find Nuke output at: {nukeOutputPath}. Ensure Nuke has run first.");
    }

    public void SetEnvironmentVariables(
        TestSettings testSettings,
        StringDictionary environmentVariables,
        string processToProfile)
    {
        string profilerPath = GetProfilerPath();

        if (IsCoreClr())
        {
            // enableStartupHook should be true by default, and the parameter should only be set
            // to false when testing the case that instrumentation should not be available.
            if (testSettings.EnableStartupHook)
            {
                environmentVariables["DOTNET_STARTUP_HOOKS"] = GetStartupHookOutputPath();
                environmentVariables["DOTNET_SHARED_STORE"] = GetSharedStorePath();
                environmentVariables["DOTNET_ADDITIONAL_DEPS"] = GetAdditionalDepsPath();
            }

            if (testSettings.EnableClrProfiler)
            {
                environmentVariables["CORECLR_ENABLE_PROFILING"] = "1";
                environmentVariables["CORECLR_PROFILER"] = EnvironmentTools.ProfilerClsId;
                environmentVariables["CORECLR_PROFILER_PATH"] = profilerPath;
            }
        }
        else
        {
            if (testSettings.EnableClrProfiler)
            {
                environmentVariables["COR_ENABLE_PROFILING"] = "1";
                environmentVariables["COR_PROFILER"] = EnvironmentTools.ProfilerClsId;
                environmentVariables["COR_PROFILER_PATH"] = profilerPath;
            }
        }

        if (DebugModeEnabled)
        {
            environmentVariables["OTEL_DOTNET_AUTO_DEBUG"] = "1";
            var solutionDirectory = EnvironmentTools.GetSolutionDirectory() ?? string.Empty;
            environmentVariables["OTEL_DOTNET_AUTO_LOG_DIRECTORY"] = Path.Combine(solutionDirectory, "build_data", "profiler-logs");
        }

        if (!string.IsNullOrEmpty(processToProfile))
        {
            environmentVariables["OTEL_DOTNET_AUTO_INCLUDE_PROCESSES"] = Path.GetFileName(processToProfile);
        }

        environmentVariables["OTEL_DOTNET_AUTO_HOME"] = GetNukeBuildOutput();

        environmentVariables["OTEL_DOTNET_AUTO_INTEGRATIONS_FILE"] = Environment.GetEnvironmentVariable("OTEL_DOTNET_AUTO_INTEGRATIONS_FILE") ?? GetIntegrationsPath();

        if (testSettings.TracesSettings != null)
        {
            environmentVariables["OTEL_TRACES_EXPORTER"] = testSettings.TracesSettings.Exporter;
            environmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = $"http://localhost:{testSettings.TracesSettings.Port}";
        }

        if (testSettings.MetricsSettings != null)
        {
            environmentVariables["OTEL_METRICS_EXPORTER"] = testSettings.MetricsSettings.Exporter;
            environmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = $"http://localhost:{testSettings.MetricsSettings.Port}";
        }

        if (testSettings.LogSettings != null)
        {
            environmentVariables["OTEL_LOGS_EXPORTER"] = testSettings.LogSettings.Exporter;
            environmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = $"http://localhost:{testSettings.LogSettings.Port}";
        }

        // for ASP.NET Core test applications, set the server's port
        environmentVariables["ASPNETCORE_URLS"] = $"http://127.0.0.1:{testSettings.AspNetCorePort}/";

        environmentVariables["OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES"] = "TestApplication.*";

        foreach (var key in CustomEnvironmentVariables.Keys)
        {
            environmentVariables[key] = CustomEnvironmentVariables[key];
        }
    }

    public string GetProfilerPath()
    {
        if (_profilerFileLocation != null)
        {
            return _profilerFileLocation;
        }

        string extension = EnvironmentTools.GetOs() switch
        {
            "win" => "dll",
            "linux" => "so",
            "osx" => "dylib",
            _ => throw new PlatformNotSupportedException()
        };

        var fileName = $"OpenTelemetry.AutoInstrumentation.Native.{extension}";
        var nukeOutput = GetNukeBuildOutput();
        var profilerPath = EnvironmentTools.IsWindows()
            ? Path.Combine(nukeOutput, $"win-{EnvironmentTools.GetPlatform().ToLower()}", fileName)
            : Path.Combine(nukeOutput, fileName);

        if (File.Exists(profilerPath))
        {
            _profilerFileLocation = profilerPath;
            _output.WriteLine($"Found profiler at {_profilerFileLocation}.");
            return _profilerFileLocation;
        }

        throw new Exception($"Unable to find profiler at: {profilerPath}");
    }

    public string GetIntegrationsPath()
    {
        if (_integrationsFileLocation != null)
        {
            return _integrationsFileLocation;
        }

        var fileName = "integrations.json";
        var integrationsPath = Path.Combine(GetNukeBuildOutput(), fileName);

        if (File.Exists(integrationsPath))
        {
            _integrationsFileLocation = integrationsPath;
            _output.WriteLine($"Found integrations at {_profilerFileLocation}.");
            return _integrationsFileLocation;
        }

        throw new Exception($"Unable to find integrations at: {integrationsPath}");
    }

    public string GetTestApplicationPath(string packageVersion = "", string framework = "")
    {
        var extension = "exe";

        if (IsCoreClr() || _testApplicationDirectory.Contains("aspnet"))
        {
            extension = "dll";
        }

        var appFileName = $"{FullTestApplicationName}.{extension}";
        var testApplicationPath = Path.Combine(GetTestApplicationApplicationOutputDirectory(packageVersion: packageVersion, framework: framework), appFileName);
        return testApplicationPath;
    }

    public string GetTestApplicationExecutionSource()
    {
        string executor;

        if (_testApplicationDirectory.Contains("aspnet"))
        {
            executor = $"C:\\Program Files{(Environment.Is64BitProcess ? string.Empty : " (x86)")}\\IIS Express\\iisexpress.exe";
        }
        else if (IsCoreClr())
        {
            executor = EnvironmentTools.IsWindows() ? "dotnet.exe" : "dotnet";
        }
        else
        {
            var appFileName = $"{FullTestApplicationName}.exe";
            executor = Path.Combine(GetTestApplicationApplicationOutputDirectory(), appFileName);

            if (!File.Exists(executor))
            {
                throw new Exception($"Unable to find executing assembly at {executor}");
            }
        }

        return executor;
    }

    public string GetTestApplicationProjectDirectory()
    {
        var solutionDirectory = EnvironmentTools.GetSolutionDirectory();
        var projectDir = Path.Combine(
            solutionDirectory ?? string.Empty,
            _testApplicationDirectory,
            $"{FullTestApplicationName}");
        return projectDir;
    }

    public string GetTestApplicationApplicationOutputDirectory(string packageVersion = "", string framework = "")
    {
        var targetFramework = string.IsNullOrEmpty(framework) ? GetTargetFramework() : framework;
        var binDir = Path.Combine(
            GetTestApplicationProjectDirectory(),
            "bin");

        if (_testApplicationDirectory.Contains("aspnet"))
        {
            return Path.Combine(
                binDir,
                EnvironmentTools.GetBuildConfiguration(),
                "app.publish");
        }

        return Path.Combine(
            binDir,
            packageVersion,
            EnvironmentTools.GetBuildConfiguration(),
            targetFramework);
    }

    public string GetTargetFramework()
    {
        if (_isCoreClr.HasValue && _isCoreClr.Value)
        {
            if (_major >= 5)
            {
                return $"net{_major}.{_minor}";
            }

            return $"netcoreapp{_major}.{_minor}";
        }

        return $"net{_major}{_minor}{_patch ?? string.Empty}";
    }

    private static string GetStartupHookOutputPath()
    {
        var startupHookOutputPath = Path.Combine(
            GetNukeBuildOutput(),
            "netcoreapp3.1",
            "OpenTelemetry.AutoInstrumentation.StartupHook.dll");

        return startupHookOutputPath;
    }

    private static string GetSharedStorePath()
    {
        var storePath = Path.Combine(
            GetNukeBuildOutput(),
            "store");

        return storePath;
    }

    private static string GetAdditionalDepsPath()
    {
        var additionalDeps = Path.Combine(
            GetNukeBuildOutput(),
            "AdditionalDeps");

        return additionalDeps;
    }
}
