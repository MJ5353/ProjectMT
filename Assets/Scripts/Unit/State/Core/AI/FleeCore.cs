using UnityEngine;

// ==================================================

// 겁 많은 몬스터(AI_Flee): 맞으면(피격 뒤 코어 AIDef.AfterHitCore = Flee) Def.FleeTime 동안 대상 반대로 빠르게 달아났다가 추적. 애니 배속은 자기 Exit에서 되돌린다

public class FleeCore : AICore
{
    private const float AnimSpeed = 1.6f;

    public override ECoreState Type => ECoreState.Flee;

    // ------------------------------------------------

    public const EAISub Needs = EAISub.TargetSense;   // 이 코어가 쓰는 서브. 조립(MonsterDef.SetAI)이 프리셋 코어들의 Needs를 모아 서브를 붙인다(AICore.TryGetNeeds)

    public FleeCore(UnitController owner, AIDef def) : base(owner, def) { }

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
