using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T s_Instance;
    private static bool s_IsQuitting;

    public static T Instance => s_IsQuitting ? null : s_Instance;

    // ==================================================

    protected void Awake()
    {
        if (s_IsQuitting || (s_Instance != null && s_Instance != this))
            Destroy(this);
    }

    protected void OnDestroy()
    {
        if (s_Instance == this)
            s_Instance = null;
    }

    protected void OnApplicationQuit()
    {
        if (s_Instance != this)
            return;

        s_IsQuitting = true;
        OnQuit();
    }

    internal static void InitializeSingleton()
    {
        if (s_IsQuitting || s_Instance != null)
            return;

        T instance = FindFirstObjectByType<T>();
        if (instance == null)
            instance = new GameObject($"[{typeof(T).Name}]").AddComponent<T>();

        instance.Init();
        DontDestroyOnLoad(instance.gameObject);
        s_Instance = instance;
    }

    // 도메인 리로드를 끈 경우에도 새 플레이에서 생성할 수 있게 한다
    protected static void ResetSingleton()
    {
        s_Instance = null;
        s_IsQuitting = false;
    }

    // virtual ----

    protected virtual void Init() { }
    protected virtual void OnQuit() { }
}
