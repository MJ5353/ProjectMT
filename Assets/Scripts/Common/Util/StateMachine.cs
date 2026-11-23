using System.Collections.Generic;

// ==================================================

public interface IState
{
    void Enter();
    void Update(float deltaTime);
    void Exit();
}

// ==================================================

// 키(보통 enum)로 상태를 바꾸는 작은 FSM. 씬 흐름(FlowBase<TState>)이 쓴다.
public class StateMachine<TKey>
{
    private readonly Dictionary<TKey, IState> m_DicState = new();
    private IState m_CurrentState;

    public TKey Current { get; private set; }
    public float TimeInState { get; private set; }

    // ------------------------------------------------

    public void Add(TKey key, IState state)
    {
        m_DicState[key] = state;
    }

    public void Change(TKey key)
    {
        if (m_DicState.TryGetValue(key, out IState next) == false)
            return;

        m_CurrentState?.Exit();
        Current = key;
        TimeInState = 0f;
        m_CurrentState = next;
        m_CurrentState.Enter();
    }

    public void Update(float deltaTime)
    {
        if (m_CurrentState == null)
            return;

        TimeInState += deltaTime;
        m_CurrentState.Update(deltaTime);
    }

    // 지금 상태를 Exit로 정리하고 멈춘다. 등록한 상태는 남겨서 다시 Change할 수 있다
    public void Stop()
    {
        m_CurrentState?.Exit();
        m_CurrentState = null;
    }

    public void Clear()
    {
        m_CurrentState?.Exit();
        m_CurrentState = null;
        m_DicState.Clear();
    }
}
