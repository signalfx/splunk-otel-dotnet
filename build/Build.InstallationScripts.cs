using Nuke.Common;
using Nuke.Common.IO;

partial class Build : NukeBuild
{
    readonly AbsolutePath InstallationScriptsFolder = RootDirectory / "InstallationScripts";

    Target BuildInstallationScripts => _ => _
        .After(Clean)
        .Executes(() =>
        {
            var scriptTemplates = RootDirectory / "script-templates";
            var templateFiles = scriptTemplates.GetFiles();
            foreach (var templateFile in templateFiles)
            {
                var scriptFile = InstallationScriptsFolder / templateFile.Name.Replace(".template", "");
                FileSystemTasks.CopyFile(templateFile, scriptFile);
                scriptFile.UpdateText(x =>
                    x.Replace("VERSION_PLACEHOLDER", VersionHelper.GetVersion()));
            }
        });
}
