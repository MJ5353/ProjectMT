using UnityEngine;

// 로비와 전투에서 공유하는 게임 상태. SceneController가 초기화한다.
// 데이터베이스, 플레이어 데이터, 세이브, 게임 흐름을 들고 있는다

public class GameManager : MonoSingleton<GameManager>
{
    public GameDatabase Database { get; private set; }
    public GameFlow Flow { get; private set; }
    public PlayerData Player { get; private set; }

    // [mj] todo) server?
    private SaveService m_SaveService;

    // ==================================================

    // virtual ----

    protected override void OnInit()
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
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (TryGetComponent<DebugPanel>(out _) == false)
            gameObject.AddComponent<DebugPanel>();
#endif
    }

    // save

    public void SaveNow() => m_SaveService.Save(Player.ToSaveData());

    public void ResetSave()
    {
        m_SaveService.Delete();
        Player.Clear();
    }
}
