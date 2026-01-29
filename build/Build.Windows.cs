using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;
using static Nuke.Common.Tools.Docker.DockerTasks;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
using static Nuke.Common.Tools.NuGet.NuGetTasks;
partial class Build
{
    Target RestoreLegacyNuGetPackages => _ => _
        .Unlisted()
        .After(Restore)
        .OnlyWhenStatic(() => EnvironmentInfo.IsWin)
        .Executes(() =>
        {
            var aspNetProject = Solution.AllProjects.FirstOrDefault(p => p.Name == "TestApplication.Snapshots.NetFramework");
            if (aspNetProject != null && (aspNetProject.Directory / "packages.config").FileExists())
            {
                NuGetRestore(s => s
                    .SetTargetPath(aspNetProject.Path)
                    .SetPackagesDirectory(RootDirectory / "packages"));
            }
        });
    Target PublishIisTestApplications => _ => _
        .Unlisted()
        .After(Compile)
        .After(BuildInstallationScripts)
        .After(RestoreLegacyNuGetPackages)
        .After(AddSplunkPlugins)
        .OnlyWhenStatic(() => EnvironmentInfo.IsWin && Containers == ContainersWindows)
        .Executes(() =>
        {
            var aspNetProject = Solution.AllProjects.FirstOrDefault(p => p.Name == "TestApplication.Snapshots.NetFramework");
            if (aspNetProject != null)
            {
                BuildDockerImage(aspNetProject);
            }
        });

    Target SupportVs2026IfAvailable => _ => _
        .OnlyWhenStatic(() => EnvironmentInfo.IsWin)
        .Executes(() =>
        {

            // Typical installation folder: C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\amd64
            // Waiting for official support in Nuke package https://github.com/nuke-build/nuke/pull/1583

            string[] editions = ["Enterprise", "Professional", "Community", "Preview"];

            foreach (var edition in editions)
            {
                var msBuildPath = Path.Combine(EnvironmentInfo.SpecialFolder(SpecialFolders.ProgramFiles).NotNull(),
                    $@"Microsoft Visual Studio\18\{edition}\MSBuild\Current\Bin\amd64\MSBuild.exe");

                if (File.Exists(msBuildPath))
                {
                    MSBuildPath = msBuildPath;
                    return;
                }
            }
        });

    void BuildDockerImage(Project project)
    {
        const string moduleName = "Splunk.OTel.DotNet.psm1";
        var sourceModulePath = InstallationScriptsFolder / moduleName;
        var localBinDirectory = project.Directory / "bin";
        var localTracerZip = localBinDirectory / "tracer.zip";
        try
        {
            sourceModulePath.CopyToDirectory(localBinDirectory, ExistsPolicy.FileOverwrite);
            OpenTelemetryDistributionFolder.ZipTo(localTracerZip, fileMode: FileMode.Create);
            if ((project.Directory / "packages.config").FileExists())
            {
                NuGetRestore(s => s
                    .SetTargetPath(project.Path)
                    .SetPackagesDirectory(RootDirectory / "packages"));
            }
            MSBuild(x => x
                .SetConfiguration(Configuration)
                .SetTargetPlatform(MSBuildTargetPlatform.x64)
                .SetProperty("DeployOnBuild", true)
                .SetMaxCpuCount(null)
                .SetProperty("PublishProfile",
                    project.Directory / "Properties" / "PublishProfiles" / $"FolderProfile.{Configuration}.pubxml")
                .SetTargetPath(project));
            DockerBuild(x => x
                .SetPath(".")
                .SetBuildArg($"configuration={Configuration}")
                .EnableRm()
                .SetProcessWorkingDirectory(project.Directory)
                .SetTag($"{Path.GetFileNameWithoutExtension(project.Path)?.Replace(".", "-")}".ToLowerInvariant())
            );
        }
        finally
        {
            localTracerZip.DeleteFile();
            var localModulePath = localBinDirectory / moduleName;
            localModulePath.DeleteFile();
        }
    }

}
