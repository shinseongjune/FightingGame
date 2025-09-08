using System.Collections.Generic;
using UnityEngine;

public static class CatalogIds
{
    // Character 규칙
    public static string PortraitKey(string name) => $"Portrait/{name}";
    public static string IllustrationKey(string name) => $"Illust/{name}";
    public static string CharacterPrefabKey(string name, int colorIndex)
        => $"Characters/{name}/Prefab/{colorIndex:D2}";

    // Stage 규칙
    public static string StagePrefabKey(string name) => $"Stage/{name}";

    // 전체 열거
    public static IEnumerable<string> AllCharacterPrefabKeys(CharacterCatalog_SO catalog)
    {
        foreach (var c in catalog.characters)
        {
            for (int i = 0; i < Mathf.Max(1, c.colorVariants); i++)
                yield return CharacterPrefabKey(c.name, i);
        }
    }

    public static IEnumerable<string> AllPortraitKeys(CharacterCatalog_SO catalog)
    {
        foreach (var c in catalog.characters)
            yield return PortraitKey(c.name);
    }

    public static IEnumerable<string> AllIllustrationKeys(CharacterCatalog_SO catalog)
    {
        foreach (var c in catalog.characters)
            yield return IllustrationKey(c.name);
    }

    public static IEnumerable<string> AllStageKeys(StageCatalog_SO catalog)
    {
        foreach (var s in catalog.stages)
            yield return StagePrefabKey(s.name);
    }
}
