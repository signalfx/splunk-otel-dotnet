using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using System.IO.Compression;
using System.Runtime.InteropServices;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build : NukeBuild
{
    private readonly static AbsolutePath TestNuGetPackageApps = NukeBuild.RootDirectory / "test" / "test-applications" / "nuget-package";

    [Solution("Splunk.OpenTelemetry.AutoInstrumentation.slnx")] readonly Solution Solution;
    public static int Main() => Execute<Build>(x => x.Workflow);

    [Parameter("Configuration to build - Default is 'Release'")]
    readonly Configuration Configuration = Configuration.Release;

    [Parameter($"Docker containers type to be used in tests. One of '{ContainersNone}', '{ContainersLinux}', '{ContainersWindows}'. Default is '{ContainersLinux}'")]
    readonly string Containers = ContainersLinux;

    const string ContainersNone = "none";
    const string ContainersLinux = "linux";
    const string ContainersWindows = "windows";

    const string OpenTelemetryAutoInstrumentationDefaultVersion = "v1.14.1";

    [Parameter($"OpenTelemetry AutoInstrumentation dependency version - Default is '{OpenTelemetryAutoInstrumentationDefaultVersion}'")]
    readonly string OpenTelemetryAutoInstrumentationVersion = OpenTelemetryAutoInstrumentationDefaultVersion;

    readonly AbsolutePath OpenTelemetryDistributionFolder = RootDirectory / "OpenTelemetryDistribution";

    private IEnumerable<Project> AllProjectsExceptNuGetTestApps() => Solution.AllProjects.Where(project => !TestNuGetPackageApps.Contains(project.Directory));

    Target Clean => _ => _
        .After(SupportVs2026IfAvailable)
        .Executes(() =>
        {
            foreach (var project in Solution.AllProjects.Where(p => !p.Name.EndsWith(".NetFramework") && p.Name != "_build"))
            {
                DotNetClean(s => s.SetProject(project));
            }
            NuGetPackageFolder.DeleteDirectory();
            InstallationScriptsFolder.DeleteDirectory();
            MatrixScriptsFolder.DeleteDirectory();
            OpenTelemetryDistributionFolder.DeleteDirectory();
            (RootDirectory / GetOTelAutoInstrumentationFileName()).DeleteDirectory();
        });

    Target Restore => _ => _
        .After(Clean)
        .Executes(() =>
        {
            foreach (var project in AllProjectsExceptNuGetTestApps().Where(p => !LegacyMsBuildProjects.Contains(p.Name)))
            {
                DotNetRestore(s => s
                    .SetProjectFile(project));
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
                var architecture = RuntimeInformation.ProcessArchitecture;
                string architectureSuffix;
                switch (architecture)
                {
                    case Architecture.Arm64:
                        architectureSuffix = "arm64";
                        break;
                    case Architecture.X64:
                        architectureSuffix = "x64";
                        break;
                    default:
                        throw new NotSupportedException("Not supported Linux architecture " + architecture);
                }

                fileName = Environment.GetEnvironmentVariable("IsAlpine") == "true"
                    ? $"opentelemetry-dotnet-instrumentation-linux-musl-{architectureSuffix}.zip"
                    : $"opentelemetry-dotnet-instrumentation-linux-glibc-{architectureSuffix}.zip";
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
            (RootDirectory / "src" / "Splunk.OpenTelemetry.AutoInstrumentation" / "bin" / Configuration /
             "net8.0" / "Splunk.OpenTelemetry.AutoInstrumentation.dll").CopyToDirectory(OpenTelemetryDistributionFolder / "net");

            if (EnvironmentInfo.IsWin)
            {
                (RootDirectory / "src" / "Splunk.OpenTelemetry.AutoInstrumentation" / "bin" / Configuration /
                 "net462" / "Splunk.OpenTelemetry.AutoInstrumentation.dll").CopyToDirectory(OpenTelemetryDistributionFolder / "netfx");
            }
        });

    Target CopyInstrumentScripts => _ => _
        .After(AddSplunkPlugins)
        .Executes(() =>
        {
            var source = RootDirectory / "instrument.sh";
            var dest = OpenTelemetryDistributionFolder;
            source.CopyToDirectory(dest, ExistsPolicy.FileOverwrite);
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

    // Projects that cannot be built with dotnet build (classic ASP.NET projects)
    private static readonly string[] LegacyMsBuildProjects = new[]
    {
        "TestApplication.Snapshots.NetFramework"
    };

    Target Compile => _ => _
        .After(Restore)
        .After(UnpackAutoInstrumentationDistribution)
        .Executes(() =>
        {
            foreach (var project in AllProjectsExceptNuGetTestApps().Where(p => !LegacyMsBuildProjects.Contains(p.Name)))
            {
                DotNetBuild(s => s
                    .SetProjectFile(project)
                    .SetNoRestore(true)
                    .SetConfiguration(Configuration));
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
                .SetConfiguration(Configuration));
        });

    Target RunIntegrationTests => _ => _
        .After(Compile)
        .After(AddSplunkPlugins)
        .After(PublishIisTestApplications)
        .Executes(() =>
        {
            var project = Solution.AllProjects.First(project => project.Name == "Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests");

            DotNetTest(s => s
                .SetNoBuild(true)
                .SetProjectFile(project)
                .SetFilter("Category!=NuGetPackage")
                .SetConfiguration(Configuration));
        });

    Target Workflow => _ => _
        .DependsOn(SupportVs2026IfAvailable)
        .DependsOn(Clean)
        .DependsOn(Restore)
        .DependsOn(RestoreLegacyNuGetPackages)
        .DependsOn(SerializeMatrix)
        .DependsOn(BuildInstallationScripts)
        .DependsOn(DownloadAutoInstrumentationDistribution)
        .DependsOn(UnpackAutoInstrumentationDistribution)
        .DependsOn(Compile)
        .DependsOn(PublishIisTestApplications)
        .DependsOn(AddSplunkPlugins)
        .DependsOn(CopyInstrumentScripts)
        .DependsOn(ExtendLicenseFile)
        .DependsOn(RunUnitTests)
        .DependsOn(RunIntegrationTests)
        .DependsOn(PackSplunkDistribution);
}
