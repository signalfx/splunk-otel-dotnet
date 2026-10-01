using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build : NukeBuild
{
    private readonly static AbsolutePath TestNuGetPackageApps = NukeBuild.RootDirectory / "test" / "test-applications" / "nuget-package";

    [Solution("Splunk.OpenTelemetry.AutoInstrumentation.slnx")] readonly Solution Solution;
    public static int Main() => Execute<Build>(x => x.Workflow);

    [Parameter("Configuration to build - Default is 'Release'")]
    readonly Configuration Configuration = Configuration.Release;

    const string OpenTelemetryAutoInstrumentationDefaultVersion = "v1.17.0";

    [Parameter($"OpenTelemetry AutoInstrumentation dependency version - Default is '{OpenTelemetryAutoInstrumentationDefaultVersion}'")]
    readonly string OpenTelemetryAutoInstrumentationVersion = OpenTelemetryAutoInstrumentationDefaultVersion;

    [Parameter("Skip OpenTelemetry AutoInstrumentation release and artifact attestation verification. Local builds only.")]
    readonly bool SkipOpenTelemetryAutoInstrumentationVerification;

    const string OpenTelemetryAutoInstrumentationRepository = "open-telemetry/opentelemetry-dotnet-instrumentation";
    const string OpenTelemetryAutoInstrumentationReleaseWorkflow = OpenTelemetryAutoInstrumentationRepository + "/.github/workflows/release.yml";
    static readonly Version MinimumGitHubCliVersion = new(2, 93, 0);
    static readonly Regex ReleaseVersionRegex = new(
        @"^v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$",
        RegexOptions.CultureInvariant);

    readonly AbsolutePath OpenTelemetryDistributionFolder = RootDirectory / "OpenTelemetryDistribution";

    private IEnumerable<Project> AllProjectsExceptNuGetTestApps() => Solution.AllProjects.Where(project => !TestNuGetPackageApps.Contains(project.Directory));

    Target Clean => _ => _
        .Executes(() =>
        {
            DotNetClean();
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
            foreach (var project in AllProjectsExceptNuGetTestApps())
            {
                DotNetRestore(s => s
                    .SetProjectFile(project));
            }
        });

    Target DownloadAutoInstrumentationDistribution => _ => _
        .Executes(async () =>
        {
            AssertValidOpenTelemetryAutoInstrumentationVersion();

            var fileName = GetOTelAutoInstrumentationFileName();

            var uri =
                $"https://github.com/{OpenTelemetryAutoInstrumentationRepository}/releases/download/{OpenTelemetryAutoInstrumentationVersion}/{fileName}";

            await HttpTasks.HttpDownloadFileAsync(uri, RootDirectory / fileName, clientConfigurator: httpClient =>
            {
                httpClient.Timeout = TimeSpan.FromMinutes(3);
                return httpClient;
            });
        });

    Target VerifyAutoInstrumentationDistribution => _ => _
        .DependsOn(DownloadAutoInstrumentationDistribution)
        .Executes(() =>
        {
            AssertValidOpenTelemetryAutoInstrumentationVersion();

            if (SkipOpenTelemetryAutoInstrumentationVerification)
            {
                if (!IsLocalBuild)
                {
                    throw new InvalidOperationException("OpenTelemetry AutoInstrumentation verification cannot be skipped on CI builds.");
                }

                Log.Warning("OpenTelemetry AutoInstrumentation release and artifact attestation verification is skipped.");
                return;
            }

            AssertSupportedGitHubCliVersion();

            var archivePath = RootDirectory / GetOTelAutoInstrumentationFileName();

            ProcessTasks.StartProcess(
                    "gh",
                    $"release verify-asset {OpenTelemetryAutoInstrumentationVersion} \"{archivePath}\" --repo {OpenTelemetryAutoInstrumentationRepository}")
                .AssertZeroExitCode();

            ProcessTasks.StartProcess(
                    "gh",
                    $"attestation verify \"{archivePath}\" --repo {OpenTelemetryAutoInstrumentationRepository} --signer-workflow {OpenTelemetryAutoInstrumentationReleaseWorkflow} --source-ref refs/tags/{OpenTelemetryAutoInstrumentationVersion}")
                .AssertZeroExitCode();
        });

    Target UnpackAutoInstrumentationDistribution => _ => _
        .DependsOn(VerifyAutoInstrumentationDistribution)
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

    void AssertValidOpenTelemetryAutoInstrumentationVersion()
    {
        if (!ReleaseVersionRegex.IsMatch(OpenTelemetryAutoInstrumentationVersion))
        {
            throw new InvalidOperationException(
                $"Invalid OpenTelemetry AutoInstrumentation version '{OpenTelemetryAutoInstrumentationVersion}'. Expected vMAJOR.MINOR.PATCH or vMAJOR.MINOR.PATCH-PRERELEASE.");
        }
    }

    static void AssertSupportedGitHubCliVersion()
    {
        var process = ProcessTasks.StartProcess("gh", "--version", logOutput: false)
            .AssertZeroExitCode();
        var versionOutput = process.Output.Select(output => output.Text).FirstOrDefault() ?? string.Empty;
        var versionParts = versionOutput.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (versionParts.Length < 3 ||
            versionParts[0] != "gh" ||
            versionParts[1] != "version" ||
            !Version.TryParse(versionParts[2], out var version))
        {
            throw new InvalidOperationException($"Unable to determine the installed GitHub CLI version from: {versionOutput}");
        }

        if (version.CompareTo(MinimumGitHubCliVersion) < 0)
        {
            throw new InvalidOperationException(
                $"GitHub CLI {MinimumGitHubCliVersion} or newer is required for secure release verification. Installed version: {version}.");
        }
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

    Target CreateSplunkVersionFile => _ => _
        .Unlisted()
        .After(UnpackAutoInstrumentationDistribution)
        .Executes(() =>
        {
            var version = VersionHelper.GetVersion();
            var refName = "local-dev";
            var gitSha = VersionHelper.GetCommitId();

            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") is "true")
            {
                refName = Environment.GetEnvironmentVariable("GITHUB_REF_NAME");
            }

            var dest = OpenTelemetryDistributionFolder / "SPLUNK_VERSION";
            dest.WriteAllLines([version, $"{refName}@{gitSha}"]);
        });

    Target PackSplunkDistribution => _ => _
        .After(CopyInstrumentScripts)
        .After(ExtendLicenseFile)
        .After(CreateSplunkVersionFile)
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
        .After(CreateSplunkVersionFile)
        .After(AddSplunkPlugins)
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
        .DependsOn(Clean)
        .DependsOn(Restore)
        .DependsOn(SerializeMatrix)
        .DependsOn(BuildInstallationScripts)
        .DependsOn(DownloadAutoInstrumentationDistribution)
        .DependsOn(VerifyAutoInstrumentationDistribution)
        .DependsOn(UnpackAutoInstrumentationDistribution)
        .DependsOn(Compile)
        .DependsOn(AddSplunkPlugins)
        .DependsOn(CopyInstrumentScripts)
        .DependsOn(ExtendLicenseFile)
        .DependsOn(CreateSplunkVersionFile)
        .DependsOn(RunUnitTests)
        .DependsOn(RunIntegrationTests)
        .DependsOn(PackSplunkDistribution);
}
