using UnityEngine;

// ==================================================

// [Managers] 루트 오브젝트. 씬이 바뀌어도 남도록 DontDestroyOnLoad를 걸고, 다른 매니저(MonoSingleton.Create)가 이 아래에 붙는다.
// 씬 UI 루트는 UIManager가 맡는다

public sealed class RootManager : MonoSingleton<RootManager>
{
    private const string RootName = "[Managers]";

    public Transform ManagersRoot => transform;

    // ------------------------------------------------

    public new static RootManager Create()
    {
        if (Instance != null)
            return Instance;

        GameObject root = new(RootName);
        DontDestroyOnLoad(root);
        
        RootManager manager = root.AddComponent<RootManager>();
        manager.Init();
        
        return manager;
    }
}
