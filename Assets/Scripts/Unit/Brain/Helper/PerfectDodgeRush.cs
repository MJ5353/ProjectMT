using UnityEngine;

// ==================================================

public enum ERushPhase
{
    None,
    Approach,   // 퍼닷몹 앞까지 돌진하는 중. 클릭은 예약만 하고 도착하면 낸다. 빈틈 타이머는 아직 안 돈다
    Rush,       // 빈틈 타이머가 도는 중. 클릭마다 좌·우 베기
    Finisher,   // 막타를 냈음. 착지(공격 끝)까지
}

// ==================================================

// 퍼펙트 닷지 뒤 반격(러시). PlayerBrain이 들고, 입력은 PlayerBrain이 읽어 여기서 낼 공격만 받는다.
// 흐름: 퍼펙트 닷지 → 전체 슬로우(플레이어만 제 속도) + 무적 → 퍼닷몹(공격자) 앞으로 돌진 → 빈틈 타이머 시작, 클릭마다 퍼닷몹을 노려 좌·우 베기(판정이 끝나면 바로 다음 타)
//      → 마지막 구간(RushFinishWindow) 안의 첫 클릭이 막타. 막타 판정 직전(정점)에 시간을 되돌리고, 착지까지 무적
//      → 마지막 구간에 안 누르면 막타 없이 끝. 퍼닷몹이 죽으면 남은 시간을 마지막 구간으로 당겨 다음 클릭이 막타
// 대상은 판단의 TargetSelector에 넣어 둔다(풀로 돌아갔거나 죽으면 TargetSelector가 무효로 본다)
// 시간은 판단의 deltaTime(플레이어 시간 = 슬로우 중에도 실제 시간)으로 센다. 일시정지면 같이 멈춘다

public class PerfectDodgeRush
{
    private const float InvincibleKeep = 0.1f;                       // 러시 동안 매 프레임 이만큼 무적을 채운다
    private const float FinisherRestoreLead = 4f / AttackSpec.Fps;    // 막타 판정 4프레임 전(도약 정점)에 시간을 되돌린다

    private readonly UnitController m_Owner;
    private readonly PlayerDef m_Def;
    private readonly TargetSelector m_TargetSelector; // 판단의 감지. 러시 대상(퍼닷몹)을 넣어 둔다
    
    private int m_SlashCount;           // 낸 베기 수. 짝수면 좌, 홀수면 우
    private bool m_IsFinisherStarted;   // 막타 공격이 실제로 시작됐는지(예약만 된 상태와 구분)
    private bool m_HasTarget;           // 대상이 살아 있었는지. 죽는 순간을 잡는다
    private float m_ApproachTime;

    public ERushPhase Phase { get; private set; }
    public float Remain { get; private set; }   // 빈틈 타이머 남은 시간

    public bool IsActive => Phase != ERushPhase.None;
    public bool IsApproaching => Phase == ERushPhase.Approach;
    private bool IsTakingClick => Phase == ERushPhase.Approach || Phase == ERushPhase.Rush; // 막타를 내기 전까지만 클릭을 받는다
    public UnitController Target => m_TargetSelector.Target; // 퍼닷몹. 죽었으면 null
    public bool IsFinishWindow => Phase == ERushPhase.Rush && Remain <= m_Def.Rush.FinishWindow;
    private bool CanRush => m_Def.Rush.Left != null && m_Def.Rush.Right != null && m_Def.Rush.Finisher != null;

    // ------------------------------------------------

    public PerfectDodgeRush(UnitController owner, PlayerDef def, TargetSelector targetSelector)
    {
        m_Owner = owner;
        m_Def = def;
        m_TargetSelector = targetSelector;
    }

