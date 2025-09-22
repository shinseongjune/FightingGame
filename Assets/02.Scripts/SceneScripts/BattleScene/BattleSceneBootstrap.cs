using UnityEngine;

[DefaultExecutionOrder(-1000)]  // 씬 내 최우선
public sealed class BattleSceneBootstrap : MonoBehaviour
{
    private void Awake()
    {
        Ensure<TickMaster>("__TickMaster__");
        Ensure<PhysicsManager>("__PhysicsManager__");
        Ensure<BoxManager>("__BoxManager__");
    }

    private static T Ensure<T>(string goName) where T : Component
    {
        // 이미 DontDestroyOnLoad로 살아있으면 건들지 않음
        var inst = FindFirstObjectByType<T>();
        if (inst != null) return inst;

        var go = new GameObject(goName);
        DontDestroyOnLoad(go);
        return go.AddComponent<T>();
    }
}
