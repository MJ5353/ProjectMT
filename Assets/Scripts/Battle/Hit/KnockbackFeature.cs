// 넉백 반응. 맞은 방향으로 MoveFeature에 순간 속도를 더한다.
public class KnockbackFeature : IHitReaction
{
    private readonly MoveFeature m_Move;
    private readonly float m_Scale;   // 1이면 그대로, 0이면 안 밀림

    // ==================================================

    public KnockbackFeature(MoveFeature move, float scale)
    {
        m_Move = move;
        m_Scale = scale;
    }

    public void OnHit(in HitParams hit)
    {
        if (m_Scale <= 0f)
            return;

        m_Move.AddImpulse(hit.Direction * (hit.Knockback * m_Scale));
    }
}
