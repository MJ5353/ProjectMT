using UnityEngine;

// ==================================================

public class MonsterIdleState : MonsterState
{
    private float m_Duration;

    // ------------------------------------------------

    public MonsterIdleState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        m_Controller.Move.Stop();
        m_Controller.Anim.Play("Idle");
        m_Duration = Random.Range(1f, 2.5f);
    }

    public override void Update(float deltaTime)
    {
        if (m_Controller.CanSeeTarget())
        {
            m_Controller.ChangeState(EMonsterState.Chase);
            return;
        }

        if (TimeInState >= m_Duration)
            m_Controller.ChangeState(EMonsterState.Wander);
    }
}
