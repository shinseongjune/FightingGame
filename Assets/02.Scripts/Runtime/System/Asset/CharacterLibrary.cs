using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CharacterLibrary : Singleton<CharacterLibrary>
{
    private readonly Dictionary<string, GameObject> prefabCache = new();

    public bool IsLoading { get; private set; }
    public float LoadProgress { get; private set; }

    public async Task PreloadAsync(List<string> characterKeys)
    {
        if (characterKeys == null || characterKeys.Count == 0) { IsLoading = false; LoadProgress = 1f; return; }
        IsLoading = true; LoadProgress = 0f;
        int done = 0; int total = characterKeys.Count;

        foreach (var key in characterKeys)
        {
            if (!prefabCache.ContainsKey(key))
            {
                var h = Addressables.LoadAssetAsync<GameObject>(key);
                while (!h.IsDone) { LoadProgress = (done + h.PercentComplete) / total; await Task.Yield(); }
                if (h.Status == AsyncOperationStatus.Succeeded) prefabCache[key] = h.Result;
                else Debug.LogWarning($"[CharacterLibrary] Load failed: {key}");
            }
            done++; LoadProgress = (float)done / total;
        }
        IsLoading = false; LoadProgress = 1f;
    }

    public GameObject Instantiate(string key, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (!prefabCache.TryGetValue(key, out var prefab))
        {
            Debug.LogError($"[CharacterLibrary] Not preloaded: {key}");
            return null;
        }
        return Object.Instantiate(prefab, pos, rot, parent);
    }

    public void UnloadAll()
    {
        foreach (var kv in prefabCache) Addressables.Release(kv.Value);
        prefabCache.Clear();
        IsLoading = false; LoadProgress = 0f;
    }
}
