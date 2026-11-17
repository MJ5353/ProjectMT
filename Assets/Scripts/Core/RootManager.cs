using UnityEngine;
using UnityEngine.SceneManagement;

// ==================================================

public sealed class RootManager : MonoSingleton<RootManager>
{
    private const string RootName = "[Managers]";
    private Scene m_UIScene;

    public Transform ManagersRoot => transform;
    public RectTransform UIRoot { get; private set; }

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

    public void SetUIRoot(Scene scene, RectTransform root)
    {
        m_UIScene = scene;
        UIRoot = root;
    }

    public void ClearUIRoot(Scene scene)
    {
        if (m_UIScene != scene)
            return;

        UIRoot = null;
        m_UIScene = default;
    }
}
