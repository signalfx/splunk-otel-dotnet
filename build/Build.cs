using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using System;
using System.IO;
using System.IO.Compression;

using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    [Solution("Splunk.OpenTelemetry.AutoInstrumentation.sln")] readonly Solution Solution;
    public static int Main() => Execute<Build>(x => x.Workflow);

    [Parameter("Configuration to build - Default is 'Release'")]
    readonly Configuration Configuration = Configuration.Release;

    [Parameter("Platform to build - x86 or x64. Default is 'x64'")]
    readonly MSBuildTargetPlatform Platform = MSBuildTargetPlatform.x64;

    const string OpenTelemetryAutoInstrumentationDefaultVersion = "v0.5.1-beta.1";
    [Parameter($"OpenTelemetry AutoInstrumentation dependency version - Default is '{OpenTelemetryAutoInstrumentationDefaultVersion}'")]
    readonly string OpenTelemetryAutoInstrumentationVersion = OpenTelemetryAutoInstrumentationDefaultVersion;

    readonly AbsolutePath OpenTelemetryDistributionFolder = RootDirectory / "OpenTelemetryDistribution";

    Target Clean => _ => _
        .Executes(() =>
        {
            DotNetClean();
            FileSystemTasks.DeleteDirectory(OpenTelemetryDistributionFolder);
            FileSystemTasks.DeleteDirectory(RootDirectory / GetOTelAutoInstrumentationFileName());
        });

    Target Restore => _ => _
        .After(Clean)
        .Executes(() =>
        {
            DotNetRestore();
        });

    Target DownloadAutoInstrumentationDistribution => _ => _
        .Executes(async () =>
        {
            var fileName = GetOTelAutoInstrumentationFileName();

            var uri =
                $"https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/{OpenTelemetryAutoInstrumentationVersion}/{fileName}";

            await HttpTasks.HttpDownloadFileAsync(uri, RootDirectory / fileName, clientConfigurator: httpClient =>
            {
                httpClient.Timeout = TimeSpan.FromMinutes(3);
                return httpClient;
            });
        });

    Target UnpackAutoInstrumentationDistribution => _ => _
        .After(DownloadAutoInstrumentationDistribution)
        .After(Clean)
        .Executes(() =>
        {
            var fileName = GetOTelAutoInstrumentationFileName();
            FileSystemTasks.DeleteDirectory(OpenTelemetryDistributionFolder);
            CompressionTasks.UncompressZip(fileName, OpenTelemetryDistributionFolder);
            FileSystemTasks.DeleteFile(RootDirectory / fileName);
        });

    static string GetOTelAutoInstrumentationFileName()
    {
        string fileName;
        switch (EnvironmentInfo.Platform)
        {
            case PlatformFamily.Windows:
                fileName = "opentelemetry-dotnet-instrumentation-windows.zip";
                break;
            case PlatformFamily.Linux:
                fileName = Environment.GetEnvironmentVariable("IsAlpine") == "true"
                    ? "opentelemetry-dotnet-instrumentation-linux-musl.zip"
                    : "opentelemetry-dotnet-instrumentation-linux-glibc.zip";
                break;
            case PlatformFamily.OSX:
                fileName = "opentelemetry-dotnet-instrumentation-macos.zip";
                break;
            case PlatformFamily.Unknown:
                throw new NotSupportedException();
            default:
                throw new ArgumentOutOfRangeException();
        }

        return fileName;
    }

    Target AddSplunkPlugins => _ => _
        .After(Compile)
        .Executes(() =>
        {
            FileSystemTasks.CopyFileToDirectory(
                RootDirectory / "src" / "Splunk.OpenTelemetry.AutoInstrumentation" / "bin" / Configuration /
                "netstandard2.0" / "Splunk.OpenTelemetry.AutoInstrumentation.dll",
                OpenTelemetryDistributionFolder / "net");

            if (EnvironmentInfo.IsWin)
            {
                FileSystemTasks.CopyFileToDirectory(
                    RootDirectory / "src" / "Splunk.OpenTelemetry.AutoInstrumentation" / "bin" / Configuration /
                    "netstandard2.0" / "Splunk.OpenTelemetry.AutoInstrumentation.dll",
                    OpenTelemetryDistributionFolder / "netfx");
            }
        });

    Target CopyInstrumentScripts => _ => _
        .After(AddSplunkPlugins)
        .Executes(() =>
        {
            var source = RootDirectory / "instrument.sh";
            var dest = OpenTelemetryDistributionFolder;
            FileSystemTasks.CopyFileToDirectory(source, dest, FileExistsPolicy.Overwrite);
        });

    Target PackSplunkDistribution => _ => _
        .After(CopyInstrumentScripts)
        .Executes(() =>
        {
            var fileName = GetOTelAutoInstrumentationFileName();
            CompressionTasks.CompressZip(OpenTelemetryDistributionFolder, RootDirectory / "bin" / ("splunk-" + fileName), compressionLevel: CompressionLevel.SmallestSize, fileMode: FileMode.Create);
        });

    Target Compile => _ => _
        .After(Restore)
        .After(UnpackAutoInstrumentationDistribution)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetNoRestore(true)
                .SetConfiguration(Configuration)
                .SetPlatform(Platform));
        });

    Target RunUnitTests => _ => _
        .After(Compile)
        .Executes(() =>
        {
            var project = Solution.GetProject("Splunk.OpenTelemetry.AutoInstrumentation.Tests");

            DotNetTest(s => s
                .SetNoBuild(true)
                .SetProjectFile(project)
                .SetConfiguration(Configuration));
        });

    Target RunIntegrationTests => _ => _
        .After(Compile)
        .After(AddSplunkPlugins)
        .Executes(() =>
        {
            var project = Solution.GetProject("Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests");

            DotNetTest(s => s
                .SetNoBuild(true)
                .SetProjectFile(project)
                .SetConfiguration(Configuration));
        });

    Target Workflow => _ => _
        .DependsOn(Clean)
        .DependsOn(Restore)
        .DependsOn(DownloadAutoInstrumentationDistribution)
        .DependsOn(UnpackAutoInstrumentationDistribution)
        .DependsOn(Compile)
        .DependsOn(AddSplunkPlugins)
        .DependsOn(CopyInstrumentScripts)
        .DependsOn(RunUnitTests)
        .DependsOn(RunIntegrationTests)
        .DependsOn(PackSplunkDistribution);
}
