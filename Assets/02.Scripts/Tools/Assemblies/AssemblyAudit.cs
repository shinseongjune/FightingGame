#if UNITY_EDITOR
using UnityEditor;
using System.Linq;

public static class AssemblyAudit
{
    [MenuItem("Tools/Assemblies/List Scripts by Assembly")]
    static void List()
    {
        var guids = AssetDatabase.FindAssets("t:MonoScript");
        var groups = guids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Select(p => (p, s: AssetDatabase.LoadAssetAtPath<MonoScript>(p)))
            .Where(x => x.s != null && x.s.GetClass() != null)
            .GroupBy(x => x.s.GetClass().Assembly.GetName().Name)
            .OrderBy(g => g.Key);

        foreach (var g in groups)
            UnityEngine.Debug.Log($"Assembly: {g.Key}\n" + string.Join("\n", g.Select(x => "  " + x.p)));
    }
}
#endif
