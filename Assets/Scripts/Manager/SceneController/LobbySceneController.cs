using UnityEngine;

// ==================================================

// 로비 씬. 지금은 공용 Service(이펙트·팝업)만 쓰고, 하단 탭·[입장]을 누르면 버섯이 튄다(UIManager.PlayClickBurst). 로비 전용 Service가 생기면 RegisterServices에서 등록한다

[DefaultExecutionOrder(-1100)]
public sealed class LobbySceneController : SceneControllerBase
{
    public override EGameState State => EGameState.Lobby;

    public ScreenLobby ScreenUI => GetScreen<ScreenLobby>();
}
