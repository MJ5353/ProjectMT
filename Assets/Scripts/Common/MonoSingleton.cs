using UnityEngine;

// ==================================================

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T s_Instance;

    public static T Instance => s_Instance.E_Alive();

    // ------------------------------------------------

    public void Init()
    {
        if (Instance != null)
            return;

        OnInit();
        s_Instance = (T)this;
    }

    public void Clear()
    {
        if (s_Instance != this)
            return;

        s_Instance = null;
        OnClear();
    }

    public static void ResetSingleton() => s_Instance = null;

    // 없으면 [Managers] 루트 아래에 만들어 등록한다. 런타임 내내 사는 매니저만 쓴다
    public static T Create()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject($"[{typeof(T).Name}]");
        go.transform.SetParent(RootManager.Create().ManagersRoot, false);

        T instance = go.AddComponent<T>();
        instance.Init();
        return instance;
    }

    // virtual ---

    protected virtual void OnInit() { }
    protected virtual void OnClear() { }
}