    // attacker: 퍼펙트 닷지를 일으킨 공격자. 러시 내내 이쪽만 노린다
    public bool TryStart(UnitController attacker)
    {
        if (IsActive)
            return false;

        if (CanRush == false)
        {
            CLogger.Warning(nameof(PerfectDodgeRush), $"{m_Def.name}의 러시 공격(Rush.Left·Right·Finisher)이 비어 있어 러시를 건너뜁니다");
            return false;
        }

        Phase = ERushPhase.Approach;
        Remain = m_Def.Rush.Time;
        m_SlashCount = 0;
        m_IsFinisherStarted = false;
        m_ApproachTime = 0f;

        m_TargetSelector.Clear();
        m_TargetSelector.SetTarget(attacker);
        m_HasTarget = m_TargetSelector.HasTarget;

        TimeManager.Instance.SetSlow(m_Def.Rush.SlowRate);
        m_Owner.UnitExecutor.Attack.CancelDelay = 0f; // 판정이 끝나자마자 다음 타로 끊는다
        m_Owner.HitF.SetInvincible(InvincibleKeep);

        SEventManager.Publish(new RushStarted(m_Owner, m_Def.Rush.Time, m_Def.Rush.FinishWindow));
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
        CheckTargetDied();

        switch (Phase)
        {
            case ERushPhase.Approach:
                TickApproach(deltaTime);
                break;
            case ERushPhase.Rush:
                Remain -= deltaTime;
                if (Remain <= 0f)
                    End(false);
                break;
            case ERushPhase.Finisher:
                TickFinisher();
                break;
        }
    }

    // 퍼닷몹 쪽 조준 방향. 대상이 없으면(죽음) false
    public bool TryGetAimDirection(out Vector3 direction)
    {
        UnitController target = m_TargetSelector.Target;
        if (target == null)
        {
            direction = Vector3.zero;
            return false;
        }

        direction = target.transform.position - m_Owner.transform.position;
        return true;
    }

    // 이번 클릭에 낼 공격. 막타를 이미 냈으면 null(클릭 무시)
    public AttackSpec GetAttack()
    {
        if (IsTakingClick == false)
            return null;

        if (IsFinishWindow)
            return m_Def.Rush.Finisher.Attack;

        return (m_SlashCount % 2 == 0 ? m_Def.Rush.Left : m_Def.Rush.Right).Attack;
    }

    // PlayerBrain이 공격을 예약했을 때. 실제로 받은 클릭만 좌·우 순서와 막타 단계에 반영한다
    public void OnQueued(AttackSpec spec)
    {
        if (IsTakingClick == false)
            return;

        if (spec == m_Def.Rush.Finisher.Attack)
        {
            Phase = ERushPhase.Finisher;
            return;
        }

        m_SlashCount++;
    }

    // ==================================================

    // 대상이 죽는 순간 남은 시간을 마지막 구간으로 당긴다(다음 클릭이 막타). 돌진 중이었으면 그 자리에서 시작
    private void CheckTargetDied()
    {
        if (m_HasTarget == false || m_TargetSelector.HasTarget)
            return;

        m_HasTarget = false;
        Remain = Mathf.Min(Remain, m_Def.Rush.FinishWindow);

        if (Phase == ERushPhase.Approach)
            BeginRush();
    }

    // 대시가 끝나 대기로 돌아오면 대상 앞(베기 사거리 안)까지 달린다. 닿거나 막히면(시간 초과) 빈틈 타이머 시작
    private void TickApproach(float deltaTime)
    {
        UnitExecutor executor = m_Owner.UnitExecutor;
        if (executor.IsIdle == false)
            return;

        UnitController target = m_TargetSelector.Target;
        if (target == null)
        {
            BeginRush();
            return;
        }

        m_ApproachTime += deltaTime;

        Vector3 offset = target.transform.position - m_Owner.transform.position;
        offset.y = 0f;

        float stopDistance = target.HurtBody.radius * target.transform.lossyScale.x + m_Def.Rush.Left.Attack.Reach;
        if (offset.magnitude <= stopDistance || m_ApproachTime >= m_Def.Rush.ApproachMaxTime)
        {
            executor.SetMove(Vector3.zero, 0f);
            m_Owner.MoveF.FaceTo(offset, true);
            BeginRush();
            return;
        }

        executor.SetMove(offset, m_Def.Rush.ApproachSpeed, true);
    }

    private void BeginRush()
    {
        Phase = ERushPhase.Rush;
    }

    // 막타가 시작되면 정점에서 시간을 되돌리고, 막타가 끝나면(다른 행동으로 바뀌면) 러시를 끝낸다
    private void TickFinisher()
    {
        UnitExecutor executor = m_Owner.UnitExecutor;
        AttackSpec finisher = m_Def.Rush.Finisher.Attack;
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
        m_HasTarget = false;
        m_TargetSelector.Clear();

        TimeManager.Instance.SetSlow(1f);
        m_Owner.UnitExecutor.Attack.CancelDelay = m_Def.CancelDelay;

        SEventManager.Publish(new RushEnded(m_Owner, isFinisher));
    }
}
