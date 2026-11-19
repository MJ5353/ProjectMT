// ==================================================

public class MonsterChaseState : MonsterState
{
    public MonsterChaseState(MonsterController controller) : base(controller) { }

    public override void Update(float deltaTime)
    {
        float distance = m_Controller.GetTargetDistance();
        bool isTargetGone = m_Controller.GetValidPlayer() == null;
        if (isTargetGone || distance > Def.LoseRange)
        {
            m_Controller.ChangeState(EMonsterState.Idle);
            return;
        }

        // 쓸 수 있는 공격이 있으면 예고(없으면 바로 공격)
        if (m_Controller.AttackChoice.TryPick(distance))
        {
            bool hasWindup = m_Controller.AttackChoice.Current.WindupTime > 0f;
            m_Controller.ChangeState(hasWindup ? EMonsterState.Windup : EMonsterState.Attack);
            return;
        }

        // 정지 거리 안: 마주 보고 대기
        if (distance <= Def.AttackRange)
        {
            m_Controller.Move.Stop();
            m_Controller.Move.FaceTo(m_Controller.GetTargetDirection());
            m_Controller.Anim.Play("Idle");
            return;
        }

        m_Controller.Anim.Play("Walk");
        m_Controller.MoveTo(m_Controller.GetTargetDirection(), Def.MoveSpeed);
    }
}
