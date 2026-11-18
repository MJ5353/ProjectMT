using UnityEngine;

// ==================================================

public class MonsterAttackState : MonsterState
{
    public MonsterAttackState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        // 방향은 추적·예고 동안 TurnSpeed로만 맞춘다. 공격 중에는 고정해서 옆으로 빠질 틈을 준다
        m_Controller.Melee.StartAttack(m_Controller.CurrentAttack);
    }

    public override void Update(float deltaTime)
    {
        m_Controller.Melee.UpdateAttack(TimeInState);
        if (m_Controller.Melee.IsFinished(TimeInState))
            m_Controller.ChangeState(EMonsterState.Chase);
    }

    public override void Exit()
    {
        m_Controller.Move.Stop();
        m_Controller.Melee.End();
        m_Controller.StartAttackCooldown();
    }
}
