using UnityEngine;

// ==================================================

public class AIChaseStateBase : AIStateBase
{
    public AIChaseStateBase(AIBrain ai) : base(ai) { }

    // ------------------------------------------------
    
    public override void Update(float deltaTime)
    {
        float distance = TargetSelector.GetDistance();
        bool isTargetGone = TargetSelector.HasTarget == false;
        
        if (isTargetGone || distance > Def.LoseRange)
        {
            m_AI.Change(EAIState.Idle);
            return;
        }

        // 쓸 수 있는 공격이 있으면 공격 행동을 요청한다(예고가 있으면 행동이 움츠리며 대상 쪽으로 돈다)
        if (m_AI.AttackSelector.TryPick(distance) && 
            m_Controller.UnitExecutor.RequestAttack(m_AI.AttackSelector.Current, Vector3.zero, TargetSelector.TargetTransform))
            return;

        // 정지 거리 안: 마주 보고 대기
        if (distance <= Def.AttackRange)
        {
            m_Controller.UnitExecutor.SetMove(Vector3.zero, 0f);
            m_Controller.MoveF.FaceTo(TargetSelector.GetDirection());
            return;
        }

        m_Controller.UnitExecutor.SetMove(TargetSelector.GetDirection(), m_AI.MoveSpeed);
    }
}
