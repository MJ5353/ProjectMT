using UnityEngine;

// ==================================================

// 주인 따라가기(Pet). FollowDistance보다 멀면 주인 쪽으로 걷고(두 배보다 멀면 달리기), 가까우면 멈춘다

public class AIFollowStateBase : AIStateBase
{
    public AIFollowStateBase(AIBrain ai) : base(ai) { }

    // ------------------------------------------------

    public override void Update(float deltaTime)
    {
        UnitController leader = m_AI.GetLeader();
        if (leader == null)
        {
            m_Controller.UnitExecutor.SetMove(Vector3.zero, 0f);
            return;
        }

        Vector3 diff = leader.transform.position - m_Controller.transform.position;
        diff.y = 0f;
        
        float distance = diff.magnitude;
        if (distance <= Def.FollowDistance)
        {
            m_Controller.UnitExecutor.SetMove(Vector3.zero, 0f);
            return;
        }

        m_Controller.UnitExecutor.SetMove(diff, m_AI.MoveSpeed, distance > Def.FollowDistance * 2f);
    }
}
