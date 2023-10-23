using MatrixHelper;
using Nuke.Common;
using Nuke.Common.IO;

partial class Build : NukeBuild
{
    readonly AbsolutePath MatrixScriptsFolder = RootDirectory / "bin" / "Matrix";

    Target SerializeMatrix => _ => _
        .After(Clean)
        .Executes(() =>
        {
            MatrixSerializer.Serialize(MatrixScriptsFolder / "splunk-otel-dotnet-metadata.yaml");
        });
}
