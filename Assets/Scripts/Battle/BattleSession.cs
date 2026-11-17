using System.Collections.Generic;
using UnityEngine.SceneManagement;

// 결과 화면까지 유지되는 전투 한 판의 상태.
public sealed class BattleSession
{
    public ActorManagement Actors { get; } = new();
    public ChunkBurstManagement Chunks { get; } = new();   // 피격 조각 연출(씬이 닫힐 때 SceneController가 Clear)
    public EBattleState State { get; internal set; } = EBattleState.Playing;
    public int KillCount { get; internal set; }
    public float ElapsedTime { get; internal set; }

    internal Scene Scene { get; }
    internal Dictionary<string, int> Loot { get; } = new();

    internal BattleSession(Scene scene)
    {
        Scene = scene;
    }
}
