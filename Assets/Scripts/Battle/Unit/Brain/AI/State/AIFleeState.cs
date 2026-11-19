using UnityEngine;

// ==================================================

// 버끼처럼 겁 많은 몬스터: 맞으면 반대로 달아났다가 다시 덤빈다

public class AIFleeState : AIState
{
    public AIFleeState(AIFeature ai) : base(ai) { }

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

        Vector3 away = -TargetF.GetDirection();
        m_Controller.ActionF.SetMove(away, Def.FleeSpeed);
    }

    public override void Exit()
    {
        m_Controller.AnimF.SetSpeed(1f);
    }
}
