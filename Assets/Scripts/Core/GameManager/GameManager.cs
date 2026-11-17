using UnityEngine;

// 로비와 전투에서 공유하는 게임 상태. SceneController가 초기화한다.
// 데이터베이스와 게임 흐름을 들고 있는다. 플레이어 데이터와 세이브는 PlayerDataManager

// ==================================================

public class GameManager : MonoSingleton<GameManager>
{
    public GameDatabase Database { get; private set; }

    public FlowService FlowService { get; private set; }
    public BattleFlow ActiveBattle => FlowService?.ActiveBattle;
    public ActorManagement ActiveActors => ActiveBattle?.Actors;

    // virtual---

    protected override void OnInit()
    {
        Database = Resources.Load<GameDatabase>(GameDatabase.ResourcePath);
        if (Database == null)
            CLogger.Error("GameManager", $"Resources/{GameDatabase.ResourcePath} 데이터베이스가 없습니다. 메뉴 MushTaker/Data/데이터베이스 갱신을 실행하세요.");
        else
            Database.Init();

        DontDestroyOnLoad(gameObject);
        
        // flow
        FlowService = new FlowService(this);
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (TryGetComponent<DebugPanel>(out _) == false)
            gameObject.AddComponent<DebugPanel>();
#endif
    }
}
