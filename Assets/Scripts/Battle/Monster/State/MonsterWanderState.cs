using UnityEngine;

// ==================================================

public class MonsterWanderState : MonsterState
{
    private const float MaxDuration = 3f;
    private Vector3 m_Destination;

    // ------------------------------------------------

    public MonsterWanderState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        Vector2 offset = Random.insideUnitCircle * Def.WanderRadius;
        m_Destination = m_Controller.HomePosition + new Vector3(offset.x, 0f, offset.y);
        m_Controller.Anim.Play("Walk");
    }

    public override void Update(float deltaTime)
    {
        if (m_Controller.CanSeeTarget())
        {
            m_Controller.ChangeState(EMonsterState.Chase);
            return;
        }

        Vector3 diff = m_Destination - m_Controller.transform.position;
        diff.y = 0f;
        if (diff.magnitude < 0.3f || TimeInState >= MaxDuration)
        {
            m_Controller.ChangeState(EMonsterState.Idle);
            return;
        }

        m_Controller.MoveTo(diff, Def.WanderSpeed);
    }
}
