using UnityEngine;

// ==================================================

// 버끼처럼 겁 많은 몬스터: 맞으면 반대로 달아났다가 다시 덤빈다

public class AIFleeStateBase : AIStateBase
{
    public AIFleeStateBase(AIBrain ai) : base(ai) { }

    // ------------------------------------------------
    
    public override void Enter()
    {
        m_Controller.AnimF.SetSpeed(1.6f);
    }

    public override void Update(float deltaTime)
    {
        if (TimeInState >= Def.FleeTime)
        {
            m_AI.Change(EAIState.Chase);
            return;
        }

        Vector3 away = -Target.GetDirection();
        m_Controller.ActionLayer.SetMove(away, Def.FleeSpeed);
    }

    public override void Exit()
    {
        m_Controller.AnimF.SetSpeed(1f);
    }
}
