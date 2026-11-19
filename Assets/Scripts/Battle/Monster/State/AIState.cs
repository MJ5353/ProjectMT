// ==================================================

// AI 판단 상태의 기반. 상태마다 파일 하나(State/). 지금은 몬스터 전용(MonsterController·MonsterDef를 읽는다)
// 흐름: 대기 ↔ 배회 → (발견) 추적 → (공격 요청: 행동 층) → 추적 / 맞으면(피격 행동 뒤) 도망 → 추적 / 사망 뒤 처리

public abstract class AIState : IState
{
    protected readonly MonsterController m_Controller;

    protected MonsterDef Def => m_Controller.Def;
    protected float TimeInState => m_Controller.TimeInState;

    // ------------------------------------------------

    protected AIState(MonsterController controller)
    {
        m_Controller = controller;
    }

    // virtual ---

    public virtual void Enter() { }
    public virtual void Update(float deltaTime) { }
    public virtual void Exit() { }
}
