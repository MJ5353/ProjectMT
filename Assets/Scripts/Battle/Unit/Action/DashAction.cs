using UnityEngine;

// ==================================================

// 대시. 정한 방향으로 순간 돌아서 일정 시간 빠르게 달리고, 끝난 뒤 쿨을 돈다(쿨은 행동이 아닐 때도 ActionLayer가 돌린다)

public class DashAction : UnitAction
{
    private readonly float m_Speed;
    private readonly float m_Duration;      // 대시 지속 시간(초)
    private readonly float m_Cooldown;      // 대시가 끝난 뒤 다시 쓰기까지
    private readonly bool m_IsInvincible;   // 대시 중 무적
    private readonly float m_AnimSpeed;     // 대시 중 달리기 애니 배속

    public override EActionType Type => EActionType.Dash;
    public override bool IsFinished => Time >= m_Duration;
    public bool IsReady => m_CooldownRemain <= 0f;

    private Vector3 m_Direction;
    private float m_CooldownRemain;

    // ------------------------------------------------

    public DashAction(UnitController owner, float speed, float duration, float cooldown, bool isInvincible, float animSpeed) : base(owner)
    {
        m_Speed = speed;
        m_Duration = duration;
        m_Cooldown = cooldown;
        m_IsInvincible = isInvincible;
        m_AnimSpeed = animSpeed;
    }

    public void Setup(Vector3 direction)
    {
        m_Direction = direction;
    }

    public void ResetCooldown()
    {
        m_CooldownRemain = 0f;
    }

    public void TickCooldown(float deltaTime)
    {
        if (m_CooldownRemain > 0f)
            m_CooldownRemain -= deltaTime;
    }

    // 쿨은 대시 시간까지 포함해서 건다(대시가 끝난 뒤부터 Cooldown만큼)
    protected override void Enter()
    {
        m_CooldownRemain = m_Cooldown + m_Duration;

        Owner.MoveF.FaceTo(m_Direction, true);
        Owner.MoveF.SetVelocity(m_Direction * m_Speed);
        Owner.AnimF.SetSpeed(m_AnimSpeed);
        Owner.AnimF.Play(AnimKey.Run, true);

        if (m_IsInvincible)
            Owner.HitF.SetInvincible(m_Duration);
    }

    public override void Exit()
    {
        Owner.MoveF.Stop();
        Owner.AnimF.SetSpeed(1f);
    }
}
