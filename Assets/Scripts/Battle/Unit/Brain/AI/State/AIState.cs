// ==================================================

// AI 판단 상태의 기반. 상태마다 파일 하나(State/). 지금은 몬스터 전용(MonsterController·MonsterDef를 읽는다)
// 스폰 자리·상태 바꾸기·공격 고르기는 판단 층(m_AI), 대상은 Target, 몸·행동은 m_Controller로 본다
// 흐름: 대기 ↔ 배회 → (발견) 추적 → (공격 요청: 행동 층) → 추적 / 맞으면(피격 행동 뒤) 도망 → 추적 / 사망 뒤 처리

public abstract class AIState : IState
{
    protected readonly MonsterBrain m_AI;
    protected readonly MonsterController m_Controller;

    protected MonsterDef Def => m_Controller.Def;
    protected float TimeInState => m_AI.TimeInState;
    protected TargetSensor Target => m_AI.Target;

    // ------------------------------------------------

    protected AIState(MonsterBrain ai)
    {
        m_AI = ai;
        m_Controller = ai.Controller;
    }

    // virtual ---

    public virtual void Enter() { }
    public virtual void Update(float deltaTime) { }
    public virtual void Exit() { }
}
