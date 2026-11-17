using UnityEngine;

[DefaultExecutionOrder(-1100)]
public sealed class SceneController : MonoBehaviour
{
    [SerializeField] private BattleManager m_BattleManager = null;
    [SerializeField] private PopupManager m_PopupManager = null;

    private HitStopManager m_HitStopManager;
    private BattleSession m_BattleSession;

    // ==================================================

    private void Awake()
    {
        GameManager gameM = GameManager.Instance;
        if (gameM == null)
        {
            gameM = FindFirstObjectByType<GameManager>();
            if (gameM == null)
                gameM = new GameObject("[GameManager]").AddComponent<GameManager>();

            gameM.Init();
        }

        if (TimeManager.Instance == null)
            gameM.gameObject.AddComponent<TimeManager>().Init();

        if (ResourceManager.Instance == null)
            gameM.gameObject.AddComponent<ResourceManager>().Init();

        if (m_PopupManager != null)
            m_PopupManager.Init();

        if (m_BattleManager != null)
        {
            m_BattleSession = gameM.Flow.BeginBattle(gameObject.scene);
            m_HitStopManager = gameObject.AddComponent<HitStopManager>();
            m_HitStopManager.Init();
        }
    }

    private void Start()
    {
        // 플레이어의 Awake에서 체력과 피격 부품을 만든 뒤 전투를 초기화한다.
        if (m_BattleSession != null)
            m_BattleManager.Init(m_BattleSession);

        GameFlow flow = GameManager.Instance?.Flow;
        if (flow != null && flow.Current == EGameState.None && flow.IsTransitioning == false)
            flow.ChangeState(GameFlow.GetStateByScene(gameObject.scene.name));
    }

    private void Update()
    {
        if (m_BattleManager != null)
            m_BattleManager.Tick(Time.deltaTime);

        if (m_HitStopManager != null)
            m_HitStopManager.Tick(Time.unscaledDeltaTime);

        m_BattleSession?.Chunks.Tick(Time.deltaTime);

        if (m_PopupManager != null)
            m_PopupManager.Tick();
    }

    private void OnDestroy()
    {
        m_BattleManager?.Clear();
        m_HitStopManager?.Clear();
        m_PopupManager?.Clear();
        m_BattleSession?.Chunks.Clear();
        GameManager.Instance?.Flow.EndBattle(m_BattleSession);
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
            GameManager.Instance?.RunSave();
    }

    private void OnApplicationQuit() => GameManager.Instance?.RunSave();

    // 도메인 리로드를 끈 상태에서도 새 플레이마다 등록을 초기화한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSingletons()
    {
        GameManager.ResetSingleton();
        TimeManager.ResetSingleton();
        ResourceManager.ResetSingleton();
        BattleManager.ResetSingleton();
        HitStopManager.ResetSingleton();
        PopupManager.ResetSingleton();
    }

#if UNITY_EDITOR
    public void EditorSetup(BattleManager battleManager = null, PopupManager popupManager = null)
    {
        m_BattleManager = battleManager;
        m_PopupManager = popupManager;
    }
#endif
}
