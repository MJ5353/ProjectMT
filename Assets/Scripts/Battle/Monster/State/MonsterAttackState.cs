using UnityEngine;

// ==================================================

public class MonsterAttackState : MonsterState
{
    private float m_Length;

    // ------------------------------------------------

    public MonsterAttackState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        AttackSpec attack = m_Controller.CurrentAttack;
        m_Length = m_Controller.Anim.GetLength(attack.Clip);
        m_Controller.Melee.Begin(attack);

        // 방향은 추적·예고 동안 TurnSpeed로만 맞춘다. 공격 중에는 고정해서 옆으로 빠질 틈을 준다
        m_Controller.Move.Stop();
        m_Controller.Anim.Play(attack.Clip, true);

        if (attack.Lunge > 0f)
            m_Controller.Move.AddImpulse(m_Controller.transform.forward * attack.Lunge);
    }

    public override void Update(float deltaTime)
    {
        // 판정 구간 동안 앞으로 돌진
        AttackSpec attack = m_Controller.CurrentAttack;
        bool isActive = m_Controller.Melee.IsActive(TimeInState);
        if (attack.ActiveMoveSpeed > 0f)
            m_Controller.Move.SetVelocity(isActive ? m_Controller.transform.forward * attack.ActiveMoveSpeed : Vector3.zero);

        m_Controller.Melee.TickAttack(TimeInState);

        if (TimeInState >= m_Length)
            m_Controller.ChangeState(EMonsterState.Chase);
    }

    public override void Exit()
    {
        m_Controller.Move.Stop();
        m_Controller.Melee.End();
        m_Controller.StartAttackCooldown();
    }
}
