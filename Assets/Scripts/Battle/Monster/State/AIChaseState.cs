using UnityEngine;

// ==================================================

public class AIChaseState : AIState
{
    public AIChaseState(MonsterController controller) : base(controller) { }

    // ------------------------------------------------
    
    public override void Update(float deltaTime)
    {
        float distance = m_Controller.GetTargetDistance();
        bool isTargetGone = m_Controller.Target == null;
        
        if (isTargetGone || distance > Def.LoseRange)
        {
            m_Controller.ChangeState(EAIState.Idle);
            return;
        }

        // 쓸 수 있는 공격이 있으면 공격 행동을 요청한다(예고가 있으면 행동이 움츠리며 대상 쪽으로 돈다)
        if (m_Controller.AttackChoice.TryPick(distance) && 
            m_Controller.Action.RequestAttack(m_Controller.AttackChoice.Current, Vector3.zero, m_Controller.Target))
            return;

        // 정지 거리 안: 마주 보고 대기
        if (distance <= Def.AttackRange)
        {
            m_Controller.Action.SetMove(Vector3.zero, 0f);
            m_Controller.Move.FaceTo(m_Controller.GetTargetDirection());
            return;
        }

        m_Controller.Action.SetMove(m_Controller.GetTargetDirection(), Def.MoveSpeed);
    }
}
