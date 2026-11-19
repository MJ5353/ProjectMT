// ==================================================

// AI 판단 상태의 기반. 상태마다 파일 하나(State/). TBrain은 상태를 쓰는 판단(MonsterBrain·PetBrain 등)
// 상태 바꾸기·스폰 자리는 판단(m_AI), 대상은 Target, 몸·행동은 m_Controller로 본다

public abstract class AIState<TBrain> : IState where TBrain : AIBrain
{
    protected readonly TBrain m_AI;
    protected readonly UnitController m_Controller;

    protected float TimeInState => m_AI.TimeInState;
    protected TargetSensor Target => m_AI.Target;

    // ------------------------------------------------

    protected AIState(TBrain ai)
    {
        m_AI = ai;
        m_Controller = ai.Controller;
    }

    // virtual ---

    public virtual void Enter() { }
    public virtual void Update(float deltaTime) { }
    public virtual void Exit() { }
}

// ==================================================

// 몬스터 상태의 기반. 수치는 MonsterDef, 공격 고르기는 m_AI.AttackChoice. MonsterBrain을 이어받은 판단(Burrow·Boss)이 같이 쓴다
// 흐름: 대기 ↔ 배회 → (발견) 추적 → (공격 요청: 행동 층) → 추적 / 맞으면(피격 행동 뒤) 도망 → 추적 / 사망 뒤 처리

public abstract class MonsterState : AIState<MonsterBrain>
{
    protected MonsterDef Def => m_AI.Def;

    // ------------------------------------------------

    protected MonsterState(MonsterBrain ai) : base(ai) { }
}
