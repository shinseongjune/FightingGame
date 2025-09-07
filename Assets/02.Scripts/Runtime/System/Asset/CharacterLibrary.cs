using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CharacterLibrary : Singleton<CharacterLibrary>
{
    // 캐시: key → 프리팹
    private readonly Dictionary<string, GameObject> _prefabCache = new();
    // 로딩 중 태스크 캐싱(중복 로드 방지)
    private readonly Dictionary<string, Task<GameObject>> _loading = new();

    public async Task<GameObject> LoadPrefabAsync(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        if (_prefabCache.TryGetValue(key, out var cached))
            return cached;

        if (_loading.TryGetValue(key, out var pending))
            return await pending;

        var tcs = new TaskCompletionSource<GameObject>();
        _loading[key] = tcs.Task;

        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key);
        await handle.Task;

        _loading.Remove(key);

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            var prefab = handle.Result;
            _prefabCache[key] = prefab;
            tcs.SetResult(prefab);
            return prefab;
        }
        else
        {
            Debug.LogError($"[CharacterLibrary] Load failed: {key}");
            tcs.SetResult(null);
            return null;
        }
    }

    public void Release(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (_prefabCache.Remove(key, out var prefab))
        {
            Addressables.Release(prefab);
        }
    }

    public void ReleaseAll()
    {
        foreach (var kv in _prefabCache)
            Addressables.Release(kv.Value);
        _prefabCache.Clear();
    }
}
