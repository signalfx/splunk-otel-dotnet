using MatrixHelper;
using Fallout.Common;
using Fallout.Common.IO;

partial class Build : FalloutBuild
{
    readonly AbsolutePath MatrixScriptsFolder = RootDirectory / "bin" / "Matrix";

    Target SerializeMatrix => _ => _
        .After(Clean)
        .Executes(() =>
        {
            MatrixScriptsFolder.CreateOrCleanDirectory();
            MatrixSerializer.Serialize(MatrixScriptsFolder / "splunk-otel-dotnet-metadata.yaml");
        });
}
