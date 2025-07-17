using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AnimationClipLibrary : Singleton<AnimationClipLibrary>
{
    private readonly Dictionary<string, AnimationClip> clipCache = new();

    public bool IsLoading { get; private set; } = false;
    public float LoadProgress { get; private set; } = 0f;

    public async Task LoadAssetsAsync(List<string> keys)
    {
        IsLoading = true;
        LoadProgress = 0f;

        int loadedCount = 0;
        int totalCount = keys.Count;

        foreach (var key in keys)
        {
            if (clipCache.ContainsKey(key))
            {
                loadedCount++;
                LoadProgress = (float)loadedCount / totalCount;
                continue;
            }

            var handle = Addressables.LoadAssetAsync<AnimationClip>(key);
            while (!handle.IsDone)
            {
                // handle.PercentComplete는 0~1 범위
                LoadProgress = (loadedCount + handle.PercentComplete) / totalCount;
                await Task.Yield(); // 프레임 대기
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                clipCache[key] = handle.Result;
            }
            else
            {
                Debug.LogWarning($"[AnimClip] Load Failed: {key}");
            }

            loadedCount++;
            LoadProgress = (float)loadedCount / totalCount;
        }

        IsLoading = false;
        LoadProgress = 1f;
    }

    public void UnloadAll()
    {
        foreach (var clip in clipCache.Values)
        {
            Addressables.Release(clip);
        }
        clipCache.Clear();
        IsLoading = false;
        LoadProgress = 0f;
    }

    public AnimationClip Get(string key)
    {
        clipCache.TryGetValue(key, out var clip);
        return clip;
    }
}
