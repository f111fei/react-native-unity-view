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

#if UNITY_WSA

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
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml;

public static class UWPPostBuild
{
    [PostProcessBuild]
    public static void OnPostBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.WSAPlayer)
        {
            return;
        }

        UpdateUnityProjectFiles(pathToBuiltProject);
    }

    /// <summary>
    /// Enumerates Unity output files and add necessary files into VS project file.
    /// It only add a reference entry into project file, without actually copy it.
    /// </summary>
    private static void UpdateUnityProjectFiles(string pathToBuiltProject)
    {
        string uwpScriptsDirRelative = "../../../unity/" + Path.Combine(Path.GetFileName(Directory.GetCurrentDirectory()), "Assets", "Scripts", "Editor", "UWP");
        string uwpScriptsDir = Path.Combine(Directory.GetCurrentDirectory() + "/Assets/Scripts/Editor/UWP");

        string projectName = Path.GetFileNameWithoutExtension(Directory.GetFiles(pathToBuiltProject).First(m => m.EndsWith(".sln")));
        string pathToUWPProject = Path.Combine(pathToBuiltProject, projectName);

        string csharpProjectFile = Path.Combine(pathToUWPProject, projectName + ".csproj");
        if (File.Exists(csharpProjectFile))
        {
            // Handle as .NET scripting backend
            //File.Copy(
            //    Path.Combine(uwpScriptsDir, "MainPage.xaml"),
            //    Path.Combine(pathToUWPProject, "MainPage.xaml"),
            //    true);
            //File.Copy(
            //    Path.Combine(uwpScriptsDir, "MainPage.xaml.cs"),
            //    Path.Combine(pathToUWPProject, "MainPage.xaml.cs"),
            //    true);

            string csharpProjectFile_Text = File.ReadAllText(csharpProjectFile);
            XNamespace defaultNS = "http://schemas.microsoft.com/developer/msbuild/2003";
            XDocument csharpProject = XDocument.Parse(csharpProjectFile_Text);
            XElement xamlRootParent = csharpProject.Root;

            foreach (var ct in xamlRootParent.Elements(defaultNS + "PropertyGroup").Select(m => m.Element(defaultNS + "OutputType")))
            {
                if (ct != null)
                {
                    ct.SetValue("Library");
                }
            }

            xamlRootParent.Add(
                new XElement(defaultNS + "ItemGroup",
                    new XElement(defaultNS + "Compile",
                        new XAttribute("Include", Path.Combine(uwpScriptsDirRelative, "UnityUtils.cs")),
                        new XElement(defaultNS + "Link", "UnityUtils.cs")),
                    new XElement(defaultNS + "Compile",
                        new XAttribute("Include", Path.Combine(uwpScriptsDirRelative, "UnityView.xaml.cs")),
                        new XElement(defaultNS + "Link", "UnityView.xaml.cs"),
                        new XElement(defaultNS + "DependentUpon", "UnityView.xaml")),
                    new XElement(defaultNS + "Page",
                        new XAttribute("Include", Path.Combine(uwpScriptsDirRelative, "UnityView.xaml")),
                        new XElement(defaultNS + "Link", "UnityView.xaml"),
                        new XElement(defaultNS + "Generator", "MSBuild:Compile"),
                        new XElement(defaultNS + "SubType", "Designer"))));
            csharpProjectFile_Text = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + csharpProject.ToString(SaveOptions.None);
            File.WriteAllText(csharpProjectFile, csharpProjectFile_Text);
            return;
        }

        string cppProjectFile = Path.Combine(pathToUWPProject, projectName + ".vcxproj");
        if (File.Exists(cppProjectFile))
        {
            // Handle as IL2CPP scripting backend
            string cppProjectFile_Text = File.ReadAllText(cppProjectFile);
            XNamespace defaultNS = "http://schemas.microsoft.com/developer/msbuild/2003";
            XDocument csharpProject = XDocument.Parse(cppProjectFile_Text);
            XElement xamlRootParent = csharpProject.Root;

            foreach (var ct in xamlRootParent.Elements(defaultNS + "PropertyGroup").Select(m => m?.Element(defaultNS + "ConfigurationType")).ToArray())
            {
                if (ct != null)
                {
                    ct.SetValue("DynamicLibrary");
                }
            }

            foreach (var ct in xamlRootParent
                .Elements(defaultNS + "ItemGroup")
                .SelectMany(m => m?
                    .Elements(defaultNS + "ClCompile")
                    .Union(m.Elements(defaultNS + "ClInclude"))
                    .Where(n => n?.Element(defaultNS + "DependentUpon") != null)
                    .Union(m.Elements(defaultNS + "ApplicationDefinition"))
                    .Union(m.Elements(defaultNS + "None"))
                    .Union(m.Elements(defaultNS + "AppxManifest"))
                    .Union(m.Elements(defaultNS + "Page")))
                .Union(
                    xamlRootParent.Elements(defaultNS + "PropertyGroup")
                    .SelectMany(m => m?.Elements(defaultNS + "PackageCertificateKeyFile")))
                .Where(m => m != null)
                .ToArray())
            {
                ct.Remove();
            }

            xamlRootParent.Elements(defaultNS + "PropertyGroup").Select(m => m?.Element(defaultNS + "RootNamespace")).FirstOrDefault()?.SetValue("UnityBridge");
            xamlRootParent.Elements(defaultNS + "PropertyGroup").FirstOrDefault()?.Add(
                new XElement(defaultNS + "Keyword", "WindowsRuntimeComponent"));
            xamlRootParent.Elements(defaultNS + "ItemDefinitionGroup")
                .SelectMany(m => m?.Elements(defaultNS + "ClCompile"))
                .Select(m => m?.Element(defaultNS + "PreprocessorDefinitions"))
                .Where(m => m != null)
                .ToList()
                .ForEach(m => m.SetValue("_WINRT_DLL;" + m.Value));

            xamlRootParent.Add(
                new XElement(defaultNS + "PropertyGroup",
                    new XElement(defaultNS + "IncludePath",
                        @"$(ProjectDir)" + uwpScriptsDirRelative + @";$(IncludePath)")));
            xamlRootParent.Add(
                new XElement(defaultNS + "ItemGroup",
                    new XElement(defaultNS + "ClCompile",
                        new XAttribute("Include", Path.Combine(uwpScriptsDirRelative, "UnityUtils.cpp"))),
                    new XElement(defaultNS + "ClCompile",
                        new XAttribute("Include", Path.Combine(uwpScriptsDirRelative, "UnityView.xaml.cpp"))),
                    new XElement(defaultNS + "ClInclude",
                        new XAttribute("Include", Path.Combine(uwpScriptsDirRelative, "UnityUtils.h"))),
                    new XElement(defaultNS + "ClInclude",
                        new XAttribute("Include", Path.Combine(uwpScriptsDirRelative, "UnityView.xaml.h")))));
            cppProjectFile_Text = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + csharpProject.ToString(SaveOptions.None);
            File.WriteAllText(cppProjectFile, cppProjectFile_Text);

            // Move files abound
            File.Move(cppProjectFile, Path.Combine(pathToUWPProject, "UnityBridge.vcxproj"));
            File.Move(cppProjectFile + ".filters", Path.Combine(pathToUWPProject, "UnityBridge.vcxproj.filters"));
            Directory.Move(pathToUWPProject, Path.Combine(Path.GetDirectoryName(pathToUWPProject), "UnityBridge"));
            pathToUWPProject = Path.Combine(Path.GetDirectoryName(pathToUWPProject), "UnityBridge");

            // Clear App.xaml.cpp/h
            File.Open(pathToUWPProject + "/App.xaml.h", FileMode.OpenOrCreate | FileMode.Truncate).Dispose();
            File.Open(pathToUWPProject + "/App.xaml.cpp", FileMode.OpenOrCreate | FileMode.Truncate).Dispose();

            UpdateNativeDependencyProject(pathToUWPProject, projectName);
            return;
        }
    }

    private static void UpdateNativeDependencyProject(string pathToUWPProject, string projectName)
    {
        string cppProjectFile = Path.Combine(pathToUWPProject, "Unity Data.vcxitems");
        if (File.Exists(cppProjectFile))
        {
            string cppProjectFile_Text = File.ReadAllText(cppProjectFile);
            XNamespace defaultNS = "http://schemas.microsoft.com/developer/msbuild/2003";
            XDocument csharpProject = XDocument.Parse(cppProjectFile_Text);
            XElement xamlRootParent = csharpProject.Root;

            xamlRootParent.Elements(defaultNS + "ItemGroup")
                .SelectMany(m => m.Elements(defaultNS + "None"))
                .ToList()
                .ForEach(m =>
                {
                    var includeAttr = m.Attribute("Include");
                    includeAttr?.SetValue(includeAttr.Value.Replace("$(OutDir)", "$(UnityWSAPlayerOutDir)"));
                    m.Name = defaultNS + "Content";
                    m.Elements().ToList().ForEach(e => e.Remove());
                    m.Add(new XElement(defaultNS + "CopyToOutputDirectory", "PreserveNewest"));
                });

            xamlRootParent.Elements(defaultNS + "Target")
                .SelectMany(m => m.Elements(defaultNS + "Copy"))
                .SelectMany(m => m.Attributes("SourceFiles").Union(m.Attributes("Condition")))
                .ToList()
                .ForEach(m => m.SetValue(m.Value.Replace("$(ProjectDir)", "$(MSBuildThisFileDirectory)")));

            cppProjectFile_Text = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + csharpProject.ToString(SaveOptions.None);
            File.WriteAllText(cppProjectFile, cppProjectFile_Text);
        }
    }
}

#endif
