using UnityEngine;

// ==================================================

public class AIIdleStateBase : AIStateBase
{
    private float m_Duration;

    // ------------------------------------------------

    public AIIdleStateBase(AIBrain ai) : base(ai) { }

    public override void Enter()
    {
        m_Controller.ActionLayer.SetMove(Vector3.zero, 0f);
        m_Duration = Random.Range(1f, 2.5f);
    }

    public override void Update(float deltaTime)
    {
        if (Target.IsInRange(Def.SightRange))
        {
            m_AI.Change(EAIState.Chase);
            return;
        }

        if (TimeInState >= m_Duration)
            m_AI.Change(EAIState.Wander);
    }
}
