using UnityEngine;

// ==================================================

// 대시. 정한 방향으로 순간 돌아서 일정 시간 빠르게 달리고, 끝난 뒤 쿨을 돈다(쿨은 행동이 아닐 때도 UnitExecutor가 돌린다).
// 생성 때 받는 값은 Def 기본값이고, 시간·쿨 최종값은 쓸 때마다 능력치(StatFeature)에서 읽는다

public class DashAction : UnitAction
{
    private const float MinCooldownRate = 0.2f; // 회피를 아무리 올려도 쿨은 이 비율 밑으로 내려가지 않는다

    private readonly float m_Speed;
    private readonly float m_Duration;      // 대시 지속 시간(초). 기본값
    private readonly float m_Cooldown;      // 대시가 끝난 뒤 다시 쓰기까지. 기본값
    private readonly bool m_IsInvincible;   // 대시 중 무적
    private readonly float m_AnimSpeed;     // 대시 중 달리기 애니 배속

    public override EActionType Type => EActionType.Dash;
    public override bool IsFinished => Time >= m_CurrentDuration;
    public bool IsReady => m_CooldownRemain <= 0f;

    private Vector3 m_Direction;
    private float m_CooldownRemain;
    private float m_CurrentDuration;   // 이번 대시 시간(대시 거리 반영)

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

    // 쿨은 대시 시간까지 포함해서 건다(대시가 끝난 뒤부터 Cooldown만큼). 시간은 대시 거리, 쿨은 회피를 반영한다
    protected override void Enter()
    {
        StatFeature stat = Owner.StatF;
        m_CurrentDuration = stat.Get(EStatType.DashDistance, m_Duration);
        m_CooldownRemain = m_Cooldown * stat.GetReduceRate(EStatType.Dodge, MinCooldownRate) + m_CurrentDuration;

        Owner.MoveF.FaceTo(m_Direction, true);
        Owner.MoveF.SetVelocity(m_Direction * m_Speed);
        Owner.AnimF.SetSpeed(m_AnimSpeed);
        Owner.AnimF.Play(AnimKey.Run, true);

        if (m_IsInvincible)
            Owner.HitF.SetInvincible(m_CurrentDuration);
    }

    public override void Exit()
    {
        Owner.MoveF.Stop();
        Owner.AnimF.SetSpeed(1f);
    }
}
