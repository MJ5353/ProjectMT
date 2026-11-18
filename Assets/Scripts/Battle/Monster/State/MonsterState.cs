// ==================================================

// 몬스터 FSM 상태들의 기반. 상태마다 파일 하나(State/), 수치는 전부 MonsterDef에서 읽는다
// 흐름: 대기 ↔ 배회 → (발견) 추적 → 움츠림 → 공격 → 추적 / 맞으면 피격 → (도망) → 추적 / 사망

public abstract class MonsterState : IState
{
    protected readonly MonsterController m_Controller;

    protected MonsterDef Def => m_Controller.Def;
    protected float TimeInState => m_Controller.TimeInState;

    // ------------------------------------------------

    protected MonsterState(MonsterController controller)
    {
        m_Controller = controller;
    }

    // virtual ---

    public virtual void Enter() { }
    public virtual void Update(float deltaTime) { }
    public virtual void Exit() { }
}
