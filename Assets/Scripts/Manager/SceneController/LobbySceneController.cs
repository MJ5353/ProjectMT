using UnityEngine;

// ==================================================

// 로비 씬. LobbyFlow(지금은 공용 Service만)를 쓰고, 하단 탭·[입장]을 누르면 버섯이 튄다(UIManager.PlayClickBurst). 로비 전용 Service가 생기면 LobbyFlow에 등록한다

[DefaultExecutionOrder(-1100)]
public sealed class LobbySceneController : SceneControllerBase
{
    public ScreenLobby ScreenUI => GetScreen<ScreenLobby>();

    // ------------------------------------------------

    protected override FlowBase CreateFlow() => new LobbyFlow();
}
