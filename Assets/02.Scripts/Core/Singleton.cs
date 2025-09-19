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

            if (_instance) return _instance;

            _instance = FindAnyObjectByType<T>();
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance && _instance != (T)(object)this) { Destroy(gameObject); return; }
        _instance = (T)(object)this;

        if (ShouldPersistAcrossScenes())
            DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// ���� �Ѿ� �������� ����. �ʿ�� �������̵�.
    /// </summary>
    protected virtual bool ShouldPersistAcrossScenes() => true;

    protected virtual void OnDestroy()
    {
        if (ReferenceEquals(_instance, this)) _instance = null;
        _quitting = true; // �ı� �߿� Instance�� null�� ��ȯ
    }

    protected virtual void OnApplicationQuit()
    {
        _quitting = true;
    }
}