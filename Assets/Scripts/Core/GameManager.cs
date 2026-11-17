using UnityEngine;
using UnityEngine.SceneManagement;

// 게임 전역 진입점. 어떤 씬에서 플레이를 눌러도 씬 로드 전에 자동 생성된다.
// 데이터베이스, 플레이어 데이터, 세이브, 게임 흐름을 들고 있는다

[DefaultExecutionOrder(-1000)]
public class GameManager : MonoSingleton<GameManager>
{
    public GameDatabase Database { get; private set; }
    public GameFlow Flow { get; private set; }
    public PlayerData Player { get; private set; }

    // [mj] todo) server?
    private SaveService m_SaveService;

    // ==================================================

    private void Start()
    {
        if (Flow == null)
            return;

        Flow.ChangeState(GameFlow.GetStateByScene(SceneManager.GetActiveScene().name));
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused && Player != null)
            SaveNow();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => _ = Instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ResetSingleton();

    // virtual ----

    protected override void Init()
    {
        Database = Resources.Load<GameDatabase>(GameDatabase.ResourcePath);
        if (Database == null)
            CLogger.Error("GameManager", $"Resources/{GameDatabase.ResourcePath} 데이터베이스가 없습니다. 메뉴 MushTaker/Data/데이터베이스 갱신을 실행하세요.");
        else
            Database.Init();

        m_SaveService = new SaveService();

        Player = new PlayerData();
        Player.Load(m_SaveService.Load());

        Flow = new GameFlow(this);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (TryGetComponent<DebugPanel>(out _) == false)
            gameObject.AddComponent<DebugPanel>();
#endif
    }

    protected override void OnQuit() => SaveNow();

    // save

    public void SaveNow() => m_SaveService.Save(Player.ToSaveData());

    public void ResetSave()
    {
        m_SaveService.Delete();
        Player.Clear();
    }
}
