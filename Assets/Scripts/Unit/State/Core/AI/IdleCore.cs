using UnityEngine;

// ==================================================

// 대기. 멈춰 서 있다가 대상이 보이면 추적, 잠시 뒤 배회. AI 기본 코어(꺼낼 때·멈출 때)

public class IdleCore : AICore
{
    private float m_Duration;

    public override ECoreState Type => ECoreState.Idle;

    // ------------------------------------------------

    public const EAISub Needs = EAISub.TargetSense;   // 이 코어가 쓰는 서브. 조립(MonsterDef.SetAI)이 프리셋 코어들의 Needs를 모아 서브를 붙인다(AICore.TryGetNeeds)

    public IdleCore(UnitController owner, AIDef def) : base(owner, def) { }

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
