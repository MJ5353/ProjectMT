// ==================================================

public enum ETitleFlowState
{
    Play,
}

// ==================================================

// 타이틀 씬 흐름. 전용 Service 없이 공용(이펙트·팝업)만 쓴다. 로비로 넘어가는 건 화면(ScreenTitle)이 GameManager에 요청한다

public sealed class TitleFlow : FlowBase<ETitleFlowState>
{
    public TitleFlow()
    {
        AddState(ETitleFlowState.Play, new PlayState(this));
    }

    // virtual ---

    protected override void OnBegin() => ChangeState(ETitleFlowState.Play);

    // state ---

    private sealed class PlayState : FlowStateBase<TitleFlow>
    {
        public PlayState(TitleFlow flow) : base(flow) { }
    }
}
