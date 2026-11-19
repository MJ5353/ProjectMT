using UnityEngine;

// ==================================================

public class AIWanderState : AIState
{
    private const float MaxDuration = 3f;
    private Vector3 m_Destination;

    // ------------------------------------------------

    public AIWanderState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        Vector2 offset = Random.insideUnitCircle * Def.WanderRadius;
        m_Destination = m_Controller.HomePosition + new Vector3(offset.x, 0f, offset.y);
    }

    public override void Update(float deltaTime)
    {
        if (m_Controller.CanSeeTarget())
        {
            m_Controller.ChangeState(EAIState.Chase);
            return;
        }

        Vector3 diff = m_Destination - m_Controller.transform.position;
        diff.y = 0f;
        if (diff.magnitude < 0.3f || TimeInState >= MaxDuration)
        {
            m_Controller.ChangeState(EAIState.Idle);
            return;
        }

        m_Controller.Action.SetMove(diff, Def.WanderSpeed);
    }
}
