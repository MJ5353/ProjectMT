using UnityEngine;

// ==================================================

// 주인 따라가기(PetBrain). FollowDistance보다 멀면 주인 쪽으로 걷고(두 배보다 멀면 달리기), 가까우면 멈춘다

public class AIFollowState : AIState<PetBrain>
{
    public AIFollowState(PetBrain ai) : base(ai) { }

    // ------------------------------------------------

    public override void Update(float deltaTime)
    {
        UnitController leader = m_AI.GetLeader();
        if (leader == null)
        {
            m_Controller.ActionLayer.SetMove(Vector3.zero, 0f);
            return;
        }

        Vector3 diff = leader.transform.position - m_Controller.transform.position;
        diff.y = 0f;
        
        float distance = diff.magnitude;
        if (distance <= m_AI.FollowDistance)
        {
            m_Controller.ActionLayer.SetMove(Vector3.zero, 0f);
            return;
        }

        m_Controller.ActionLayer.SetMove(diff, m_AI.MoveSpeed, distance > m_AI.FollowDistance * 2f);
    }
}
