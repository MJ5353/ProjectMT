using UnityEngine;

// ==================================================

public class AIWanderState : MonsterState
{
    private const float MaxDuration = 3f;
    private Vector3 m_Destination;

    // ------------------------------------------------

    public AIWanderState(MonsterBrain ai) : base(ai) { }

    public override void Enter()
    {
        Vector2 offset = Random.insideUnitCircle * Def.WanderRadius;
        m_Destination = m_AI.HomePosition + new Vector3(offset.x, 0f, offset.y);
    }

    public override void Update(float deltaTime)
    {
        if (Target.IsInRange(Def.SightRange))
        {
            m_AI.Change(EAIState.Chase);
            return;
        }

        Vector3 diff = m_Destination - m_Controller.transform.position;
        diff.y = 0f;
        if (diff.magnitude < 0.3f || TimeInState >= MaxDuration)
        {
            m_AI.Change(EAIState.Idle);
            return;
        }

        m_Controller.ActionLayer.SetMove(diff, Def.WanderSpeed);
    }
}
