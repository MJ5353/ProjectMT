using UnityEngine;

// ==================================================

public enum ERushPhase
{
    None,
    Approach,   // 퍼닷몹 앞까지 돌진하는 중(RushApproachCore). 클릭은 예약만 하고 도착하면 낸다. 빈틈 타이머는 아직 안 돈다
    Rush,       // 빈틈 타이머가 도는 중. 클릭마다 좌·우 베기
    Finisher,   // 막타를 냈음. 착지(공격 끝)까지
}

// ==================================================

// 퍼펙트 닷지 뒤 반격(러시, Timed 서브). PlayerInputSub가 한 벌을 들고 퍼펙트 닷지 때 시작하며, 입력은 PlayerInputSub가 읽어 여기서 낼 공격만 받는다.
// 흐름: 퍼펙트 닷지 → 전체 슬로우(플레이어만 제 속도) + 무적 → 퍼닷몹(공격자) 앞으로 돌진(RushApproachCore) → 빈틈 타이머 시작, 클릭마다 퍼닷몹을 노려 좌·우 베기(판정이 끝나면 바로 다음 타)
//      → 마지막 구간(RushFinishWindow) 안의 첫 클릭이 막타. 막타 판정 직전(정점)에 시간을 되돌리고, 착지까지 무적
//      → 마지막 구간에 안 누르면 막타 없이 끝. 퍼닷몹이 죽으면 남은 시간을 마지막 구간으로 당겨 다음 클릭이 막타
// 끝나면(IsFinished) StateFeature가 빼고 Exit한다. 멈춤·풀 반환으로 빠질 때도 Exit에서 슬로우·빠른 캔슬·예약을 정리한다
// 시간은 플레이어 deltaTime(슬로우 중에도 실제 시간)으로 센다. 일시정지·히트스톱이면 같이 멈춘다

public class RushSub : SubState
{
    private const float InvincibleKeep = 0.1f;                       // 러시 동안 매 프레임 이만큼 무적을 채운다
    private const float FinisherRestoreLead = 4f / AttackSpec.Fps;    // 막타 판정 4프레임 전(도약 정점)에 시간을 되돌린다

    private readonly PlayerDef m_Def;
    private readonly RushApproachCore m_Approach;
    private readonly TargetSelector m_TargetSelector; // 러시 대상(퍼닷몹). 죽거나 풀로 돌아가면 무효로 본다

    private UnitController m_Attacker;  // TryStart ~ Enter 사이에만
    private int m_SlashCount;           // 낸 베기 수. 짝수면 좌, 홀수면 우
    private bool m_IsFinisherStarted;   // 막타 공격이 실제로 시작됐는지(예약만 된 상태와 구분)
    private bool m_HasTarget;           // 대상이 살아 있었는지. 죽는 순간을 잡는다
    private bool m_IsApproachRequested; // 돌진 코어를 요청했는지. 그 뒤 기본 코어로 돌아오면 도착(또는 막힘)

    public override ESubLifetime Lifetime => ESubLifetime.Timed;
    public override bool IsFinished => Phase == ERushPhase.None;

    public ERushPhase Phase { get; private set; }
    public float Remain { get; private set; }   // 빈틈 타이머 남은 시간

    public bool IsActive => Phase != ERushPhase.None;
    public bool IsApproaching => Phase == ERushPhase.Approach;
    private bool IsTakingClick => Phase == ERushPhase.Approach || Phase == ERushPhase.Rush; // 막타를 내기 전까지만 클릭을 받는다
    public UnitController Target => m_TargetSelector.Target; // 퍼닷몹. 죽었으면 null
    public bool IsFinishWindow => Phase == ERushPhase.Rush && Remain <= m_Def.Rush.FinishWindow;
    private bool CanRush => m_Def.Rush.Left != null && m_Def.Rush.Right != null && m_Def.Rush.Finisher != null;

    // ------------------------------------------------

    public RushSub(UnitController owner, PlayerDef def, RushApproachCore approach) : base(owner)
    {
        m_Def = def;
        m_Approach = approach;
        m_TargetSelector = new TargetSelector(owner);
    }

    // attacker: 퍼펙트 닷지를 일으킨 공격자. 러시 내내 이쪽만 노린다
    public bool TryStart(UnitController attacker)
    {
        if (IsActive)
            return false;

        if (CanRush == false)
        {
            CLogger.Warning(nameof(RushSub), $"{m_Def.name}의 러시 공격(Rush.Left·Right·Finisher)이 비어 있어 러시를 건너뜁니다");
            return false;
        }

        m_Attacker = attacker;
        Owner.StateF.AddSub(this);
        return true;
    }

