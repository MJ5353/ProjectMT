using UnityEngine;

[DefaultExecutionOrder(-1100)]
public sealed class SceneController : MonoBehaviour
{
    [SerializeField] private RectTransform m_UIRoot = null;
    [SerializeField] private RegionDef m_DefaultRegion = null;

    // ------------------------------------------------

    private void Awake()
    {
        RootManager rootM = RootManager.Create();
        GameManager.Create();
        TimeManager.Create();
        ResourceManager.Create();
        PlayerDataManager.Create();
        
        rootM.SetUIRoot(gameObject.scene, m_UIRoot);
    }

    private void Start() => GameManager.Instance.InitSceneServices(gameObject.scene, m_DefaultRegion);

    private void Update() => GameManager.Instance?.TickSceneServices(gameObject.scene, Time.deltaTime);

    private void OnDestroy()
    {
        GameManager.Instance?.ClearSceneServices(gameObject.scene);
        RootManager.Instance?.ClearUIRoot(gameObject.scene);
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
            PlayerDataManager.Instance?.RunSave();
    }

    private void OnApplicationQuit() => PlayerDataManager.Instance?.RunSave();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSingletons()
    {
        RootManager.ResetSingleton();
        GameManager.ResetSingleton();
        TimeManager.ResetSingleton();
        ResourceManager.ResetSingleton();
        PlayerDataManager.ResetSingleton();
    }

#if UNITY_EDITOR
    public void EditorSetup(RectTransform uiRoot, RegionDef defaultRegion = null)
    {
        m_UIRoot = uiRoot;
        m_DefaultRegion = defaultRegion;
    }
#endif
}
