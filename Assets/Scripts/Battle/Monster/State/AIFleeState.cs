using UnityEngine;

// ==================================================

// 버끼처럼 겁 많은 몬스터: 맞으면 반대로 달아났다가 다시 덤빈다

public class AIFleeState : AIState
{
    public AIFleeState(MonsterController controller) : base(controller) { }

    // ------------------------------------------------
    
    public override void Enter()
    {
        m_Controller.Anim.SetSpeed(1.6f);
    }

    public override void Update(float deltaTime)
    {
        if (TimeInState >= Def.FleeTime)
        {
            m_Controller.ChangeState(EAIState.Chase);
            return;
        }

        Vector3 away = -m_Controller.GetTargetDirection();
        m_Controller.Action.SetMove(away, Def.FleeSpeed);
    }

    public override void Exit()
    {
        m_Controller.Anim.SetSpeed(1f);
    }
}
