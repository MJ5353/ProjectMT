// ==================================================

// AI 판단 상태의 기반. 상태마다 파일 하나(State/). 어느 AI 종류에나 끼울 수 있다(AIBrain.SetStates).
// 상태 바꾸기·스폰 자리·공격 고르기는 판단(m_AI), 수치는 Def(AIDef), 속도는 m_AI.MoveSpeed·WanderSpeed, 대상은 TargetSelector, 몸·행동은 m_Controller로 본다
// 흐름: 대기 ↔ 배회 → (발견) 추적 → (공격 요청: 실행기) → 추적 / 맞으면(피격 행동 뒤) 도망·숨기 → 추적 / 사망 뒤 처리

public abstract class AIStateBase : IState
{
    protected readonly AIBrain m_AI;
    protected readonly UnitController m_Controller;

    protected AIDef Def => m_AI.Def;
    protected TargetSelector TargetSelector => m_AI.TargetSelector;
    protected float TimeInState => m_AI.TimeInState;

    // ------------------------------------------------

    protected AIStateBase(AIBrain ai)
    {
        m_AI = ai;
        m_Controller = ai.Controller;
    }

    // virtual ---

    public virtual void Enter() { }
    public virtual void Update(float deltaTime) { }
    public virtual void Exit() { }
}
