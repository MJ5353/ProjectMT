using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T s_Instance;

    public static T Instance => s_Instance.E_Alive();

    // ==================================================

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

    // virtual---

    protected virtual void OnInit() { }
    protected virtual void OnClear() { }
}
