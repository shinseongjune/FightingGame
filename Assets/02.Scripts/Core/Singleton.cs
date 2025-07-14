using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool _quitting = false;

    public static T Instance
    {
        get
        {
            if (_quitting) return null;

            if (_instance == null)
            {
                _instance = FindAnyObjectByType<T>();

                if (_instance == null)
                {
                    GameObject go = new GameObject(typeof(T).Name);
                    _instance = go.AddComponent<T>();
                }
            }

            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;

        if (ShouldPersistAcrossScenes())
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// 씬을 넘어 유지할지 여부. 필요시 오버라이드.
    /// </summary>
    protected virtual bool ShouldPersistAcrossScenes() => true;

    protected virtual void OnApplicationQuit()
    {
        _quitting = true;
    }
}