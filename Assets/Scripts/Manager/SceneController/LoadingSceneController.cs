using UnityEngine;

// ==================================================

// 로딩 씬(가짜 로딩). 씬 전환 중에 SceneLoader.LoadViaLoading이 거쳐 가며 최소 시간만 보여 준다. 바로 실행하면 로비로 간다

[DefaultExecutionOrder(-1100)]
public sealed class LoadingSceneController : SceneControllerBase
{
    public override EGameState State => EGameState.Lobby;
    public override bool UseClickBurst => true;
}
