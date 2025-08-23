using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class CharacterLoader
{
    public static IEnumerator LoadAndSpawn(
        string charId,
        CharacterCatalog_SO catalog,
        Vector2 spawnPos,
        bool facingRight,
        Transform parent,
        Action<GameObject, CharacterProperty> onReady,
        Action<string> onError = null)
    {
        if (catalog == null || !catalog.TryGet(charId, out var e))
        { onError?.Invoke($"Catalog entry not found: {charId}"); yield break; }

        // 1) 애니메이션 선로딩(결정론엔 영향 없음, 어드레서블 캐시 예열)
        if (e.clipKeys != null && e.clipKeys.Count > 0)
        {
            var task = AnimationClipLibrary.Instance.LoadAssetsAsync(e.clipKeys);
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) { onError?.Invoke(task.Exception?.Message ?? "Anim load failed"); yield break; }
        }

        // 2) 프리팹 인스턴스
        AsyncOperationHandle<GameObject> h = Addressables.InstantiateAsync(e.prefabKey, parent);
        if (!h.IsValid()) { onError?.Invoke($"Invalid prefab key: {e.prefabKey}"); yield break; }
        yield return h;

        if (h.Status != AsyncOperationStatus.Succeeded || h.Result == null)
        { onError?.Invoke($"Failed to instantiate prefab: {e.prefabKey}"); yield break; }

        var go = h.Result;
        var prop = go.GetComponent<CharacterProperty>();
        var phys = go.GetComponent<PhysicsEntity>();
        var box = go.GetComponent<BoxPresetApplier>();
        var fsm = go.GetComponent<CharacterFSM>();

        if (!prop || !phys) { onError?.Invoke("Missing CharacterProperty/PhysicsEntity"); yield break; }

        // 3) 스폰 초기화
        CharacterWarp.Teleport(prop, spawnPos, facingRight, resetVelocity: true, refreshBoxes: true);
        fsm?.TransitionTo("Idle");
        box?.ClearAllBoxes(); // Idle pose에서 필요 박스는 각 시스템이 곧 셋업

        onReady?.Invoke(go, prop);
    }

    public static IEnumerator Despawn(GameObject go)
    {
        if (go == null) yield break;
        var h = Addressables.ReleaseInstance(go);
        yield return null; // 안전용 - 바로 null 되므로 한 프레임 양보
    }
}