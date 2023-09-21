using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using System.IO.Compression;

using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build : NukeBuild
{
    private readonly static AbsolutePath TestNuGetPackageApps = NukeBuild.RootDirectory / "test" / "test-applications" / "nuget-package";

    [Solution("Splunk.OpenTelemetry.AutoInstrumentation.sln")] readonly Solution Solution;
    public static int Main() => Execute<Build>(x => x.Workflow);

    [Parameter("Configuration to build - Default is 'Release'")]
    readonly Configuration Configuration = Configuration.Release;

    [Parameter("Platform to build - x86 or x64. Default is 'x64'")]
    readonly MSBuildTargetPlatform Platform = MSBuildTargetPlatform.x64;

    const string OpenTelemetryAutoInstrumentationDefaultVersion = "v1.0.1";
    [Parameter($"OpenTelemetry AutoInstrumentation dependency version - Default is '{OpenTelemetryAutoInstrumentationDefaultVersion}'")]
    readonly string OpenTelemetryAutoInstrumentationVersion = OpenTelemetryAutoInstrumentationDefaultVersion;

    readonly AbsolutePath OpenTelemetryDistributionFolder = RootDirectory / "OpenTelemetryDistribution";

    private IEnumerable<Project> AllProjectsExceptNuGetTestApps() => Solution.AllProjects.Where(project => !TestNuGetPackageApps.Contains(project.Directory));

    Target Clean => _ => _
        .Executes(() =>
        {
            DotNetClean();
            NuGetPackageFolder.DeleteDirectory();
            OpenTelemetryDistributionFolder.DeleteDirectory();
            (RootDirectory / GetOTelAutoInstrumentationFileName()).DeleteDirectory();
        });

    Target Restore => _ => _
        .After(Clean)
        .Executes(() =>
        {
            foreach (var project in AllProjectsExceptNuGetTestApps())
            {
                DotNetRestore(s => s
                    .SetProjectFile(project)
                    .SetPlatform(Platform));
            }
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
            OpenTelemetryDistributionFolder.DeleteDirectory();
            (RootDirectory / fileName).UnZipTo(OpenTelemetryDistributionFolder);
            (RootDirectory / fileName).DeleteFile();
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
                RootDirectory / "src" / "Splunk.OpenTelemetry.AutoInstrumentation" / "bin" / ((string)Platform).ToLower() / Configuration /
                "net6.0" / "Splunk.OpenTelemetry.AutoInstrumentation.dll",
                OpenTelemetryDistributionFolder / "net");

            if (EnvironmentInfo.IsWin)
            {
                FileSystemTasks.CopyFileToDirectory(
                    RootDirectory / "src" / "Splunk.OpenTelemetry.AutoInstrumentation" / "bin" / ((string)Platform).ToLower() / Configuration /
                    "net462" / "Splunk.OpenTelemetry.AutoInstrumentation.dll",
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

    Target ExtendLicenseFile => _ => _
        .After(AddSplunkPlugins)
        .Executes(() =>
        {
            var licenseFilePath = OpenTelemetryDistributionFolder / "LICENSE";

            var licenseContent = licenseFilePath.ReadAllText();

            var additionalOTelNetAutoInstrumentationContent = @"
Libraries

- OpenTelemetry.AutoInstrumentation.Native
- OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper
- OpenTelemetry.AutoInstrumentation.Loader,
- OpenTelemetry.AutoInstrumentation.StartupHook,
- OpenTelemetry.AutoInstrumentation,
are under the following copyright:
Copyright The OpenTelemetry Authors under Apache License Version 2.0
(<https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/LICENSE>).
";

            if (!licenseContent.Contains(additionalOTelNetAutoInstrumentationContent))
            {
                licenseFilePath.WriteAllText(licenseContent + additionalOTelNetAutoInstrumentationContent);
            }
        });

    Target PackSplunkDistribution => _ => _
        .After(CopyInstrumentScripts)
        .After(ExtendLicenseFile)
        .Executes(() =>
        {
            var fileName = GetOTelAutoInstrumentationFileName();
            OpenTelemetryDistributionFolder.ZipTo(RootDirectory / "bin" / ("splunk-" + fileName), compressionLevel: CompressionLevel.SmallestSize, fileMode: FileMode.Create);
        });

    Target Compile => _ => _
        .After(Restore)
        .After(UnpackAutoInstrumentationDistribution)
        .Executes(() =>
        {
            foreach (var project in AllProjectsExceptNuGetTestApps())
            {
                DotNetBuild(s => s
                    .SetProjectFile(project)
                    .SetNoRestore(true)
                    .SetConfiguration(Configuration)
                    .SetPlatform(Platform));
            }
        });

    Target RunUnitTests => _ => _
        .After(Compile)
        .Executes(() =>
        {
            var project = Solution.AllProjects.First(project => project.Name == "Splunk.OpenTelemetry.AutoInstrumentation.Tests");

            DotNetTest(s => s
                .SetNoBuild(true)
                .SetProjectFile(project)
                .SetConfiguration(Configuration)
                .SetProperty("Platform", Platform));
        });

    Target RunIntegrationTests => _ => _
        .After(Compile)
        .After(AddSplunkPlugins)
        .Executes(() =>
        {
            var project = Solution.AllProjects.First(project => project.Name == "Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests");

            DotNetTest(s => s
                .SetNoBuild(true)
                .SetProjectFile(project)
                .SetFilter("Category!=NuGetPackage")
                .SetConfiguration(Configuration)
                .SetProperty("Platform", Platform));
        });

    Target Workflow => _ => _
        .DependsOn(Clean)
        .DependsOn(Restore)
        .DependsOn(DownloadAutoInstrumentationDistribution)
        .DependsOn(UnpackAutoInstrumentationDistribution)
        .DependsOn(Compile)
        .DependsOn(AddSplunkPlugins)
        .DependsOn(CopyInstrumentScripts)
        .DependsOn(ExtendLicenseFile)
        .DependsOn(RunUnitTests)
        .DependsOn(RunIntegrationTests)
        .DependsOn(PackSplunkDistribution);
}
