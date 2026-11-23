using UnityEngine;

// ==================================================

// 배회. 스폰 자리(TargetSenseSub.HomePosition) 근처 한 곳으로 걷다가 닿거나 시간이 다 되면 대기. 대상이 보이면 추적

public class WanderCore : AICore
{
    private const float MaxDuration = 3f;
    private const float ArriveDistance = 0.3f;

    private Vector3 m_Destination;

    public override ECoreState Type => ECoreState.Wander;

    // ------------------------------------------------

    public const EAISub Needs = EAISub.TargetSense;   // 이 코어가 쓰는 서브. 조립(MonsterDef.SetAI)이 프리셋 코어들의 Needs를 모아 서브를 붙인다(AICore.TryGetNeeds)

    public WanderCore(UnitController owner, AIDef def) : base(owner, def) { }

    protected override void Enter()
    {
        base.Enter();
        Vector2 offset = Random.insideUnitCircle * Def.WanderRadius;
        m_Destination = Sense.HomePosition + new Vector3(offset.x, 0f, offset.y);
    }

    protected override void Decide(float deltaTime)
    {
        if (Target.IsInRange(Def.SightRange))
        {
            ChangeCore(ECoreState.Chase);
            return;
        }

        Vector3 diff = m_Destination - Owner.transform.position;
        diff.y = 0f;
        if (diff.magnitude < ArriveDistance || TimeInState >= MaxDuration)
        {
            ChangeCore(ECoreState.Idle);
            return;
        }

        Move(diff, MoveSpeed * Def.WanderSpeedRate);
    }
}
