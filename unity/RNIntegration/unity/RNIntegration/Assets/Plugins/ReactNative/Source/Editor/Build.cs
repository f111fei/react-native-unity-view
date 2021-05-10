using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using Application = UnityEngine.Application;
using BuildResult = UnityEditor.Build.Reporting.BuildResult;

public static class Build
{
    public static BuildTargetGroup? CurrentGroup { get; private set; } = null;

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
        try
        {
            CurrentGroup = BuildTargetGroup.Android;

            string exportPath = Path.GetFullPath(Path.Combine(ProjectPath, "../../android/UnityExport"));
            string buildPath = apkPath;

            if (Directory.Exists(apkPath))
                Directory.Delete(apkPath, true);

            DeleteFolderContent(exportPath);

            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

            string oldScriptingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, oldScriptingDefines + ";UNITY_EXPORT");

            try
            {
                var report = BuildPipeline.BuildPlayer(
                    GetEnabledScenes(),
                    apkPath,
                    BuildTarget.Android,
                    BuildOptions.AllowDebugging);

                if (report.summary.result != BuildResult.Succeeded)
                    throw new Exception("Build failed");

                // Copy build output from UnityExport
                CopyDirectory(
                    buildPath,
                    exportPath,
                    mergeDirectories: false,
                    overwriteFiles: true);

                // Copy gradle.properties
                CopyFile(
                    Path.Combine(exportPath, "gradle.properties"),
                    Path.Combine(exportPath, "unityLibrary/gradle.properties"),
                    overwriteFiles: true);

                // Copy some files from 'launcher' project
                CopyDirectory(
                    Path.Combine(exportPath, "launcher/src/main/res"),
                    Path.Combine(exportPath, "unityLibrary/src/main/res"),
                    mergeDirectories: true,
                    overwriteFiles: true);

                // Modify build.gradle
                {
                    var launcher_build_file = Path.Combine(exportPath, "launcher/build.gradle");
                    var launcher_build_text = File.ReadAllText(launcher_build_file);

                    var build_file = Path.Combine(exportPath, "unityLibrary/build.gradle");
                    var build_text = File.ReadAllText(build_file);
                    build_text = build_text.Replace("com.android.application", "com.android.library");
                    build_text = Regex.Replace(build_text, @"\n.*applicationId '.+'.*\n", "\n");
                    build_text = Regex.Replace(build_text, @":unityLibrary", ":UnityExport");
                    build_text = Regex.Replace(build_text, @"dependencies\s+\{[^\}]+\}", d =>
                    {
                        var value = d.Value;
                        value = Regex.Replace(value, @"(\s+)(implementation project)([^\n]+)'([^\n]+)('[^\n]+)", m => m.Groups[1].Value + "api project(':" + m.Groups[4].Value + m.Groups[5].Value);
                        value = Regex.Replace(value, @"(\s+)(compile)([^\n]+\n)", m => m.Groups[1].Value + "api" + m.Groups[3].Value);
                        value = Regex.Replace(value, @"(\s+)(implementation)([^\n]+\n)", m => m.Groups[1].Value + "api" + m.Groups[3].Value);
                        value = Regex.Replace(value, @"(\s+)api.+appcenter-release.+", m => m.Groups[1].Value + "api 'com.microsoft.appcenter:appcenter:+'");
                        value = Regex.Replace(value, @"(\s+)api.+appcenter-analytics-release.+", m => m.Groups[1].Value + "api 'com.microsoft.appcenter:appcenter-analytics:+'");
                        value = Regex.Replace(value, @"(\s+)api.+appcenter-crashes-release.+", m => m.Groups[1].Value + "api 'com.microsoft.appcenter:appcenter-crashes:+'");
                        return value;
                    });

                    build_text = CopyGradleBlock(
                        launcher_build_text,
                        build_text,
                        @"(\s+aaptOptions\s+\{[^\}]+\})",
                        @"(android\s+\{)(([^\}]+)+)",
                        overwrite: false);

                    build_text = CopyGradleBlock(
                        launcher_build_text,
                        build_text,
                        @"(\s+buildTypes\s+\{([^\{\}]+\{[^\{\}]+\}[^\{\}]+)+\})",
                        @"(android\s+\{)(([^\}]+)+)",
                        overwrite: false);

                    File.WriteAllText(build_file, build_text);
                }

                // Modify AndroidManifest.xml
                var manifest_file = Path.Combine(exportPath, "unityLibrary/src/main/AndroidManifest.xml");
                var manifest_text = File.ReadAllText(manifest_file);
                manifest_text = Regex.Replace(manifest_text, @"\s*<uses-sdk[^>]*/>", "");
                manifest_text = Regex.Replace(manifest_text, @"<application .*>", "<application>");
                Regex regex = new Regex(@"<activity.*>(\s|\S)+?</activity>", RegexOptions.Multiline);
                manifest_text = regex.Replace(manifest_text, "");
                File.WriteAllText(manifest_file, manifest_text);
            }
            finally
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, oldScriptingDefines);
            }
        }
        finally
        {
            CurrentGroup = null;
        }
    }

    [MenuItem("Build/Export IOS %&i", false, 2)]
    public static void DoBuildIOS()
    {
        try
        {
            CurrentGroup = BuildTargetGroup.iOS;

            string exportPath = Path.GetFullPath(Path.Combine(ProjectPath, "../../ios/UnityExport"));

            DeleteFolderContent(exportPath);

            EditorUserBuildSettings.iOSBuildConfigType = iOSBuildType.Release;

            string oldScriptingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, oldScriptingDefines + ";UNITY_EXPORT");

            try
            {
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
            finally
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, oldScriptingDefines);
            }
        }
        finally
        {
            CurrentGroup = null;
        }
    }

    [MenuItem("Build/Export UWP %&w", false, 1)]
    public static void DoBuildUWP()
    {
        try
        {
            CurrentGroup = BuildTargetGroup.WSA;

            string exportPath = Path.GetFullPath(Path.Combine(ProjectPath, "../../windows/UnityExport"));

            DeleteFolderContent(exportPath);

            EditorUserBuildSettings.wsaBuildAndRunDeployTarget = WSABuildAndRunDeployTarget.LocalMachine;
            // EditorUserBuildSettings.wsaGenerateReferenceProjects = true;
            EditorUserBuildSettings.wsaSubtarget = WSASubtarget.PC;
            EditorUserBuildSettings.wsaUWPBuildType = WSAUWPBuildType.XAML;
            EditorUserBuildSettings.buildScriptsOnly = false;
            EditorUserBuildSettings.installInBuildFolder = false;
            // EditorUserBuildSettings.SetWSADotNetNative(WSABuildType.Master, false);

            string oldScriptingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.WSA);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WSA, oldScriptingDefines + ";UNITY_EXPORT");

            try
            {
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
            finally
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WSA, oldScriptingDefines);
            }
        }
        finally
        {
            CurrentGroup = null;
        }
    }

    private static void CopyDirectory(
        string sourcePath,
        string destinationPath,
        bool mergeDirectories = false,
        bool overwriteFiles = true)
    {
        if (!mergeDirectories && Directory.Exists(destinationPath))
        {
            DeleteFolderContent(destinationPath);
        }

        if (!Directory.Exists(destinationPath))
        {
            Directory.CreateDirectory(destinationPath);
        }

        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            var newDirectoryPath = dirPath.Replace(sourcePath, destinationPath);

            if (!Directory.Exists(newDirectoryPath))
            {
                Directory.CreateDirectory(newDirectoryPath);
            }
        }

        foreach (string srcFilePath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            var destFilePath = srcFilePath.Replace(sourcePath, destinationPath);
            CopyFile(srcFilePath, destFilePath, overwriteFiles);
        }
    }

    private static void CopyFile(
        string sourcePath,
        string destinationPath,
        bool overwriteFiles = true)
    {
        File.Copy(sourcePath, destinationPath, overwriteFiles);
    }

    private static string CopyGradleBlock(
        string sourceGradleText,
        string destinationGradleText,
        string sourceRegex,
        string destinationRegex,
        bool overwrite)
    {
        var m = Regex.Match(sourceGradleText, sourceRegex);
        if (m.Success && (overwrite || !Regex.IsMatch(destinationGradleText, sourceRegex)))
        {
            return Regex.Replace(destinationGradleText, destinationRegex, d =>
            {
                return d.Groups[1].Value + m.Groups[1].Value + d.Groups[2].Value;
            });
        }

        return destinationGradleText;
    }

    private static string[] GetEnabledScenes()
        => EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

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
