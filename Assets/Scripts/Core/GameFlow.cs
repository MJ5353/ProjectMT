using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum EGameState
{
    None,
    Lobby,
    Battle,
    Result
}

// 게임 전체 흐름(로비 → 전투 → 결과 → 로비) 상태 머신.
// 허용된 전환만 통과시키고, 상태에 맞는 씬이 다르면 씬을 바꾼 뒤 GameStateChanged를 발행한다
public class GameFlow
{
    // fsm st
    private static readonly Dictionary<EGameState, EGameState[]> s_DicAllowedTransition = new()
    {
        { EGameState.None, new[] { EGameState.Lobby, EGameState.Battle } },
        { EGameState.Lobby, new[] { EGameState.Battle } },
        { EGameState.Battle, new[] { EGameState.Result, EGameState.Lobby } },
        { EGameState.Result, new[] { EGameState.Lobby, EGameState.Battle } },
    };

    private readonly MonoBehaviour m_Runner;

    public EGameState Current { get; private set; } = EGameState.None;
    public bool IsTransitioning { get; private set; }
    public BattleSession ActiveBattle { get; private set; }

    public GameFlow(MonoBehaviour runner)
    {
        m_Runner = runner;
    }

    // ==================================================

    // 결과(Result)처럼 씬이 따로 없는 상태는 null
    public static string GetSceneName(EGameState state)
    {
        switch (state)
        {
            case EGameState.Lobby:
                return SceneNames.Lobby;
            case EGameState.Battle:
                return SceneNames.Battle;
            default:
                return null;
        }
    }

    // 에디터에서 아무 씬이나 바로 플레이해도 흐름이 맞게 시작하도록 씬 이름으로 상태를 추정한다
    public static EGameState GetStateByScene(string sceneName)
    {
        if (sceneName == SceneNames.Battle)
            return EGameState.Battle;

        return EGameState.Lobby;
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
            CLogger.Warning("GameFlow", $"허용되지 않은 전환: {Current} → {next}");
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

    internal BattleSession GetBattle(Scene scene)
    {
        return ActiveBattle != null && ActiveBattle.Scene == scene ? ActiveBattle : null;
    }

    internal BattleSession BeginBattle(Scene scene)
    {
        BattleSession existing = GetBattle(scene);
        if (existing != null)
            return existing;

        ActiveBattle?.Actors.Clear();
        ActiveBattle = new BattleSession(scene);
        return ActiveBattle;
    }

    internal void EndBattle(BattleSession session)
    {
        if (session == null || session != ActiveBattle)
            return;

        session.Actors.Clear();
        ActiveBattle = null;
    }

    private bool StartTransition(EGameState next, bool reloadScene)
    {
        string sceneName = GetSceneName(next);
        bool shouldLoad = sceneName != null && (reloadScene || SceneManager.GetActiveScene().name != sceneName);
        if (shouldLoad && Application.CanStreamedLevelBeLoaded(sceneName) == false)
        {
            CLogger.Warning("GameFlow", $"씬을 로드할 수 없습니다: {sceneName}");
            return false;
        }

        m_Runner.StartCoroutine(Transition(next, shouldLoad ? sceneName : null));
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

        EventBus.Publish(new GameStateChanged(from, next));
    }
}
