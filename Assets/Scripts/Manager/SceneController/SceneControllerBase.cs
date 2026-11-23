using UnityEngine;

// ==================================================

// 씬마다 하나. 런타임 매니저를 만들고(없을 때만), 이 씬의 Flow를 FlowManager에 넘겨 시작·Tick·정리하고, UI 루트(UIManager)·메인 카메라를 연결한다.
// 씬마다 다른 것(어떤 Flow를 쓸지)은 자식(Title·Lobby·BattleSceneController)이 CreateFlow로 정한다

public abstract class SceneControllerBase : MonoBehaviour
{
    [SerializeField] private RectTransform m_UIRoot = null;
    [SerializeField] private Camera m_MainCamera = null;
    [SerializeField] private ScreenBase m_Screen = null; // 이 씬의 화면(ScreenLobby·ScreenBattle). 자식이 GetScreen<T>()로 꺼내 쓴다

    private static bool s_IsQuitting; // 앱 종료 중. 매니저가 먼저 파괴될 수 있어 씬 정리를 건너뛴다

    // ------------------------------------------------

    private void Awake()
    {
        RootManager.Create();
        ResourceManager.Create(); // GameManager가 Init에서 DB를 로드하므로 먼저
        GameManager.Create();
        UIManager.Create(); // 클릭 연출이 Init에서 DB를 읽으므로 GameManager 뒤
        FlowManager.Create();
        TimeManager.Create();
        InputManager.Create();
        CameraManager.Create();
        PlayerDataManager.Create();

        UIManager.Instance.SetUIRoot(gameObject.scene, m_UIRoot);
        CameraManager.Instance.SetMain(m_MainCamera);
    }

    // 다른 오브젝트의 Awake·OnEnable이 끝난 뒤 Flow를 시작한다(화면이 OnEnable에서 이벤트를 먼저 구독한다)
    private void Start()
    {
        GameManager.Instance.ApplySceneCursor(gameObject.scene.name);
        FlowManager.Instance.Begin(CreateFlow());
    }

    private void Update()
    {
        UIManager.Instance.Tick();
        FlowManager.Instance.Tick(Time.deltaTime);
    }

    // 씬이 바뀔 때만 정리한다. 앱이 꺼질 때는 정리할 필요가 없다
    // Flow는 GameManager가 로드 전에 이미 끝냈다. 다른 길로 씬이 내려갔을 때만 여기서 끝난다
    private void OnDestroy()
    {
        if (s_IsQuitting)
            return;

        FlowManager.Instance.End();
        UIManager.Instance.ClearUIRoot(gameObject.scene);
        CameraManager.Instance.ClearMain(m_MainCamera);
    }

    // 앱이 내려가거나 포커스를 잃으면 게임 시간을 멈춘다. 다른 이유(확인 팝업·시장)와 따로 걸고 푼다
    // 씬과 상관없는 일이지만, 앱 종료 중(s_IsQuitting)에는 매니저가 먼저 파괴될 수 있어 그 판단이 있는 여기서 받는다
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

    // 이 씬의 Flow를 만든다. 씬 전용 Service는 Flow가 들고, 공용(이펙트·UI 요소·팝업)은 FlowManager가 붙인다. 씬마다 한 번만 불린다(다시 하기는 같은 Flow를 다시 쓴다)
    protected abstract FlowBase CreateFlow();

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
        FlowManager.ResetSingleton();
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
