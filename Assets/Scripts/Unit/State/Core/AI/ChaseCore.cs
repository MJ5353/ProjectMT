using UnityEngine;

// ==================================================

// 추적. 대상을 놓치면(없음·LoseRange 밖) 대기. 쓸 수 있는 공격이 있으면 공격을 요청하고(끝나면 이 코어로 돌아온다), 정지 거리 안이면 마주 보고 선다

public class ChaseCore : AICore
{
    private readonly AttackPickerSub m_Picker;

    public override ECoreState Type => ECoreState.Chase;

    // ------------------------------------------------

    public ChaseCore(UnitController owner, AIDef def, TargetSenseSub sense, AttackPickerSub picker) : base(owner, def, sense)
    {
        m_Picker = picker;
    }

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
        if (m_Picker.TryPick(distance) &&
            Owner.StateF.RequestAttack(m_Picker.Current, Vector3.zero, Target.TargetTransform))
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
