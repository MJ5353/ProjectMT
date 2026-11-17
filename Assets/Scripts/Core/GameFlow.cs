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

        m_Runner.StartCoroutine(Transition(next));
        return true;
    }

    private IEnumerator Transition(EGameState next)
    {
        IsTransitioning = true;
        EGameState from = Current;

        string sceneName = GetSceneName(next);
        if (sceneName.E_IsNullOrEmpty() == false && SceneManager.GetActiveScene().name != sceneName)
            yield return SceneLoader.Load(sceneName);

        Current = next;
        IsTransitioning = false;
        EventBus.Publish(new GameStateChanged(from, next));
    }
}
