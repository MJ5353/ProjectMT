// ==================================================

public enum ELobbyFlowState
{
    Play,
}

// ==================================================

// 로비 씬 흐름. 지금은 공용 Service만 쓴다. 로비 전용 Service가 생기면 생성자에서 Register한다

public sealed class LobbyFlow : FlowBase<ELobbyFlowState>
{
    public LobbyFlow()
    {
        AddState(ELobbyFlowState.Play, new PlayState(this));
    }

    // virtual ---

    protected override void OnBegin() => ChangeState(ELobbyFlowState.Play);

    // state ---

    private sealed class PlayState : FlowStateBase<LobbyFlow>
    {
        public PlayState(LobbyFlow flow) : base(flow) { }
    }
}
