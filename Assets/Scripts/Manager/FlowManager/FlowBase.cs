using System;
using System.Collections.Generic;

// ==================================================

// 씬 하나의 흐름. 이 씬에서만 쓰는 Service를 들고 Init·Tick·Clear를 부른다. 공용 Service는 FlowManager가 따로 돌린다.
// 어떤 Flow를 쓸지는 씬 컨트롤러(CreateFlow)가 정하고, FlowManager가 Begin·Tick·End를 부른다.
// 자식은 생성자에서 Service를 Register하고, 시작·끝에 할 일은 OnBegin·OnEnd에 둔다.
// 다시 하기는 같은 인스턴스로 End → Begin을 다시 부른다. Service는 Clear에서 처음 상태로 되돌려야 한다

public abstract class FlowBase
{
    private readonly List<ServiceBase> m_ListService = new();

    // ------------------------------------------------

    // 등록한 순서로 Init하고, 상태 머신을 시작한다
    public void Begin()
    {
        foreach (ServiceBase service in m_ListService)
        {
            service.Init();
        }

        OnBegin();
    }

    public void Tick(float deltaTime)
    {
        foreach (ServiceBase service in m_ListService)
        {
            service.Tick(deltaTime);
        }

        TickState(deltaTime);
    }

    // 상태를 먼저 끝내고(Exit), 등록 반대 순서로 Clear한다
    public void End()
    {
        StopState();
        OnEnd();

        for (int i = m_ListService.Count - 1; i >= 0; i--)
        {
            m_ListService[i].Clear();
        }
    }

    // 팝업이 없을 때 뒤로 가기(Esc). PopupService가 부른다
    public void Back() => OnBack();

    // 생성자에서 부른다
    protected T Register<T>(T service) where T : ServiceBase
    {
        m_ListService.Add(service);
        return service;
    }

    // virtual ---

    protected virtual void OnBegin() { }
    protected virtual void OnEnd() { }
    protected virtual void OnBack() { }

    // FlowBase<TState>만 쓴다
    protected virtual void TickState(float deltaTime) { }
    protected virtual void StopState() { }
}

// ==================================================

// 상태(enum)를 가진 Flow. 상태는 생성자에서 AddState로 등록하고, OnBegin에서 첫 상태로 ChangeState한다.
// 상태가 하나뿐이면(Title·Lobby) 이걸 쓰지 않고 FlowBase를 바로 상속한다

public abstract class FlowBase<TState> : FlowBase where TState : struct, Enum
{
    private readonly StateMachine<TState> m_StateMachine = new();

    public TState State => m_StateMachine.Current;
    public float TimeInState => m_StateMachine.TimeInState;

    // ------------------------------------------------

    // 상태를 바꾸고(이전 Exit → 다음 Enter) 자식에게 알린다
    public void ChangeState(TState next)
    {
        m_StateMachine.Change(next);
        OnStateChanged(next);
    }

    protected void AddState(TState key, IState state) => m_StateMachine.Add(key, state);

    // virtual ---

    protected virtual void OnStateChanged(TState state) { }

    protected sealed override void TickState(float deltaTime) => m_StateMachine.Update(deltaTime);
    
    protected sealed override void StopState() => m_StateMachine.Stop();
}
