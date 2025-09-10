using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class StageLibrary : Singleton<StageLibrary>
{
    private readonly Dictionary<string, GameObject> prefabCache = new();

    public bool IsLoading { get; private set; }
    public float LoadProgress { get; private set; }

    public async Task PreloadAsync(List<string> stageKeys)
    {
        if (stageKeys == null || stageKeys.Count == 0) { IsLoading = false; LoadProgress = 1f; return; }
        IsLoading = true; LoadProgress = 0f;
        int done = 0; int total = stageKeys.Count;

        foreach (var key in stageKeys)
        {
            if (!prefabCache.ContainsKey(key))
            {
                var h = Addressables.LoadAssetAsync<GameObject>(key);
                while (!h.IsDone) { LoadProgress = (done + h.PercentComplete) / total; await Task.Yield(); }
                if (h.Status == AsyncOperationStatus.Succeeded) prefabCache[key] = h.Result;
                else Debug.LogWarning($"[StageLibrary] Load failed: {key}");
            }
            done++; LoadProgress = (float)done / total;
        }
        IsLoading = false; LoadProgress = 1f;
    }

    public GameObject Instantiate(string key, Transform parent = null)
    {
        if (!prefabCache.TryGetValue(key, out var prefab))
        {
            Debug.LogError($"[StageLibrary] Not preloaded: {key}");
            return null;
        }
        return Object.Instantiate(prefab, parent);
    }

    public void UnloadAll()
    {
        foreach (var kv in prefabCache) Addressables.Release(kv.Value);
        prefabCache.Clear();
        IsLoading = false; LoadProgress = 0f;
    }
}
