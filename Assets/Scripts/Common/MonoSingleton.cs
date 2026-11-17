using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T s_Instance;
    private static bool s_IsQuitting;

    public static T Instance
    {
        get
        {
            if (s_IsQuitting || Application.isPlaying == false)
                return null;

            if (s_Instance == null)
            {
                T instance = FindFirstObjectByType<T>();
                if (instance == null)
                    new GameObject($"[{typeof(T).Name}]").AddComponent<T>();
                else
                    instance.Register();
            }

            return s_Instance;
        }
    }

    // ==================================================

    protected void Awake() => Register();

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

    // 도메인 리로드를 끈 경우에도 새 플레이에서 생성할 수 있게 한다
    protected static void ResetSingleton()
    {
        s_Instance = null;
        s_IsQuitting = false;
    }

    private void Register()
    {
        if (s_Instance == this || s_Instance == null)
            return;
        
        if (s_IsQuitting)
        {
            Destroy(this);
            return;
        }

        s_Instance = (T)this;
        
        DontDestroyOnLoad(gameObject);
        Init();
    }

    // virtual ----

    protected virtual void Init() { }
    protected virtual void OnQuit() { }
}
