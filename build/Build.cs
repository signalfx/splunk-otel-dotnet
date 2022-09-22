using System;
using System.IO;
using System.IO.Compression;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Workflow);

    [Parameter("Configuration to build - Default is 'Release'")]
    readonly Configuration Configuration = Configuration.Release;

    const string OpenTelemetryAutoInstrumentationDefaultVersion = "v0.3.1-beta.1";
    [Parameter($"OpenTelemetry AutoInstrumentation dependency version - Default is '{OpenTelemetryAutoInstrumentationDefaultVersion}'")]
    readonly string OpenTelemetryAutoInstrumentationVersion = OpenTelemetryAutoInstrumentationDefaultVersion;

    readonly AbsolutePath BinDirectory = RootDirectory / "bin";

    Target Clean => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetClean();
            FileSystemTasks.DeleteDirectory(BinDirectory);
        });

    Target Restore => _ => _
        .After(Clean)
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore();
        });

    Target DownloadAutoInstrumentationDistribution => _ => _
        .Executes(async () =>
        {
            var fileName = GetOTelAutoInstrumentationFileName();

            var uri =
                $"https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/{OpenTelemetryAutoInstrumentationVersion}/{fileName}";

            await HttpTasks.HttpDownloadFileAsync(uri, BinDirectory / fileName, clientConfigurator: httpClient =>
            {
                httpClient.Timeout = TimeSpan.FromMinutes(3);
                return httpClient;
            });
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

    Target PackSplunkDistribution => _ => _
        .After(Compile)
        .After(DownloadAutoInstrumentationDistribution)
        .Executes(() =>
        {
            var fileName = GetOTelAutoInstrumentationFileName();
            var uncompressedFolder = BinDirectory / "uncompressed";
            FileSystemTasks.DeleteDirectory(uncompressedFolder);
            CompressionTasks.UncompressZip(BinDirectory / fileName, uncompressedFolder);

            FileSystemTasks.CopyFileToDirectory(RootDirectory / "src" / "Splunk.OpenTelemetry.AutoInstrumentation.Plugin" / "bin" / Configuration / "netstandard2.0" / "Splunk.OpenTelemetry.AutoInstrumentation.Plugin.dll", uncompressedFolder / "plugins");

            CompressionTasks.CompressZip(uncompressedFolder, RootDirectory / "bin" / ("splunk-" + fileName), compressionLevel: CompressionLevel.SmallestSize, fileMode: FileMode.Create);
        });

    Target Compile => _ => _
        .After(Restore)
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
        .DependsOn(Restore)
        .DependsOn(DownloadAutoInstrumentationDistribution)
        .DependsOn(Compile)
        .DependsOn(Test)
        .DependsOn(PackSplunkDistribution);
}
