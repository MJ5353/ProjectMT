using UnityEngine;

// ==================================================

// 포효(Boss: 첫 발견·분노). 멈춰서 대상을 보며 Roar 애니를 한 번 틀고 추적으로 간다

public class AIRoarStateBase : AIStateBase
{
    private const string RoarClip = "Roar";

    private float m_Length;

    // ------------------------------------------------

    public AIRoarStateBase(AIBrain ai) : base(ai) { }

    public override void Enter()
    {
        m_Controller.UnitExecutor.SetMove(Vector3.zero, 0f);
        m_Length = m_Controller.AnimF.GetLength(RoarClip);
        m_Controller.AnimF.Play(RoarClip, true);
    }

    public override void Update(float deltaTime)
    {
        m_Controller.MoveF.FaceTo(TargetSelector.GetDirection());
        if (TimeInState >= m_Length)
            m_AI.Change(EAIState.Chase);
    }
}
