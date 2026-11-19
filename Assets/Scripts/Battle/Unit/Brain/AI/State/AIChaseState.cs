using UnityEngine;

// ==================================================

public class AIChaseState : AIState
{
    public AIChaseState(AIFeature ai) : base(ai) { }

    // ------------------------------------------------
    
    public override void Update(float deltaTime)
    {
        float distance = TargetF.GetDistance();
        bool isTargetGone = TargetF.HasTarget == false;
        
        if (isTargetGone || distance > Def.LoseRange)
        {
            m_AI.Change(EAIState.Idle);
            return;
        }

        // 쓸 수 있는 공격이 있으면 공격 행동을 요청한다(예고가 있으면 행동이 움츠리며 대상 쪽으로 돈다)
        if (m_Controller.AttackChoiceF.TryPick(distance) && 
            m_Controller.ActionF.RequestAttack(m_Controller.AttackChoiceF.Current, Vector3.zero, TargetF.TargetTransform))
            return;

        // 정지 거리 안: 마주 보고 대기
        if (distance <= Def.AttackRange)
        {
            m_Controller.ActionF.SetMove(Vector3.zero, 0f);
            m_Controller.MoveF.FaceTo(TargetF.GetDirection());
            return;
        }

        m_Controller.ActionF.SetMove(TargetF.GetDirection(), Def.MoveSpeed);
    }
}
