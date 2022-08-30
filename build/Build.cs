using Nuke.Common;
using Nuke.Common.Tools.DotNet;

class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Release'")]
    readonly Configuration Configuration = Configuration.Release;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetClean();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore();
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                    .SetNoRestore(true)
                    .SetConfiguration(Configuration));
        });

    Target Test => _ => _
        .After(Compile)
        .Executes(() =>
        {
            DotNetTasks.DotNetTest(s => s
                .SetNoBuild(true)
                .SetConfiguration(Configuration));
        });

    Target Workflow => _ => _
        .DependsOn(Clean)
        .DependsOn(Compile)
        .DependsOn(Test);
}
