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

    public WanderCore(UnitController owner, AIDef def, TargetSenseSub sense) : base(owner, def, sense) { }

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
