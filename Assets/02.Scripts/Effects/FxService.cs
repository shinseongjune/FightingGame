using System.Collections.Generic;
using UnityEngine;

public sealed class FxService : MonoBehaviour
{
    [SerializeField] private EffectLibrary_SO library;

    private readonly Dictionary<string, Queue<FxInstance>> pools = new();
    private readonly Dictionary<string, Transform> poolRoots = new();

    public static FxService Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (library != null) library.BuildMap();
        PrewarmAll();
    }

    private void PrewarmAll()
    {
        if (library == null) return;
        foreach (var e in library.entries)
        {
            if (!e || !e.prefab) continue;
            GetRoot(e.key);
            for (int i = 0; i < Mathf.Max(0, e.prewarm); i++)
                ReturnToPool(e.key, CreateInstance(e));
        }
    }

    private Transform GetRoot(string key)
    {
        if (!poolRoots.TryGetValue(key, out var root))
        {
            var go = new GameObject($"FXPool_{key}");
            go.transform.SetParent(transform);
            root = go.transform;
            poolRoots[key] = root;
        }
        return root;
    }

    private FxInstance CreateInstance(EffectEntry_SO e)
    {
        var go = Instantiate(e.prefab, GetRoot(e.key));
        if (!go.TryGetComponent<FxInstance>(out var fx))
            fx = go.AddComponent<FxInstance>();
        go.SetActive(false);
        return fx;
    }

    private FxInstance Rent(string key, out EffectEntry_SO entry)
    {
        entry = null;
        if (library == null || !library.TryGet(key, out entry) || entry.prefab == null) return null;

        if (!pools.TryGetValue(key, out var q))
            pools[key] = q = new Queue<FxInstance>();

        if (q.Count == 0)
            return CreateInstance(entry);

        return q.Dequeue();
    }

    private void ReturnToPool(string key, FxInstance fx)
    {
        if (!pools.TryGetValue(key, out var q))
            pools[key] = q = new Queue<FxInstance>();
        fx.transform.SetParent(GetRoot(key), false);
        q.Enqueue(fx);
    }

    // ===== Public API =====
    public void Spawn(string key, Vector3 worldPos, Quaternion? rot = null, float lifetimeOverride = -1f, Vector3? scale = null, Transform follow = null)
    {
        var fx = Rent(key, out var entry);
        if (fx == null) return;

        if (follow == null)
        {
            fx.transform.SetParent(GetRoot(key), false);
            fx.transform.SetPositionAndRotation(worldPos, rot ?? Quaternion.identity);
        }
        else
        {
            fx.transform.SetParent(follow, false);
            fx.transform.localPosition = Vector3.zero;
            fx.transform.localRotation = rot ?? Quaternion.identity;
        }
        if (scale.HasValue) fx.transform.localScale = scale.Value;

        float life = lifetimeOverride >= 0f ? lifetimeOverride : entry.defaultLifetime;
        fx.Play(life, _ => ReturnToPool(key, fx));
    }

    // 기존 Spawn은 그대로 두되, 핸들을 돌려주는 버전과 Attach 버전 추가
    public FxInstance SpawnPersistent(string key, Vector3 pos, Quaternion? rot = null, Transform parent = null)
    {
        if (!library.TryGet(key, out var entry)) return null;

        var fx = GetOrCreateInstance(key);
        if (parent != null) fx.transform.SetParent(parent, worldPositionStays: false);
        fx.transform.position = parent ? Vector3.zero : pos;
        fx.transform.rotation = rot ?? Quaternion.identity;
        fx.transform.localScale = Vector3.one;

        // lifetime -1f => 수동 종료 전까지 유지
        fx.Play(-1f, _ => ReturnToPool(key, fx));
        return fx;
    }

    public FxInstance SpawnAttached(string key, Transform parent, Vector3 localOffset, Quaternion? localRot = null)
    {
        var inst = SpawnPersistent(key, Vector3.zero, Quaternion.identity, parent);
        if (inst == null) return null;
        inst.transform.localPosition = localOffset;
        inst.transform.localRotation = localRot ?? Quaternion.identity;
        return inst;
    }

    public FxInstance SpawnAt(Transform target, string key, Vector3 localOffset, float lifetimeOverride = -1f)
    {
        if (!library.TryGet(key, out var entry)) return null;

        var fx = GetOrCreateInstance(key);
        fx.transform.SetParent(target, worldPositionStays: false);
        fx.transform.localPosition = localOffset;
        fx.transform.localRotation = Quaternion.identity;
        fx.transform.localScale = Vector3.one;

        float life = lifetimeOverride >= 0f ? lifetimeOverride : entry.defaultLifetime;
        fx.Play(life, _ => ReturnToPool(key, fx));
        return fx;
    }

    private FxInstance GetOrCreateInstance(string key)
    {
        if (!library.TryGet(key, out var entry)) return null;
        if (!pools.TryGetValue(key, out var q))
        {
            q = new Queue<FxInstance>(entry.prewarm);
            pools[key] = q;

            // 풀 루트
            var root = new GameObject($"FXPool_{key}").transform;
            root.SetParent(transform, false);
            poolRoots[key] = root;

            // 프리웜
            for (int i = 0; i < entry.prewarm; i++)
            {
                var go = Instantiate(entry.prefab, root);
                go.SetActive(false);
                q.Enqueue(go.GetComponent<FxInstance>() ?? go.AddComponent<FxInstance>());
            }
        }

        FxInstance fx;
        if (q.Count > 0) fx = q.Dequeue();
        else
        {
            var go = Instantiate(entry.prefab, poolRoots[key]);
            fx = go.GetComponent<FxInstance>() ?? go.AddComponent<FxInstance>();
        }
        return fx;
    }
}