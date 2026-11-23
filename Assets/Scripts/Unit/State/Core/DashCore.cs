using UnityEngine;

// ==================================================

// 대시. 정한 방향으로 순간 돌아서 일정 시간 빠르게 달리고, 끝난 뒤 쿨을 돈다(쿨은 코어가 아닐 때도 StateFeature가 돌린다).
// 생성 때 받는 값은 Def 기본값이고, 시간·쿨 최종값은 쓸 때마다 능력치(StatFeature)에서 읽는다

public class DashCore : CoreState
{
    private readonly DashSpec m_Spec; // Def 기본값(PlayerDef.Dash)

    private Vector3 m_Direction;
    private float m_CooldownRemain;
    private float m_CurrentDuration;   // 이번 대시 시간(대시 거리 반영)

    public override ECoreState Type => ECoreState.Dash;
    public override ECoreKind Kind => ECoreKind.Dash;
    public override ECoreKind InterruptedBy => ECoreKind.Hit | ECoreKind.Dead;
    public override bool IsFinished => Time >= m_CurrentDuration;
    public bool IsReady => m_CooldownRemain <= 0f;

    // ------------------------------------------------

    public DashCore(UnitController owner, DashSpec spec) : base(owner)
    {
        m_Spec = spec;
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
        m_CurrentDuration = stat.Get(EStatType.DashDistance, m_Spec.Time);
        m_CooldownRemain = m_Spec.Cooldown * stat.GetReduceRate(EStatType.Dodge, m_Spec.MinCooldownRate) + m_CurrentDuration;

        Owner.MoveF.FaceToInstant(m_Direction);
        Owner.MoveF.SetVelocity(m_Direction * m_Spec.Speed);
        Owner.AnimF.SetSpeed(m_Spec.AnimSpeed);
        Owner.AnimF.Play(AnimKey.Run, true);

        if (m_Spec.IsInvincible)
            Owner.HitF.SetInvincible(m_CurrentDuration);

        // 무적으로 버티든 범위 밖으로 빠지든, 대시 뒤 창 안에 판정을 안 맞으면 퍼펙트 닷지
        Owner.PerfectDodgeF?.OnDash();
    }

    public override void Exit()
    {
        Owner.MoveF.Stop();
        Owner.AnimF.SetSpeed(1f);
    }
}
