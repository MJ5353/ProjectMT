using UnityEngine;

// ==================================================

// 로비 씬. 지금은 공용 Service(팝업)만 쓴다. 로비 전용 Service가 생기면 RegisterServices에서 등록한다

[DefaultExecutionOrder(-1100)]
public sealed class LobbySceneController : SceneControllerBase
{
    public override EGameState State => EGameState.Lobby;

    public ScreenLobby Screen => GetScreen<ScreenLobby>();
}
