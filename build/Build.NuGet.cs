using System.Runtime.InteropServices;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;

using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build : NukeBuild
{
    readonly AbsolutePath NuGetPackageFolder = RootDirectory / "NuGetPackage";

    Target BuildNuGetPackage => _ => _
        .Executes(() =>
        {
            var project = Solution.AllProjects.First(project => project.Name == "Splunk.OpenTelemetry.AutoInstrumentation");

            DotNetPack(s => s
                .SetProject(project)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(NuGetPackageFolder));
        });


    Target BuildNuGetPackageTestApplication => _ => _
        .After(BuildNuGetPackage)
        .Executes(() =>
        {
            var project = Solution.AllProjects.First(project => project.Name == "TestApplication.SelfContained");
            DotNetBuild(s => s
                .SetProjectFile(project)
                .SetProperty("NuGetPackageVersion", VersionHelper.GetVersion())
                .SetRuntime(RuntimeInformation.RuntimeIdentifier)
                .SetSelfContained(true)
                .SetConfiguration(Configuration)
                .SetPlatform(Platform));
        });

    Target RunNuGetPackageIntegrationTests => _ => _
        .After(BuildNuGetPackageTestApplication)
        .Executes(() =>
        {
            var project = Solution.AllProjects.First(project => project.Name == "Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests");

            DotNetTest(s => s
                .SetProjectFile(project)
                .SetFilter("Category=NuGetPackage")
                .SetConfiguration(Configuration));
        });

    Target TestNuGetPackage => _ => _
        .DependsOn(BuildNuGetPackageTestApplication)
        .DependsOn(RunNuGetPackageIntegrationTests)
        .Executes(() =>
        {
        });

    Target NuGetWorkflow => _ => _
        .DependsOn(BuildNuGetPackage)
        .DependsOn(TestNuGetPackage);
}
