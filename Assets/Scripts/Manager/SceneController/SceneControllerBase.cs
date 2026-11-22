using UnityEngine;

// ==================================================

// 씬마다 하나. 런타임 매니저를 만들고(없을 때만), 씬 Service의 수명 주기(등록·Tick·정리)와 UI 루트(UIManager)·메인 카메라 연결을 맡는다.
// 씬마다 다른 것(어떤 Service를 둘지, 바로 실행했을 때의 상태)은 자식(BattleSceneController·LobbySceneController)이 정한다

public abstract class SceneControllerBase : MonoBehaviour
{
    [SerializeField] private RectTransform m_UIRoot = null;
    [SerializeField] private Camera m_MainCamera = null;
    [SerializeField] private ScreenBase m_Screen = null; // 이 씬의 화면(ScreenLobby·ScreenBattle). 자식이 GetScreen<T>()로 꺼내 쓴다

    // 이 씬을 바로 실행했을 때 시작할 게임 상태
    public abstract EGameState State { get; }

    private static bool s_IsQuitting; // 앱 종료 중. 매니저가 먼저 파괴될 수 있어 씬 정리를 건너뛴다

    // ------------------------------------------------

    private void Awake()
    {
        RootManager.Create();
        ResourceManager.Create(); // GameManager가 Init에서 DB를 로드하므로 먼저
        GameManager.Create();
        UIManager.Create(); // 클릭 연출이 Init에서 DB를 읽으므로 GameManager 뒤
        TimeManager.Create();
        InputManager.Create();
        CameraManager.Create();
        PlayerDataManager.Create();

        UIManager.Instance.SetUIRoot(gameObject.scene, m_UIRoot);
        CameraManager.Instance.SetMain(m_MainCamera);
    }

    private void Start() => GameManager.Instance.InitSceneServices(gameObject.scene, this);

    private void Update()
    {
        UIManager.Instance.Tick();
        GameManager.Instance.TickSceneServices(gameObject.scene, Time.deltaTime);
    }

    // 씬이 바뀔 때만 정리한다. 앱이 꺼질 때는 정리할 필요가 없다
    private void OnDestroy()
    {
        if (s_IsQuitting)
            return;

        GameManager.Instance.ClearSceneServices(gameObject.scene);
        UIManager.Instance.ClearUIRoot(gameObject.scene);
        CameraManager.Instance.ClearMain(m_MainCamera);
    }

    // 앱이 내려가거나 포커스를 잃으면 게임 시간을 멈춘다. 다른 이유(확인 팝업·시장)와 따로 걸고 푼다
    // FlowManager가 생기면 그쪽으로 넘긴다(Flow 묶음 2)
    private void OnApplicationPause(bool isPaused)
    {
        if (s_IsQuitting)
            return;

        TimeManager.Instance.SetPause(EPauseReason.App, isPaused);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (s_IsQuitting)
            return;

        TimeManager.Instance.SetPause(EPauseReason.Focus, hasFocus == false);
    }

    // 이 씬 전용 Service를 GameManager에 등록한다. 공용(팝업)은 GameManager가 넣는다
    public virtual void RegisterServices(GameManager gameM) { }

    protected T GetScreen<T>() where T : ScreenBase => m_Screen as T;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSingletons()
    {
        s_IsQuitting = false;
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;

        RootManager.ResetSingleton();
        ResourceManager.ResetSingleton();
        GameManager.ResetSingleton();
        UIManager.ResetSingleton();
        TimeManager.ResetSingleton();
        InputManager.ResetSingleton();
        CameraManager.ResetSingleton();
        PlayerDataManager.ResetSingleton();
    }

    private static void OnQuitting() => s_IsQuitting = true;

#if UNITY_EDITOR
    public void EditorSetup(RectTransform uiRoot, Camera mainCamera, ScreenBase screen)
    {
        m_UIRoot = uiRoot;
        m_MainCamera = mainCamera;
        m_Screen = screen;
    }
#endif
}
