using UnityEngine;

// ==================================================

// 버끼처럼 겁 많은 몬스터: 맞으면(Def.FleeTime > 0) 대상 반대로 빠르게 달아났다가 추적. 애니 배속은 자기 Exit에서 되돌린다

public class FleeCore : AICore
{
    private const float AnimSpeed = 1.6f;

    public override ECoreState Type => ECoreState.Flee;

    // ------------------------------------------------

    public FleeCore(UnitController owner, AIDef def, TargetSenseSub sense) : base(owner, def, sense) { }

    protected override void Enter()
    {
        base.Enter();
        Owner.AnimF.SetSpeed(AnimSpeed);
    }

    protected override void Decide(float deltaTime)
    {
        if (TimeInState >= Def.FleeTime)
        {
            ChangeCore(ECoreState.Chase);
            return;
        }

        Move(-Target.GetDirection(), Def.FleeSpeed);
    }

    public override void Exit()
    {
        Owner.AnimF.SetSpeed(1f);
    }
}
