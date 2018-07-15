using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Application;
using BuildResult = UnityEditor.Build.Reporting.BuildResult;

public class Build : MonoBehaviour
{
    static readonly string[] whitelistedItems = new string[]
    {
        ".git",
        ".gitignore",
        ".gitattributes",
        "README.md"
    };

    static readonly string ProjectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

    static readonly string apkPath = Path.Combine(ProjectPath, "Builds/" + Application.productName + ".apk");

    [MenuItem("Build/Export Android %&a", false, 1)]
    public static void DoBuildAndroid()
    {
        string buildPath = Path.Combine(apkPath, Application.productName);
        string exportPath = Path.GetFullPath(Path.Combine(ProjectPath, "../../android/UnityExport"));

        if (Directory.Exists(apkPath))
            Directory.Delete(apkPath, true);

        DeleteFolderContent(exportPath);

        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

        var options = BuildOptions.AcceptExternalModificationsToPlayer;
        var report = BuildPipeline.BuildPlayer(
            GetEnabledScenes(),
            apkPath,
            BuildTarget.Android,
            options
        );

        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Build failed");

        Copy(buildPath, exportPath);

        // Modify build.gradle
        var build_file = Path.Combine(exportPath, "build.gradle");
        var build_text = File.ReadAllText(build_file);
        build_text = build_text.Replace("com.android.application", "com.android.library");
        build_text = Regex.Replace(build_text, @"com.android.tools.build:gradle:[0-9\.]+", "com.android.tools.build:gradle:3.1.3");
        build_text = Regex.Replace(build_text, @"\n.*applicationId '.+'.*\n", "\n");
        build_text = Regex.Replace(build_text, @"dependencies\s+\{[^\}]+\}", d => Regex.Replace(d.Value, @"(\s+)(compile)([^\n]+\n)", m => m.Groups[1].Value + "api" + m.Groups[3].Value));
        build_text = Regex.Replace(build_text, @"dependencies\s+\{[^\}]+\}", d => Regex.Replace(d.Value, @"(\s+)(implementation)([^\n]+\n)", m => m.Groups[1].Value + "api" + m.Groups[3].Value));
        if (!Regex.IsMatch(build_text, @"buildscript[^\{]+\{[^\{]+repositories[^\{]+\{[^\}]+google\(\)[^\}]"))
        {
            build_text = Regex.Replace(build_text, @"(buildscript[^\{]+\{[^\{]+repositories[^\{]+\{)([^\}]+)\n([^\n]+\})", d =>
            {
                var repos = d.Groups[2].Value.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                repos.Add(Regex.Replace(repos[0], @"[a-zA-Z0-9]+", @"google"));
                return d.Groups[1].Value + "\n" + string.Join("\n", repos) + "\n" + d.Groups[3].Value;
            });
        }
        File.WriteAllText(build_file, build_text);

        // Modify AndroidManifest.xml
        var manifest_file = Path.Combine(exportPath, "src/main/AndroidManifest.xml");
        var manifest_text = File.ReadAllText(manifest_file);
        manifest_text = Regex.Replace(manifest_text, @"<application .*>", "<application>");
        Regex regex = new Regex(@"<activity.*>(\s|\S)+?</activity>", RegexOptions.Multiline);
        manifest_text = regex.Replace(manifest_text, "");
        File.WriteAllText(manifest_file, manifest_text);
    }

    [MenuItem("Build/Export IOS %&i", false, 2)]
    public static void DoBuildIOS()
    {
        string exportPath = Path.GetFullPath(Path.Combine(ProjectPath, "../../ios/UnityExport"));

        DeleteFolderContent(exportPath);

        EditorUserBuildSettings.iOSBuildConfigType = iOSBuildType.Release;

        var options = BuildOptions.AcceptExternalModificationsToPlayer;
        var report = BuildPipeline.BuildPlayer(
            GetEnabledScenes(),
            exportPath,
            BuildTarget.iOS,
            options
        );

        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Build failed");
    }

    [MenuItem("Build/Export UWP %&w", false, 1)]
    public static void DoBuildUWP()
    {
        string exportPath = Path.GetFullPath(Path.Combine(ProjectPath, "../../windows/UnityExport"));

        DeleteFolderContent(exportPath);

        EditorUserBuildSettings.wsaBuildAndRunDeployTarget = WSABuildAndRunDeployTarget.LocalMachine;
        EditorUserBuildSettings.wsaGenerateReferenceProjects = true;
        EditorUserBuildSettings.wsaSubtarget = WSASubtarget.PC;
        EditorUserBuildSettings.wsaUWPBuildType = WSAUWPBuildType.XAML;
        EditorUserBuildSettings.buildScriptsOnly = false;
        EditorUserBuildSettings.installInBuildFolder = false;
        EditorUserBuildSettings.SetWSADotNetNative(WSABuildType.Master, false);

        var options = BuildOptions.AcceptExternalModificationsToPlayer;
        var report = BuildPipeline.BuildPlayer(
            GetEnabledScenes(),
            exportPath,
            BuildTarget.WSAPlayer,
            options
        );

        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Build failed");
    }

    static void Copy(string source, string destinationPath)
    {
        DeleteFolderContent(destinationPath);

        Directory.CreateDirectory(destinationPath);

        foreach (string dirPath in Directory.GetDirectories(source, "*",
            SearchOption.AllDirectories))
            Directory.CreateDirectory(dirPath.Replace(source, destinationPath));

        foreach (string newPath in Directory.GetFiles(source, "*.*",
            SearchOption.AllDirectories))
            File.Copy(newPath, newPath.Replace(source, destinationPath), true);
    }

    static string[] GetEnabledScenes()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        return scenes;
    }

    private static void DeleteFolderContent(string path)
    {
        if (Directory.Exists(path))
        {
            string[] files = Directory.GetFiles(path);
            string[] dirs = Directory.GetDirectories(path);

            foreach (string file in files)
            {
                if (whitelistedItems.Any(f => file.EndsWith(f, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                if (whitelistedItems.Any(f => dir.EndsWith(f, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                Directory.Delete(dir, true);
            }
        }
    }
}
