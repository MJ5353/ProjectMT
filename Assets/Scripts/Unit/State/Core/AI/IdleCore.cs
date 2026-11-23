using UnityEngine;

// ==================================================

// 대기. 멈춰 서 있다가 대상이 보이면 추적, 잠시 뒤 배회. AI 기본 코어(꺼낼 때·멈출 때)

public class IdleCore : AICore
{
    private float m_Duration;

    public override ECoreState Type => ECoreState.Idle;

    // ------------------------------------------------

    public IdleCore(UnitController owner, AIDef def, TargetSenseSub sense) : base(owner, def, sense) { }

    protected override void Enter()
    {
        base.Enter();
        Move(Vector3.zero, 0f);
        m_Duration = Random.Range(1f, 2.5f);
    }

    protected override void Decide(float deltaTime)
    {
        if (Target.IsInRange(Def.SightRange))
        {
            ChangeCore(ECoreState.Chase);
            return;
        }

        if (TimeInState >= m_Duration)
            ChangeCore(ECoreState.Wander);
    }
}
