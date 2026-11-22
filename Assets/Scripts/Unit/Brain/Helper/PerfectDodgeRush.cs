// ==================================================

public enum ERushPhase
{
    None,
    Rush,       // 빈틈 타이머가 도는 중. 클릭마다 좌·우 베기
    Finisher,   // 막타를 냈음. 착지(공격 끝)까지
}

// ==================================================

// 퍼펙트 닷지 뒤 반격(러시). PlayerBrain이 들고, 입력은 PlayerBrain이 읽어 여기서 낼 공격만 받는다.
// 흐름: 퍼펙트 닷지 → 전체 슬로우(플레이어만 제 속도) + 빈틈 타이머 + 무적 → 클릭마다 좌·우 베기(판정이 끝나면 바로 다음 타)
//      → 마지막 구간(RushFinishWindow) 안의 첫 클릭이 막타. 막타 판정 직전(정점)에 시간을 되돌리고, 착지까지 무적
//      → 마지막 구간에 안 누르면 막타 없이 끝
// 시간은 판단의 deltaTime(플레이어 시간 = 슬로우 중에도 실제 시간)으로 센다. 일시정지면 같이 멈춘다

public class PerfectDodgeRush
{
    private const float InvincibleKeep = 0.1f;                       // 러시 동안 매 프레임 이만큼 무적을 채운다
    private const float FinisherRestoreLead = 4f / AttackSpec.Fps;    // 막타 판정 4프레임 전(도약 정점)에 시간을 되돌린다

    private readonly UnitController m_Owner;
    private readonly PlayerDef m_Def;
    private int m_SlashCount;           // 낸 베기 수. 짝수면 좌, 홀수면 우
    private bool m_IsFinisherStarted;   // 막타 공격이 실제로 시작됐는지(예약만 된 상태와 구분)

    public ERushPhase Phase { get; private set; }
    public float Remain { get; private set; }   // 빈틈 타이머 남은 시간
    public bool IsActive => Phase != ERushPhase.None;
    public bool IsFinishWindow => Phase == ERushPhase.Rush && Remain <= m_Def.RushFinishWindow;
    private bool CanRush => m_Def.RushLeft != null && m_Def.RushRight != null && m_Def.RushFinisher != null;

    // ------------------------------------------------

    public PerfectDodgeRush(UnitController owner, PlayerDef def)
    {
        m_Owner = owner;
        m_Def = def;
    }

    public bool TryStart()
    {
        if (IsActive || CanRush == false)
            return false;

        Phase = ERushPhase.Rush;
        Remain = m_Def.RushTime;
        m_SlashCount = 0;
        m_IsFinisherStarted = false;

        TimeManager.Instance.SetSlow(m_Def.RushSlowRate);
        m_Owner.UnitExecutor.Attack.CancelDelay = 0f; // 판정이 끝나자마자 다음 타로 끊는다
        m_Owner.HitF.SetInvincible(InvincibleKeep);

        SEventManager.Publish(new RushStarted(m_Owner, m_Def.RushTime, m_Def.RushFinishWindow));
        return true;
    }

    // 하던 러시를 접는다(전투 정지·풀 반환). 막타 없이 끝낸 것으로 친다
    public void Cancel()
    {
        if (IsActive)
            End(false);
    }

    public void Tick(float deltaTime)
    {
        if (IsActive == false)
            return;

        m_Owner.HitF.SetInvincible(InvincibleKeep);

        if (Phase == ERushPhase.Rush)
        {
            Remain -= deltaTime;
            if (Remain <= 0f)
                End(false);

            return;
        }

        TickFinisher();
    }

    // 이번 클릭에 낼 공격. 막타를 이미 냈으면 null(클릭 무시)
    public AttackSpec GetAttack()
    {
        if (Phase != ERushPhase.Rush)
            return null;

        if (IsFinishWindow)
            return m_Def.RushFinisher.Attack;

        return (m_SlashCount % 2 == 0 ? m_Def.RushLeft : m_Def.RushRight).Attack;
    }

    // PlayerBrain이 공격을 예약했을 때. 실제로 받은 클릭만 좌·우 순서와 막타 단계에 반영한다
    public void OnQueued(AttackSpec spec)
    {
        if (Phase != ERushPhase.Rush)
            return;

        if (spec == m_Def.RushFinisher.Attack)
        {
            Phase = ERushPhase.Finisher;
            return;
        }

        m_SlashCount++;
    }

    // ==================================================

    // 막타가 시작되면 정점에서 시간을 되돌리고, 막타가 끝나면(다른 행동으로 바뀌면) 러시를 끝낸다
    private void TickFinisher()
    {
        UnitExecutor executor = m_Owner.UnitExecutor;
        AttackSpec finisher = m_Def.RushFinisher.Attack;
        bool isRunning = executor.Current == EActionType.Attack && executor.Attack.Spec == finisher;

        if (m_IsFinisherStarted == false)
        {
            m_IsFinisherStarted = isRunning;
            return;
        }

        if (isRunning == false)
        {
            End(true);
            return;
        }

        if (TimeManager.Instance.IsSlow && executor.Attack.AttackTime >= finisher.HitTime - FinisherRestoreLead)
            TimeManager.Instance.SetSlow(1f);
    }

    private void End(bool isFinisher)
    {
        Phase = ERushPhase.None;
        Remain = 0f;

        TimeManager.Instance.SetSlow(1f);
        m_Owner.UnitExecutor.Attack.CancelDelay = m_Def.CancelDelay;

        SEventManager.Publish(new RushEnded(m_Owner, isFinisher));
    }
}
