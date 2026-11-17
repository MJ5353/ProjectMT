using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MushTaker.Core
{
    public enum GameState
    {
        None,
        Lobby,
        Battle,
        Result
    }

    /// <summary>
    /// 게임 전체 흐름(로비 → 전투 → 결과 → 로비)을 다루는 상태 머신.
    /// 허용된 전환만 통과시키고, 상태에 맞는 씬이 다르면 씬을 바꾼 뒤 GameStateChanged를 발행한다.
    /// </summary>
    public class GameFlow
    {
        static readonly Dictionary<GameState, GameState[]> s_allowed = new()
        {
            { GameState.None, new[] { GameState.Lobby, GameState.Battle } },
            { GameState.Lobby, new[] { GameState.Battle } },
            { GameState.Battle, new[] { GameState.Result, GameState.Lobby } },
            { GameState.Result, new[] { GameState.Lobby, GameState.Battle } },
        };

        readonly MonoBehaviour _runner;

        public GameState Current { get; private set; } = GameState.None;
        public bool IsTransitioning { get; private set; }

        public GameFlow(MonoBehaviour runner) => _runner = runner;

        // ==================================================
        
        /// 결과(Result)처럼 씬이 따로 없는 상태는 null.
        public static string SceneFor(GameState state) => state switch
        {
            GameState.Lobby => SceneNames.Lobby,
            GameState.Battle => SceneNames.Battle,
            _ => null,
        };

        /// 에디터에서 아무 씬이나 바로 플레이해도 흐름이 맞게 시작하도록 씬 이름으로 상태를 추정한다.
        public static GameState StateForScene(string sceneName) => sceneName switch
        {
            SceneNames.Battle => GameState.Battle,
            _ => GameState.Lobby,
        };

        public bool CanChange(GameState next)
        {
            if (IsTransitioning || s_allowed.TryGetValue(Current, out var targets) == false)
                return false;

            // [mj] todo) common func
            return System.Array.IndexOf(targets, next) >= 0;
        }

        public bool ChangeState(GameState next)
        {
            if (!CanChange(next))
            {
                // [mj] todo) log util
                Debug.LogWarning($"[GameFlow] 허용되지 않은 전환: {Current} → {next}");
                return false;
            }

            _runner.StartCoroutine(Transition(next));
            return true;
        }

        IEnumerator Transition(GameState next)
        {
            IsTransitioning = true;
            GameState from = Current;

            // [mj] todo) isnulloremtpy
            string scene = SceneFor(next);
            if (scene != null && SceneManager.GetActiveScene().name != scene)
                yield return SceneLoader.Load(scene);

            Current = next;
            IsTransitioning = false;
            EventBus.Publish(new GameStateChanged(from, next));
        }
    }
}