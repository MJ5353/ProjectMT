using UnityEngine;

// ==================================================

// 추적. 대상을 놓치면(없음·LoseRange 밖) 대기. 쓸 수 있는 공격이 있으면 공격을 요청하고(끝나면 이 코어로 돌아온다), 정지 거리 안이면 마주 보고 선다

public class ChaseCore : AICore
{

    public override ECoreState Type => ECoreState.Chase;

    // ------------------------------------------------

    public const EAISub Needs = EAISub.TargetSense | EAISub.AttackPicker;   // 이 코어가 쓰는 서브. 조립(MonsterDef.SetAI)이 프리셋 코어들의 Needs를 모아 서브를 붙인다(AICore.TryGetNeeds)

    public ChaseCore(UnitController owner, AIDef def) : base(owner, def) { }

    protected override void Decide(float deltaTime)
    {
        float distance = Target.GetDistance();
        bool isTargetGone = Target.HasTarget == false;

        if (isTargetGone || distance > Def.LoseRange)
        {
            ChangeCore(ECoreState.Idle);
            return;
        }

        // 예고가 있으면 공격 코어가 움츠리며 대상 쪽으로 돈다
        if (Picker.TryPick(distance) &&
            Owner.StateF.RequestAttack(Picker.Current, Vector3.zero, Target.TargetTransform))
            return;

        // 정지 거리 안: 마주 보고 대기
        if (distance <= Def.AttackRange)
        {
            Move(Vector3.zero, 0f);
            Owner.MoveF.FaceTo(Target.GetDirection(), deltaTime);
            return;
        }

        Move(Target.GetDirection(), MoveSpeed);
    }
}
