using UnityEngine;

// ==================================================

public class AIIdleState : AIState
{
    private float m_Duration;

    // ------------------------------------------------

    public AIIdleState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        m_Controller.ActionF.SetMove(Vector3.zero, 0f);
        m_Duration = Random.Range(1f, 2.5f);
    }

    public override void Update(float deltaTime)
    {
        if (m_Controller.CanSeeTarget())
        {
            m_Controller.ChangeState(EAIState.Chase);
            return;
        }

        if (TimeInState >= m_Duration)
            m_Controller.ChangeState(EAIState.Wander);
    }
}
