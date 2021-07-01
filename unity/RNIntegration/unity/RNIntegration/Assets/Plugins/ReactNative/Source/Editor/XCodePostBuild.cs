/*
MIT License
Copyright (c) 2017 Jiulong Wang
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#if UNITY_IOS

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using Application = UnityEngine.Application;

/// <summary>
/// Adding this post build script to Unity project enables Unity iOS build output to be embedded
/// into existing Xcode Swift project.
///
/// However, since this script touches Unity iOS build output, you will not be able to use Unity
/// iOS build directly in Xcode. As a result, it is recommended to put Unity iOS build output into
/// a temporary directory that you generally do not touch, such as '/tmp'.
///
/// In order for this to work, necessary changes to the target Xcode Swift project are needed.
/// Especially the 'AppDelegate.swift' should be modified to properly initialize Unity.
/// See https://github.com/jiulongw/swift-unity for details.
/// </summary>
public static class XcodePostBuild
{
    /// <summary>
    /// Name of the Xcode project.
    /// This script looks for '${XcodeProjectName} + ".xcodeproj"' under '${XcodeProjectRoot}'.
    /// Sample value: "DemoApp"
    /// </summary>
    private static string XcodeProjectName = "Unity-iPhone";
    private static string UnityFrameworkTargetName = "UnityFramework";

    /// <summary>
    /// Path, relative to the root directory of the Xcode project, to put information about generated Unity output.
    /// </summary>
    //private static string ExportsConfigProjectPath =  "UnityExport/Exports.xcconfig";

    private static string PbxFilePath = XcodeProjectName + ".xcodeproj/project.pbxproj";

    /// <summary>
    /// The identifier added to touched file to avoid double edits when building to existing directory without
    /// replace existing content.
    /// </summary>
    private const string TouchedMarker = "https://github.com/coder89/react-native-unity-view";

    [PostProcessBuild]
    public static void OnPostBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
        {
            return;
        }

        PatchUnityNativeCode(pathToBuiltProject);

        UpdateUnityProjectFiles(pathToBuiltProject);
    }

    /// <summary>
    /// Enumerates Unity output files and add necessary files into Xcode project file.
    /// It only add a reference entry into project.pbx file, without actually copy it.
    /// Xcode pre-build script will copy files into correct location.
    /// </summary>
    private static void UpdateUnityProjectFiles(string pathToBuiltProject)
    {
        var pbx = new PBXProject();
        var pbxPath = Path.Combine(pathToBuiltProject, PbxFilePath);
        pbx.ReadFromFile(pbxPath);

        // Add Data to UnityFramework target
        var targetGuid = pbx.TargetGuidByName(UnityFrameworkTargetName);
        var targetGuid2 = pbx.GetUnityFrameworkTargetGuid();
        var fileGuid = pbx.FindFileGuidByProjectPath("Data");
        pbx.AddFileToBuild(targetGuid, fileGuid);

        pbx.WriteToFile(pbxPath);
    }

    /// <summary>
    /// Make necessary changes to Unity build output that enables it to be embedded into existing Xcode project.
    /// </summary>
    private static void PatchUnityNativeCode(string pathToBuiltProject)
    {
        EditMainMM(Path.Combine(pathToBuiltProject, "Classes/main.mm"));
        EditUnityAppControllerH(Path.Combine(pathToBuiltProject, "Classes/UnityAppController.h"));
        EditUnityAppControllerMM(Path.Combine(pathToBuiltProject, "Classes/UnityAppController.mm"));

        if (Application.unityVersion == "2017.1.1f1")
        {
            EditMetalHelperMM(Path.Combine(pathToBuiltProject, "Classes/Unity/MetalHelper.mm"));
        }

        // TODO: Parse unity version number and do range comparison.
        if (Application.unityVersion.StartsWith("2017.3.0f") || Application.unityVersion.StartsWith("2017.3.1f"))
        {
            EditSplashScreenMM(Path.Combine(pathToBuiltProject, "Classes/UI/SplashScreen.mm"));
        }
    }

    /// <summary>
    /// Edit 'main.mm': removes 'main' entry that would conflict with the Xcode project it embeds into.
    /// </summary>
    private static void EditMainMM(string path)
    {
        EditCodeFile(path, line =>
        {
            if (line.TrimStart().StartsWith("int main", StringComparison.Ordinal))
            {
                return line.Replace("int main", "int old_main");
            }

            return line;
        });
    }

    /// <summary>
    /// Edit 'UnityAppController.h': returns 'UnityAppController' from 'AppDelegate' class.
    /// </summary>
    private static void EditUnityAppControllerH(string path)
    {
        var inScope = false;
        var markerDetected = false;
        var markerAdded = false;
		
		// Add static GetAppController
		EditCodeFile(path, line =>
        {
			inScope |= line.Contains("- (void)startUnity:");

			if (inScope)
			{
				if (line.Trim() == "")
				{
					inScope = false;

					return new string[]
					{
						"",
						"// Added by " + TouchedMarker,
						"+ (UnityAppController*)GetAppController;",
						""
					};
				}
			}
			
			return new string[] { line };
		});

		inScope = false;
		markerDetected = false;

		// Modify inline GetAppController
        EditCodeFile(path, line =>
        {
            inScope |= line.Contains("inline UnityAppController");

            if (inScope && !markerDetected)
            {
                if (line.Trim() == "}")
                {
                    inScope = false;
					markerDetected = true;

                    return new string[]
                    {
                        "// }",
                        "",
                        "static inline UnityAppController* GetAppController()",
                        "{",
                        "    return [UnityAppController GetAppController];",
                        "}",
                    };
                }

                if (!markerAdded)
                {
                    markerAdded = true;
                    return new string[]
                    {
                        "// Modified by " + TouchedMarker,
                        "// " + line,
                    };
                }

                return new string[] { "// " + line };
            }

            return new string[] { line };
        });
    }

    /// <summary>
    /// Edit 'UnityAppController.mm': triggers 'UnityReady' notification after Unity is actually started.
    /// </summary>
    private static void EditUnityAppControllerMM(string path)
    {
        EditCodeFile(path, line =>
        {
            if (line.Trim() == "@end")
            {
                return new string[]
				{
					"",
					"// Added by " + TouchedMarker,
					"static UnityAppController *unityAppController = nil;",
					"",
					@"+ (UnityAppController*)GetAppController",
					"{",
					"    static dispatch_once_t onceToken;",
					"    dispatch_once(&onceToken, ^{",
					"        unityAppController = [[self alloc] init];",
					"    });",
					"    return unityAppController;",
					"}",
					"",
					line,
				};
            }

            return new string[] { line };
        });
    }

    /// <summary>
    /// Edit 'MetalHelper.mm': fixes a bug (only in 2017.1.1f1) that causes crash.
    /// </summary>
    private static void EditMetalHelperMM(string path)
    {
        var markerDetected = false;

        EditCodeFile(path, line =>
        {
            markerDetected |= line.Contains(TouchedMarker);

            if (!markerDetected && line.Trim() == "surface->stencilRB = [surface->device newTextureWithDescriptor: stencilTexDesc];")
            {
                return new string[]
                {
                    "",
                    "    // Modified by " + TouchedMarker,
                    "    // Default stencilTexDesc.usage has flag 1. In runtime it will cause assertion failure:",
                    "    // validateRenderPassDescriptor:589: failed assertion `Texture at stencilAttachment has usage (0x01) which doesn't specify MTLTextureUsageRenderTarget (0x04)'",
                    "    // Adding MTLTextureUsageRenderTarget seems to fix this issue.",
                    "    stencilTexDesc.usage |= MTLTextureUsageRenderTarget;",
                    line,
                };
            }

            return new string[] { line };
        });
    }

    // TODO: Verify this is not breaking anything (check github forks and history!)
    /// <summary>
    /// Edit 'SplashScreen.mm': Unity introduces its own 'LaunchScreen.storyboard' since 2017.3.0f3.
    /// Disable it here and use Swift project's launch screen instead.
    /// </summary>
    private static void EditSplashScreenMM(string path) {
        var markerDetected = false;
        var markerAdded = false;
        var inScope = false;
        var level = 0;

        EditCodeFile(path, line =>
        {
            inScope |= line.Trim() == "void ShowSplashScreen(UIWindow* window)";
            markerDetected |= line.Contains(TouchedMarker);

            if (inScope && !markerDetected)
            {
                if (line.Trim() == "{")
                {
                    level++;
                }
                else if (line.Trim() == "}")
                {
                    level--;
                }

                if (line.Trim() == "}" && level == 0)
                {
                    inScope = false;
                }

                if (level > 0 && line.Trim().StartsWith("bool hasStoryboard"))
                {
                    return new string[]
                    {
                        "    // " + line,
                        "    bool hasStoryboard = false;",
                    };
                }

                if (!markerAdded)
                {
                    markerAdded = true;
                    return new string[]
                    {
                        "// Modified by " + TouchedMarker,
                        line,
                    };
                }
            }

            return new string[] { line };
        });
    }

    private static void EditCodeFile(string path, Func<string, string> lineHandler)
    {
        EditCodeFile(path, line =>
        {
            return new string[] { lineHandler(line) };
        });
    }

    private static void EditCodeFile(string path, Func<string, IEnumerable<string>> lineHandler)
    {
        var bakPath = path + ".bak";
        if (File.Exists(bakPath))
        {
            File.Delete(bakPath);
        }

        File.Move(path, bakPath);

        using (var reader = File.OpenText(bakPath))
        using (var stream = File.Create(path))
        using (var writer = new StreamWriter(stream))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var outputs = lineHandler(line);
                foreach (var o in outputs)
                {
                    writer.WriteLine(o);
                }
            }
        }
    }
}

#endif
