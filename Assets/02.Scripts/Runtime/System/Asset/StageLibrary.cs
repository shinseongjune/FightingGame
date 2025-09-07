using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class StageLibrary : Singleton<StageLibrary>
{
    private GameObject _currentStagePrefabInstance;
    private SceneInstance? _loadedScene; // 씬 로드시

    // 프리팹 캐시(선택)
    private readonly Dictionary<string, GameObject> _prefabCache = new();

    public bool IsSceneKey(string stageId, out string sceneKey)
    {
        sceneKey = null;
        if (string.IsNullOrEmpty(stageId)) return false;
        if (stageId.StartsWith("scene:"))
        {
            sceneKey = stageId.Substring("scene:".Length);
            return true;
        }
        return false;
    }

    public bool IsPrefabKey(string stageId, out string prefabKey)
    {
        prefabKey = null;
        if (string.IsNullOrEmpty(stageId)) return false;
        if (stageId.StartsWith("prefab:"))
        {
            prefabKey = stageId.Substring("prefab:".Length);
            return true;
        }
        return false;
    }

    public async Task<bool> LoadAsync(string stageId, Transform parent = null)
    {
        await UnloadAsync();

        if (IsSceneKey(stageId, out var sceneKey))
        {
            var handle = Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Additive, true);
            await handle.Task;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _loadedScene = handle.Result;
                SceneManager.SetActiveScene(handle.Result.Scene);
                return true;
            }
            Debug.LogError($"[StageLibrary] Scene load failed: {sceneKey}");
            return false;
        }
        else if (IsPrefabKey(stageId, out var prefabKey))
        {
            GameObject prefab;
            if (!_prefabCache.TryGetValue(prefabKey, out prefab))
            {
                var h = Addressables.LoadAssetAsync<GameObject>(prefabKey);
                await h.Task;
                if (h.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[StageLibrary] Prefab load failed: {prefabKey}");
                    return false;
                }
                prefab = h.Result;
                _prefabCache[prefabKey] = prefab;
            }

            _currentStagePrefabInstance = Instantiate(prefab, parent);
            return true;
        }
        else
        {
            // 기본: 씬 키로 간주
            var handle = Addressables.LoadSceneAsync(stageId, LoadSceneMode.Additive, true);
            await handle.Task;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _loadedScene = handle.Result;
                SceneManager.SetActiveScene(handle.Result.Scene);
                return true;
            }
            Debug.LogError($"[StageLibrary] Unknown stageId format: {stageId}");
            return false;
        }
    }

    public async Task UnloadAsync()
    {
        // 프리팹 스테이지 해제
        if (_currentStagePrefabInstance != null)
        {
            Destroy(_currentStagePrefabInstance);
            _currentStagePrefabInstance = null;
        }

        // 씬 스테이지 해제
        if (_loadedScene.HasValue)
        {
            var inst = _loadedScene.Value;
            var h = Addressables.UnloadSceneAsync(inst);
            await h.Task;
            _loadedScene = null;
        }
    }

    public void ReleaseAllPrefabs()
    {
        foreach (var kv in _prefabCache)
            Addressables.Release(kv.Value);
        _prefabCache.Clear();
    }
}
