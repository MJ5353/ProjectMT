using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ==================================================

public enum EGameState
{
    None,
    Lobby,
    Battle,
    Result
}

// ==================================================

// 로비와 전투의 상태 전환과 씬 서비스의 수명 주기를 관리한다.
public class GameManager : MonoSingleton<GameManager>
{
    private static readonly Dictionary<EGameState, EGameState[]> s_DicAllowedTransition = new()
    {
        { EGameState.None, new[] { EGameState.Lobby, EGameState.Battle } },
        { EGameState.Lobby, new[] { EGameState.Battle } },
        { EGameState.Battle, new[] { EGameState.Result, EGameState.Lobby } },
        { EGameState.Result, new[] { EGameState.Lobby, EGameState.Battle } },
    };

    private readonly List<ServiceBase> m_ListService = new();
    private Scene m_ServiceScene;

    public GameDatabase Database { get; private set; }
    public DefineDef Define { get; private set; } // 전역 조절값. 에셋이 없으면 코드 기본값으로 만든다
    public EGameState Current { get; private set; } = EGameState.None;
    public bool IsTransitioning { get; private set; }

    // service
    public BattleService BattleS { get; private set; }
    public UnitService UnitS { get; private set; }
    public EffectService EffectS { get; private set; }
    public HitStopService HitStopS { get; private set; }
    public PopupService PopupS { get; private set; }

    // ------------------------------------------------

    // 씬 전용 Service는 씬 컨트롤러가 정하고(RegisterServices), 공용(팝업)은 여기서 넣는다
    internal void InitSceneServices(Scene scene, SceneControllerBase sceneController)
    {
        if (m_ServiceScene == scene)
            return;

        ClearSceneServices(m_ServiceScene);
        m_ServiceScene = scene;

        sceneController.RegisterServices(this);
        PopupS = Register(new PopupService());
        foreach (ServiceBase service in m_ListService)
        {
            service.Init();
        }

        if (Current == EGameState.None && IsTransitioning == false)
            ChangeState(sceneController.State);
    }

    // BattleSceneController가 부른다
    public void RegisterBattleServices(MapDef mapDef)
    {
        UnitS = Register(new UnitService());
        EffectS = Register(new EffectService());
        HitStopS = Register(new HitStopService());
        BattleS = Register(new BattleService(mapDef));
    }

    private T Register<T>(T service) where T : ServiceBase
    {
        m_ListService.Add(service);
        return service;
    }

    internal void TickSceneServices(Scene scene, float deltaTime)
    {
        if (m_ServiceScene != scene)
            return;

        foreach (ServiceBase service in m_ListService)
        {
            service.Tick(deltaTime);
        }
    }

    internal void ClearSceneServices(Scene scene)
    {
        if (m_ServiceScene != scene)
            return;

        for (int i = m_ListService.Count - 1; i >= 0; i--)
        {
            m_ListService[i].Clear();
        }

        m_ListService.Clear();
        BattleS = null;
        UnitS = null;
        EffectS = null;
        HitStopS = null;
        PopupS = null;
        
        m_ServiceScene = default;
    }

    // 상태와 씬 전환 ---

    public static string GetSceneName(EGameState state)
    {
        switch (state)
        {
            case EGameState.Lobby:
                return SceneNames.Lobby;
            case EGameState.Battle:
                return SceneNames.Battle;
            default: // 결과(Result)처럼 씬이 따로 없는 상태는 null
                return null;
        }
    }

    public bool CanChange(EGameState next)
    {
        if (IsTransitioning || s_DicAllowedTransition.TryGetValue(Current, out EGameState[] targets) == false)
            return false;

        return targets.E_Contains(next);
    }

    public bool ChangeState(EGameState next)
    {
        if (CanChange(next) == false)
        {
            CLogger.Warning("GameManager", $"허용되지 않은 전환: {Current} → {next}");
            return false;
        }

        return StartTransition(next, Current == EGameState.Result && next == EGameState.Battle);
    }

    public bool RetryBattle()
    {
        if (Current == EGameState.Result)
            return ChangeState(EGameState.Battle);

        if (Current != EGameState.Battle || IsTransitioning)
            return false;

        return StartTransition(EGameState.Battle, true);
    }

    private bool StartTransition(EGameState next, bool reloadScene)
    {
        string sceneName = GetSceneName(next);
        bool shouldLoad = sceneName != null && (reloadScene || SceneManager.GetActiveScene().name != sceneName);

        if (shouldLoad && Application.CanStreamedLevelBeLoaded(sceneName) == false)
        {
            CLogger.Warning("GameManager", $"씬을 로드할 수 없습니다: {sceneName}");
            return false;
        }

        StartCoroutine(Transition(next, shouldLoad ? sceneName : null));
        return true;
    }

    private IEnumerator Transition(EGameState next, string sceneName)
    {
        IsTransitioning = true;
        EGameState from = Current;

        try
        {
            if (sceneName != null)
            {
                Scene previousScene = SceneManager.GetActiveScene();
                SEventManager.Publish(new SceneUnloading()); // 씬 오브젝트가 살아 있을 때 UI 칸을 풀로 돌려보낸다
                yield return SceneLoader.Load(sceneName);

                Scene loadedScene = SceneManager.GetActiveScene();
                if (loadedScene == previousScene || loadedScene.name != sceneName)
                    yield break;
            }

            Current = next;
        }
        finally
        {
            IsTransitioning = false;
        }

        SEventManager.Publish(new GameStateChanged(from, next));
    }

    // virtual ---

    protected override void OnInit()
    {
        Database = ResourceManager.Instance?.Load<GameDatabase>(CommonPath.GameDatabase, "MushTaker/데이터 편집기에서 DB 갱신을 실행하세요.");
        if (Database != null)
            Database.Init();

        Define = Database != null ? Database.Get<DefineDef>(DefineDef.DefineID) : null;
        if (Define == null)
        {
            CLogger.Warning("GameManager", "Define 에셋이 DB에 없어 코드 기본값을 씁니다.");
            Define = ScriptableObject.CreateInstance<DefineDef>();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (TryGetComponent<DebugPanel>(out _) == false)
            gameObject.AddComponent<DebugPanel>();
#endif
    }
}
