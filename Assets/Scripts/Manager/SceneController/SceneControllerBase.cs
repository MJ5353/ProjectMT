using UnityEngine;

// ==================================================

// 씬마다 하나. 런타임 매니저를 만들고(없을 때만), 씬 Service의 수명 주기(등록·Tick·정리)와 UI 루트·메인 카메라를 맡는다.
// 씬마다 다른 것(어떤 Service를 둘지, 바로 실행했을 때의 상태)은 자식(BattleSceneController·LobbySceneController)이 정한다

public abstract class SceneControllerBase : MonoBehaviour
{
    [SerializeField] private RectTransform m_UIRoot = null;
    [SerializeField] private Camera m_MainCamera = null;
    [SerializeField] private ScreenBase m_Screen = null; // 이 씬의 화면(ScreenLobby·ScreenBattle). 자식이 GetScreen<T>()로 꺼내 쓴다

    // 이 씬을 바로 실행했을 때 시작할 게임 상태
    public abstract EGameState State { get; }

    // 버튼을 누르면 버섯이 튀는 연출(EffectService.UIClickBurst)을 켤지. 터지는 건 PlayClickBurst를 부르는 버튼뿐(타이틀·로딩 화면은 화면 전체 배경 버튼).
    // 로딩 화면은 떠나는 씬의 연출을 쓰므로, 로딩 화면에서 터지려면 떠나는 씬이 켜 둬야 한다
    public virtual bool UseClickBurst => false;
    private static bool s_IsQuitting; // 앱 종료 중. 매니저가 먼저 파괴될 수 있어 씬 정리를 건너뛴다

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

    private void Update() => GameManager.Instance.TickSceneServices(gameObject.scene, Time.deltaTime);

    // 씬이 바뀔 때만 정리한다. 앱이 꺼질 때는 정리할 필요가 없다
    private void OnDestroy()
    {
        if (s_IsQuitting)
            return;

        GameManager.Instance.ClearSceneServices(gameObject.scene);
        RootManager.Instance.ClearUIRoot(gameObject.scene);
        CameraManager.Instance.ClearMain(m_MainCamera);
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
