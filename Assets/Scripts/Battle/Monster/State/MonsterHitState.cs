// ==================================================

public class MonsterHitState : MonsterState
{
    public MonsterHitState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        m_Controller.Move.Stop();
        m_Controller.Anim.Play("Hit", true);
    }

    public override void Update(float deltaTime)
    {
        if (TimeInState < Def.HitStunTime)
            return;

        m_Controller.ChangeState(Def.FleeTime > 0f ? EMonsterState.Flee : EMonsterState.Chase);
    }
}
