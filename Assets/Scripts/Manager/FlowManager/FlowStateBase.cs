// ==================================================

// Flow 상태 하나. 주인 Flow를 들고, 들어올 때·매 프레임·나갈 때 할 일만 덮어쓴다

public abstract class FlowStateBase<TFlow> : IState where TFlow : FlowBase
{
    protected TFlow Flow { get; }

    // ------------------------------------------------

    protected FlowStateBase(TFlow flow)
    {
        Flow = flow;
    }

    // virtual ---

    public virtual void Enter() { }
    public virtual void Update(float deltaTime) { }
    public virtual void Exit() { }
}
