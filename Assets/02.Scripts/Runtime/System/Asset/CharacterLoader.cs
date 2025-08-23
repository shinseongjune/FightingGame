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

        // 1) �ִϸ��̼� ���ε�(�����п� ���� ����, ��巹���� ĳ�� ����)
        if (e.clipKeys != null && e.clipKeys.Count > 0)
        {
            var task = AnimationClipLibrary.Instance.LoadAssetsAsync(e.clipKeys);
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) { onError?.Invoke(task.Exception?.Message ?? "Anim load failed"); yield break; }
        }

        // 2) ������ �ν��Ͻ�
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

        // 3) ���� �ʱ�ȭ
        CharacterWarp.Teleport(prop, spawnPos, facingRight, resetVelocity: true, refreshBoxes: true);
        fsm?.TransitionTo("Idle");
        box?.ClearAllBoxes(); // Idle pose���� �ʿ� �ڽ��� �� �ý����� �� �¾�

        onReady?.Invoke(go, prop);
    }

    public static IEnumerator Despawn(GameObject go)
    {
        if (go == null) yield break;
        var h = Addressables.ReleaseInstance(go);
        yield return null; // ������ - �ٷ� null �ǹǷ� �� ������ �纸
    }
}