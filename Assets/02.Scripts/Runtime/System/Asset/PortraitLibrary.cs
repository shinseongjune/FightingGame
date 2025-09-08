using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class PortraitLibrary : Singleton<PortraitLibrary>
{
    private readonly Dictionary<string, Sprite> cache = new();

    public bool IsLoading { get; private set; }
    public float LoadProgress { get; private set; }

    /// <summary>���� ���� ��������Ʈ�� �ϰ� �����ε�</summary>
    public async Task LoadAssetsAsync(List<string> keys)
    {
        if (keys == null || keys.Count == 0) return;

        IsLoading = true;
        LoadProgress = 0f;

        int loaded = 0;
        int total = keys.Count;

        foreach (var key in keys)
        {
            if (!string.IsNullOrEmpty(key) && !cache.ContainsKey(key))
            {
                var handle = Addressables.LoadAssetAsync<Sprite>(key);
                while (!handle.IsDone)
                {
                    LoadProgress = (loaded + handle.PercentComplete) / total;
                    await Task.Yield();
                }

                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                    cache[key] = handle.Result;
                else
                    Debug.LogWarning($"[PortraitLibrary] Load failed: {key}");
            }

            loaded++;
            LoadProgress = (float)loaded / total;
        }

        IsLoading = false;
        LoadProgress = 1f;
    }

    /// <summary>���� Ű�� ��� �ε�(��ĳ�� ��) �� ��ȯ</summary>
    public async Task<Sprite> LoadAndGetAsync(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (cache.TryGetValue(key, out var found)) return found;

        var handle = Addressables.LoadAssetAsync<Sprite>(key);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
        {
            cache[key] = handle.Result;
            return handle.Result;
        }

        Debug.LogWarning($"[PortraitLibrary] Load failed: {key}");
        return null;
    }

    public Sprite Get(string key)
    {
        cache.TryGetValue(key, out var s);
        return s;
    }

    public void UnloadAll()
    {
        foreach (var s in cache.Values)
            if (s != null) Addressables.Release(s);
        cache.Clear();
        IsLoading = false;
        LoadProgress = 0f;
    }
}
