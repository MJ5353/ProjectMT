using System.Collections.Generic;
using UnityEngine.SceneManagement;

// 결과 화면까지 유지되는 전투 한 판의 상태.
public sealed class BattleFlow
{
    public EBattleState State { get; set; } = EBattleState.Playing;
    public float ElapsedTime { get; set; }
    public int KillCount { get; set; }
    
    // management
    public ActorManagement Actors { get; } = new();
    public ChunkBurstManagement Chunks { get; } = new();   // 피격 조각 연출(씬이 닫힐 때 SceneController가 Clear)

    public Scene Scene { get; }
    public Dictionary<string, int> Loot { get; } = new();

    public BattleFlow(Scene scene)
    {
        Scene = scene;
    }
}
