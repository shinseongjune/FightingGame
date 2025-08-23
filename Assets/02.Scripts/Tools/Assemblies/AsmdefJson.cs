#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable] class AsmdefJson { public string name; public string[] references; }

public static class AsmdefScan
{
    [MenuItem("Tools/Assemblies/Find Asmdefs Referencing GG.*")]
    static void FindRefsToGG()
    {
        var guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");
        var hits = new List<string>();
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var text = System.IO.File.ReadAllText(path);
            var data = new AsmdefJson();
            EditorJsonUtility.FromJsonOverwrite(text, data);
            if (data?.references == null) continue;
            foreach (var r in data.references)
                if (!string.IsNullOrEmpty(r) && r.Contains("GG."))
                    hits.Add($"{System.IO.Path.GetFileNameWithoutExtension(path)} -> {r}");
        }
        Debug.Log(hits.Count == 0 ? "No asmdef references to GG.*" : string.Join("\n", hits));
    }
}
#endif
