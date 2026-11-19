using UnityEngine;

// ==================================================

// 대시 실행. 정한 방향으로 순간 돌아서 일정 시간 빠르게 달리고, 끝난 뒤 쿨을 돈다.
// 언제·어느 방향으로 할지는 주인(입력·AI)이 정한다. 대시가 끝났는지(IsDashing)를 보고 주인이 상태를 바꾼다

public class DashFeature : UnitFeature
{
    private const string DashClip = "Run";

    private readonly float m_Speed;
    private readonly float m_Time;          // 대시 지속 시간(초)
    private readonly float m_Cooldown;      // 대시가 끝난 뒤 다시 쓰기까지
    private readonly bool m_IsInvincible;   // 대시 중 무적
    private readonly float m_AnimSpeed;     // 대시 중 달리기 애니 배속

    public bool IsReady => m_CooldownRemain <= 0f;
    public bool IsDashing => m_DashRemain > 0f;

    // runtime ---

    private float m_DashRemain;
    private float m_CooldownRemain;

    // ------------------------------------------------

    public DashFeature(IActor owner, float speed, float time, float cooldown, bool isInvincible, float animSpeed) : base(owner)
    {
        m_Speed = speed;
        m_Time = time;
        m_Cooldown = cooldown;
        m_IsInvincible = isInvincible;
        m_AnimSpeed = animSpeed;
    }

    public override void Reset()
    {
        m_DashRemain = 0f;
        m_CooldownRemain = 0f;
    }

    public override void Tick(float deltaTime)
    {
        if (m_DashRemain > 0f)
            m_DashRemain -= deltaTime;

        if (m_CooldownRemain > 0f)
            m_CooldownRemain -= deltaTime;
    }

    // 쿨은 대시 시간까지 포함해서 건다(대시가 끝난 뒤부터 Cooldown만큼)
    public void StartDash(Vector3 direction)
    {
        m_DashRemain = m_Time;
        m_CooldownRemain = m_Cooldown + m_Time;

        Owner.Move.FaceTo(direction, true);
        Owner.Move.SetVelocity(direction * m_Speed);
        Owner.Anim.SetSpeed(m_AnimSpeed);
        Owner.Anim.Play(DashClip, true);

        if (m_IsInvincible)
            Owner.HitFeature.SetInvincible(m_Time);
    }
}
