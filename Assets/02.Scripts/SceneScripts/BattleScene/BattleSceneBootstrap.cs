using UnityEngine;

[DefaultExecutionOrder(-1000)]  // �� �� �ֿ켱
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
        // �̹� DontDestroyOnLoad�� ��������� �ǵ��� ����
        var inst = FindFirstObjectByType<T>();
        if (inst != null) return inst;

        var go = new GameObject(goName);
        DontDestroyOnLoad(go);
        return go.AddComponent<T>();
    }
}
