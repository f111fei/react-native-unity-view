using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
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

    static readonly string androidBuildPath = Path.Combine(ProjectPath, "Builds/android/" + Application.productName);
    static readonly string iosBuildPath = Path.Combine(ProjectPath, "Builds/ios/" + Application.productName);

    [MenuItem("Build/Export Android/Debug", false, 1)]
    public static void DoBuildAndroid_Debug() => DoBuildAndroidInternal(Il2CppCompilerConfiguration.Debug);

    [MenuItem("Build/Export Android/Release", false, 1)]
    public static void DoBuildAndroid_Release() => DoBuildAndroidInternal(Il2CppCompilerConfiguration.Release);

    [MenuItem("Build/Export Android/Master", false, 1)]
    public static void DoBuildAndroid() => DoBuildAndroidInternal(Il2CppCompilerConfiguration.Master);

    private static void DoBuildAndroidInternal(Il2CppCompilerConfiguration compilerConfiguration)
    {
        Debug.Log("Building Android...");

        CurrentGroup = BuildTargetGroup.Android;

        var prevCompilerConfiguration = PlayerSettings.GetIl2CppCompilerConfiguration(BuildTargetGroup.Android);
        var prevScriptingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
        var isDebug = !(compilerConfiguration == Il2CppCompilerConfiguration.Master);

        using var revertSettings = new Disposable(() =>
        {
            CurrentGroup = null;

            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, prevCompilerConfiguration);

            if (!Application.isBatchMode)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, prevScriptingDefines);
            }
        });

        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, compilerConfiguration);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, ProcessDefines(prevScriptingDefines, isDebug));

        string exportPath = Path.GetFullPath(Path.Combine(ProjectPath, "../../android/UnityExport"));
        string buildPath = androidBuildPath;

        if (Directory.Exists(buildPath))
            Directory.Delete(buildPath, true);

        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

        try
        {
            var options = (compilerConfiguration == Il2CppCompilerConfiguration.Debug ? BuildOptions.AllowDebugging : BuildOptions.None);
            var report = BuildPipeline.BuildPlayer(
                GetEnabledScenes(),
                buildPath,
                BuildTarget.Android,
                options);

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Build failed");

            // Modify build.gradle
            {
                Debug.Log("Patch launcher/build.gradle");
                var launcher_build_file = Path.Combine(buildPath, "launcher/build.gradle");
                var launcher_build_text = File.ReadAllText(launcher_build_file);

                Debug.Log("Patch unityLibrary/build.gradle");
                var build_file = Path.Combine(buildPath, "unityLibrary/build.gradle");
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
                build_text = Regex.Replace(build_text, @"\s*def BuildIl2Cpp\([^\(\)\{\}]*\)\s*\{", d =>
                {
                    var builder = new StringBuilder();
                    builder.AppendLine();
                    builder.AppendLine("def getUnityDir() {");
                    builder.AppendLine("    Properties local = new Properties()");
                    builder.AppendLine("    local.load(new FileInputStream(\"${rootDir}/local.properties\"))");
                    builder.AppendLine("    return local.getProperty('unity.dir')");
                    builder.AppendLine("}");
                    builder.AppendLine(d.Groups[0].Value);
                    builder.AppendLine("    String il2cppPath = getUnityDir();");
                    builder.AppendLine("    if (!il2cppPath) {");
                    builder.AppendLine("        il2cppPath = workingDir + \"/src/main/Il2CppOutputProject\"");
                    builder.AppendLine("    }");
                    return builder.ToString();
                });

                build_text = Regex.Replace(
                    build_text,
                    "commandLine\\(workingDir\\s*\\+\\s*\"/src/main/Il2CppOutputProject/IL2CPP/build/deploy/netcoreapp3\\.1/il2cpp\\.exe\",",
                    "commandLine(il2cppPath + \"/IL2CPP/build/deploy/netcoreapp3.1/il2cpp\",");

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
            Debug.Log("Patch AndroidManifest.xml");
            var manifest_file = Path.Combine(buildPath, "unityLibrary/src/main/AndroidManifest.xml");
            var manifest_text = File.ReadAllText(manifest_file);
            manifest_text = Regex.Replace(manifest_text, @"\s*<uses-sdk[^>]*/>", "");
            manifest_text = Regex.Replace(manifest_text, @"<application .*>", "<application>");
            Regex regex = new Regex(@"<activity.*>(\s|\S)+?</activity>", RegexOptions.Multiline);
            manifest_text = regex.Replace(manifest_text, "");
            File.WriteAllText(manifest_file, manifest_text);

            // Clear UnityExport
            DeleteFolderContent(exportPath);

            // Copy build output to UnityExport
            Debug.Log("Copy to UnityExport");
            CopyDirectory(
                buildPath,
                exportPath,
                mergeDirectories: false,
                overwriteFiles: true);

            // Copy local.properties
            Debug.Log("Copy local.properties");
            CopyFile(
                Path.Combine(exportPath, "local.properties"),
                Path.Combine(exportPath, "../local.properties"),
                overwriteFiles: true);

            // Copy gradle.properties
            Debug.Log("Copy gradle.properties");
            CopyFile(
                Path.Combine(exportPath, "gradle.properties"),
                Path.Combine(exportPath, "unityLibrary/gradle.properties"),
                overwriteFiles: true);

            // Copy some files from 'launcher' project
            Debug.Log("Copy resources");
            CopyDirectory(
                Path.Combine(exportPath, "launcher/src/main/res"),
                Path.Combine(exportPath, "unityLibrary/src/main/res"),
                mergeDirectories: true,
                overwriteFiles: true);
        }
        catch (Exception e)
        {
            Debug.Log("Export failed!");

            if (Application.isBatchMode)
            {
                Debug.LogError(e);
                EditorApplication.Exit(-1);
            }
            else
            {
                throw;
            }
        }
        finally
        {
            Debug.Log("Export completed!");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }

    [MenuItem("Build/Export IOS/Debug", false, 1)]
    public static void DoBuildIOS_Debug() => DoBuildIOSInternal(iOSBuildType.Debug, iOSSdkVersion.DeviceSDK);

    [MenuItem("Build/Export IOS/Release", false, 1)]
    public static void DoBuildIOS_Release() => DoBuildIOSInternal(iOSBuildType.Release, iOSSdkVersion.DeviceSDK);

    [MenuItem("Build/Export IOS/Debug [Simulator]", false, 1)]
    public static void DoBuildIOS_Debug_Simulator() => DoBuildIOSInternal(iOSBuildType.Debug, iOSSdkVersion.SimulatorSDK);

    [MenuItem("Build/Export IOS/Release [Simulator]", false, 1)]
    public static void DoBuildIOS_Release_Simulator() => DoBuildIOSInternal(iOSBuildType.Release, iOSSdkVersion.SimulatorSDK);

    private static void DoBuildIOSInternal(iOSBuildType buildType, iOSSdkVersion sdkVersion)
    {
        Debug.Log("Building iOS...");

        CurrentGroup = BuildTargetGroup.iOS;

        var prevCompilerConfiguration = PlayerSettings.GetIl2CppCompilerConfiguration(BuildTargetGroup.iOS);
        var prevScriptingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS);
        var prev_sdkVersion = PlayerSettings.iOS.sdkVersion;
        var isDebug = !(buildType == iOSBuildType.Debug);

        using var revertSettings = new Disposable(() =>
        {
            CurrentGroup = null;

            PlayerSettings.iOS.sdkVersion = prev_sdkVersion;
            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.iOS, prevCompilerConfiguration);

            if (!Application.isBatchMode)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, prevScriptingDefines);
            }
        });

        var compilerConfiguration = isDebug ? Il2CppCompilerConfiguration.Debug : Il2CppCompilerConfiguration.Master;
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.iOS, compilerConfiguration);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, ProcessDefines(prevScriptingDefines, isDebug));
        PlayerSettings.iOS.sdkVersion = sdkVersion;

        string exportPath = Path.GetFullPath(Path.Combine(ProjectPath, "../../ios/UnityExport"));
        string buildPath = iosBuildPath;

        if (Directory.Exists(buildPath))
            Directory.Delete(buildPath, true);

        EditorUserBuildSettings.iOSBuildConfigType = buildType;

        try
        {
            var options = (buildType == iOSBuildType.Debug ? BuildOptions.AllowDebugging : BuildOptions.None);
            var report = BuildPipeline.BuildPlayer(
                GetEnabledScenes(),
                buildPath,
                BuildTarget.iOS,
                options
            );

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Build failed");

            // Clear UnityExport
            DeleteFolderContent(exportPath);

            // Copy build output to UnityExport
            Debug.Log("Copy to UnityExport");
            CopyDirectory(
                buildPath,
                exportPath,
                mergeDirectories: false,
                overwriteFiles: true);
        }
        catch (Exception e)
        {
            Debug.Log("Export failed!");

            if (Application.isBatchMode)
            {
                Debug.LogError(e);
                EditorApplication.Exit(-1);
            }
            else
            {
                throw;
            }
        }
        finally
        {
            Debug.Log("Export completed!");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
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
            string newScriptingDefines = ProcessDefines(oldScriptingDefines, true);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WSA, newScriptingDefines);

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
            catch (Exception e)
            {
                Debug.Log("Export failed!");

                if (Application.isBatchMode)
                {
                    Debug.LogError(e);
                    EditorApplication.Exit(-1);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                Debug.Log("Export completed!");

                if (!Application.isBatchMode)
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WSA, oldScriptingDefines);
                }
                else
                {
                    EditorApplication.Exit(0);
                }
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
        if (overwriteFiles || !File.Exists(destinationPath))
        {
            File.Copy(sourcePath, destinationPath, overwriteFiles);
        }
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

    private static string ProcessDefines(string defines, bool isDebug)
    {
        defines = Regex.Replace(defines, ";?UNITY_STANDALONE;?", ";");

        if (isDebug)
        {
            defines = Regex.Replace(defines, ";?ENABLE_TRACE_LOGGING;?", ";");
            defines = Regex.Replace(defines, ";?ENABLE_DEBUG_LOGGING;?", ";");
        }

        return $"{defines};UNITY_EXPORT";
    }
}