    protected override void Enter()
    {
        Phase = ERushPhase.Approach;
        Remain = m_Def.Rush.Time;
        m_SlashCount = 0;
        m_IsFinisherStarted = false;
        m_IsApproachRequested = false;

        m_TargetSelector.Clear();
        m_TargetSelector.SetTarget(m_Attacker);
        m_HasTarget = m_TargetSelector.HasTarget;
        m_Attacker = null;

        TimeManager.Instance.SetSlow(ESlowReason.Rush, m_Def.Rush.SlowRate);
        Owner.StateF.SetQuickCancel(true); // 판정이 끝나자마자 다음 타로 끊는다
        Owner.HitF.SetInvincible(InvincibleKeep);

        SEventManager.Publish(new RushStarted(Owner, this, m_Def.Rush.Time, m_Def.Rush.FinishWindow));
    }

    // 끝까지 못 가고 빠질 때(멈춤·풀 반환). 막타 없이 끝낸 것으로 친다
    public override void Exit()
    {
        if (IsActive)
            End(false);
    }

    protected override void Tick(float deltaTime)
    {
        Owner.HitF.SetInvincible(InvincibleKeep);
        CheckTargetDied();

        switch (Phase)
        {
            case ERushPhase.Approach:
                TickApproach();
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

        direction = target.transform.position - Owner.transform.position;
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

    // PlayerInputSub가 공격을 예약했을 때. 실제로 받은 클릭만 좌·우 순서와 막타 단계에 반영한다
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

    // ---
    
    // 대상이 죽는 순간 남은 시간을 마지막 구간으로 당긴다(다음 클릭이 막타). 돌진 중이었으면 그 자리에서 시작(돌진 코어도 같은 프레임에 끝난다)
    private void CheckTargetDied()
    {
        if (m_HasTarget == false || m_TargetSelector.HasTarget)
            return;

        m_HasTarget = false;
        Remain = Mathf.Min(Remain, m_Def.Rush.FinishWindow);

        if (Phase == ERushPhase.Approach)
            BeginRush();
    }

    // 대시가 끝나 기본 코어로 돌아오면 대상 앞(베기 사거리 안)까지 돌진 코어로 달린다. 이미 닿아 있거나, 돌진이 끝나 돌아오면 빈틈 타이머 시작
    private void TickApproach()
    {
        StateFeature state = Owner.StateF;
        if (state.IsIdle == false)
            return;

        UnitController target = m_TargetSelector.Target;
        if (target == null || m_IsApproachRequested)
        {
            BeginRush();
            return;
        }

        float reach = m_Def.Rush.Left.Attack.Reach;
        if (RushApproachCore.IsInReach(Owner, target, reach))
        {
            Owner.MoveF.FaceToInstant(RushApproachCore.GetOffset(Owner, target));
            BeginRush();
            return;
        }

        m_Approach.Setup(target, reach, m_Def.Rush.ApproachSpeed, m_Def.Rush.ApproachMaxTime);
        state.Request(m_Approach);
        m_IsApproachRequested = true;
    }

    private void BeginRush()
    {
        Phase = ERushPhase.Rush;
    }

    // 막타가 시작되면 정점에서 시간을 되돌리고, 막타가 끝나면(다른 코어로 바뀌면) 러시를 끝낸다
    private void TickFinisher()
    {
        StateFeature state = Owner.StateF;
        AttackSpec finisher = m_Def.Rush.Finisher.Attack;
        bool isRunning = state.Current == ECoreState.Attack && state.Attack.Spec == finisher;

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

        // [mj] todo) 슬로우 이유가 Rush 말고 더 생기면 TimeManager.IsSlowBy(ESlowReason) 추가해 IsSlow 대신 IsSlowBy(ESlowReason.Rush)로 본다
        if (TimeManager.Instance.IsSlow && state.Attack.AttackTime >= finisher.HitTime - FinisherRestoreLead)
            TimeManager.Instance.ClearSlow(ESlowReason.Rush);
    }

    // 러시가 끝난 프레임에는 남은 러시 예약도 버린다
    private void End(bool isFinisher)
    {
        Phase = ERushPhase.None;
        Remain = 0f;
        m_HasTarget = false;
        m_TargetSelector.Clear();

        TimeManager.Instance.ClearSlow(ESlowReason.Rush);
        Owner.StateF.SetQuickCancel(false);
        Owner.StateF.ClearReservedAttack();

        SEventManager.Publish(new RushEnded(Owner, isFinisher));
    }
}
