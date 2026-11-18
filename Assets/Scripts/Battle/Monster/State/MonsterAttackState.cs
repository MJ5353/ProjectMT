using UnityEngine;

// ==================================================

public class MonsterAttackState : MonsterState
{
    public MonsterAttackState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        // 방향은 추적·예고 동안 TurnSpeed로만 맞춘다. 공격 중에는 고정해서 옆으로 빠질 틈을 준다
        m_Controller.Melee.StartAttack(m_Controller.CurrentAttack, m_Controller.Def.AttackBlendTime);
    }

    public override void Update(float deltaTime)
    {
        m_Controller.Melee.UpdateAttack(TimeInState);
        if (m_Controller.Melee.IsFinished(TimeInState) == false)
            return;

        // 콤보면 추적으로 돌아가지 않고 바로 다음 타. 방향은 첫 타 그대로
        if (m_Controller.CurrentAttack.NextAttack != null)
            m_Controller.ContinueCombo();
        else
            m_Controller.ChangeState(EMonsterState.Chase);
    }

    public override void Exit()
    {
        m_Controller.Move.Stop();
        m_Controller.Melee.End();
        m_Controller.StartAttackCooldown();
    }
}
