using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public static class ScenePreprocessor
{
    [PostProcessScene(0)]
    public static void OnPostprocessScene()
    {
        if (BuildPipeline.isBuildingPlayer && !Application.isPlaying && Build.CurrentGroup.HasValue)
        {
            var isUnityExport = PlayerSettings.GetScriptingDefineSymbolsForGroup(Build.CurrentGroup.Value)
                .Split(';')
                .Select(m => m.Trim())
                .Any(m => m == "UNITY_EXPORT");

            if (isUnityExport)
            {
                Debug.Log("Building Unity Export");

                foreach (GameObject obj in GameObject.FindGameObjectsWithTag("StandaloneOnly"))
                {
                    if (obj && !AssetDatabase.Contains(obj))
                    {
                        Object.DestroyImmediate(obj);
                    }
                }
            }
            else
            {
                Debug.Log("Building Standalone");
            }
        }
    }
}
