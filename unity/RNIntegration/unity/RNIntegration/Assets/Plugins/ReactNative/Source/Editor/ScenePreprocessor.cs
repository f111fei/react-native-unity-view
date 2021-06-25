using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ScenePreprocessor
{
    [PostProcessScene(0)]
    public static void OnPostprocessScene()
    {
        if (BuildPipeline.isBuildingPlayer && !Application.isPlaying && Build.CurrentGroup.HasValue)
        {
            Debug.Log("Preprocessing scene: " + SceneManager.GetActiveScene().name);

            if (!IsIOSSimulator())
            {
                RemoveObjectsWithTag("SimulatorOnly");
            }

            if (IsUnityExport())
            {
                RemoveObjectsWithTag("StandaloneOnly");
            }
            else
            {
                Debug.Log("Building Standalone");
            }
        }
    }

    private static bool IsIOSSimulator()
        => Build.CurrentGroup == BuildTargetGroup.iOS
        && PlayerSettings.iOS.sdkVersion == iOSSdkVersion.SimulatorSDK;

    private static bool IsUnityExport()
        => PlayerSettings.GetScriptingDefineSymbolsForGroup(Build.CurrentGroup.Value)
                         .Split(';')
                         .Select(m => m.Trim())
                         .Any(m => m == "UNITY_EXPORT");

    private static void RemoveObjectsWithTag(string tag)
    {
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag(tag))
        {
            if (obj && !AssetDatabase.Contains(obj))
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}
