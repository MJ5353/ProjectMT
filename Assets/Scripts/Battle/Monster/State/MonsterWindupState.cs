using UnityEngine;

// ==================================================

// 공격 예고. 몸을 납작하게 움츠려서 플레이어가 보고 빠질 수 있게 한다
public class MonsterWindupState : MonsterState
{
    private static readonly Vector3 s_SquashScale = new(1.15f, 0.8f, 1.15f);
    private Vector3 m_BaseScale;

    // ------------------------------------------------

    public MonsterWindupState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        m_Controller.Move.Stop();
        m_Controller.Anim.Play("Idle");
        m_BaseScale = m_Controller.VisualRoot.localScale;
    }

    public override void Update(float deltaTime)
    {
        m_Controller.Move.FaceTo(m_Controller.GetTargetDirection());

        float windupTime = m_Controller.AttackChoice.Current.WindupTime;
        float t = windupTime > 0f ? Mathf.Clamp01(TimeInState / windupTime) : 1f;
        m_Controller.VisualRoot.localScale = Vector3.Scale(m_BaseScale, Vector3.Lerp(Vector3.one, s_SquashScale, t));

        if (t >= 1f)
            m_Controller.ChangeState(EMonsterState.Attack);
    }

    public override void Exit()
    {
        m_Controller.VisualRoot.localScale = m_BaseScale;
    }
}
