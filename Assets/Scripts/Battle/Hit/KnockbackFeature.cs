using UnityEngine;

// 넉백 반응. 맞은 방향으로 MoveFeature에 순간 속도를 더한다.
[RequireComponent(typeof(MoveFeature))]
public class KnockbackFeature : MonoBehaviour, IHitReaction
{
    [SerializeField, Tooltip("1이면 그대로, 0이면 안 밀림")] private float m_Scale = 1f;
    [SerializeField, Tooltip("위로 살짝 뜨는 양")] private float m_Lift = 0f;

    private MoveFeature m_Mover;

    public int Order => 10;

    // ==================================================

    private void Awake()
    {
        m_Mover = GetComponent<MoveFeature>();
    }

    public void Init(float scale)
    {
        m_Scale = scale;
    }

    public void OnHit(in HitParams hit)
    {
        if (m_Scale <= 0f)
            return;

        m_Mover.AddImpulse(hit.Direction * (hit.Knockback * m_Scale));
        if (m_Lift > 0f)
            m_Mover.AddVerticalSpeed(m_Lift);
    }
}
