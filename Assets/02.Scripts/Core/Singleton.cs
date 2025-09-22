using UnityEngine;

/// 안전한 싱글톤 베이스:
/// - isQuitting 은 오직 OnApplicationQuit 에서만 true
/// - OnDestroy 에서는 isQuitting 건드리지 않음
/// - Domain Reload OFF 대비: SubsystemRegistration 으로 static 초기화
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }
    private static bool isQuitting;

    // Domain Reload 비활성일 때도 플레이 시작 시 static 초기화를 보장
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticsOnPlayEnter()
    {
        Instance = null;
        isQuitting = false;
    }

    protected virtual void Awake()
    {
        // 중복 방지
        if (Instance != null && Instance != this as T)
        {
            // ⚠️ 여기서 isQuitting 을 건드리지 마세요!
            Destroy(gameObject);
            return;
        }
        Instance = this as T;

        if (ShouldPersistAcrossScenes()) DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnApplicationQuit()
    {
        isQuitting = true;
    }

    /// 필요 시: 인스턴스가 없으면 생성
    public static T Ensure()
    {
        if (isQuitting) return Instance; // 종료 중엔 새로 만들지 않음
        if (Instance == null)
        {
            var go = new GameObject(typeof(T).Name);
            Instance = go.AddComponent<T>();
            DontDestroyOnLoad(go);
        }
        return Instance;
    }

    /// 필요 시: 널체크
    public static bool TryGet(out T inst)
    {
        inst = Instance;
        return inst != null;
    }

    protected virtual bool ShouldPersistAcrossScenes() => true;
}
