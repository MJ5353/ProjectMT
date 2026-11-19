using UnityEngine;

// ==================================================

// 씬마다 하나. 런타임 매니저를 만들고(없을 때만), 씬 Service의 수명 주기(등록·Tick·정리)와 UI 루트·메인 카메라를 맡는다.
// 씬마다 다른 것(어떤 Service를 둘지, 바로 실행했을 때의 상태)은 자식(BattleSceneController·LobbySceneController)이 정한다

public abstract class SceneControllerBase : MonoBehaviour
{
    [SerializeField] private RectTransform m_UIRoot = null;
    [SerializeField] private Camera m_MainCamera = null;

    // 이 씬을 바로 실행했을 때 시작할 게임 상태
    public abstract EGameState State { get; }

    // ------------------------------------------------

    private void Awake()
    {
        RootManager rootM = RootManager.Create();
        ResourceManager.Create(); // GameManager가 Init에서 DB를 로드하므로 먼저
        GameManager.Create();
        TimeManager.Create();
        InputManager.Create();
        CameraManager.Create();
        PlayerDataManager.Create();

        rootM.SetUIRoot(gameObject.scene, m_UIRoot);
        CameraManager.Instance.SetMain(m_MainCamera);
    }

    private void Start() => GameManager.Instance.InitSceneServices(gameObject.scene, this);

    private void Update() => GameManager.Instance?.TickSceneServices(gameObject.scene, Time.deltaTime);

    private void OnDestroy()
    {
        GameManager.Instance?.ClearSceneServices(gameObject.scene);
        RootManager.Instance?.ClearUIRoot(gameObject.scene);
        CameraManager.Instance?.ClearMain(m_MainCamera);
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
            PlayerDataManager.Instance?.RunSave();
    }

    private void OnApplicationQuit() => PlayerDataManager.Instance?.RunSave();

    // 이 씬 전용 Service를 GameManager에 등록한다. 공용(팝업)은 GameManager가 넣는다
    public virtual void RegisterServices(GameManager gameM) { }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSingletons()
    {
        RootManager.ResetSingleton();
        ResourceManager.ResetSingleton();
        GameManager.ResetSingleton();
        TimeManager.ResetSingleton();
        InputManager.ResetSingleton();
        CameraManager.ResetSingleton();
        PlayerDataManager.ResetSingleton();
    }

#if UNITY_EDITOR
    public void EditorSetup(RectTransform uiRoot, Camera mainCamera)
    {
        m_UIRoot = uiRoot;
        m_MainCamera = mainCamera;
    }
#endif
}
