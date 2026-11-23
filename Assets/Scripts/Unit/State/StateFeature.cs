using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 상태(StateFeature). 코어 하나(CoreState: 몸을 차지하는 일, 끊기 규칙)와 서브 여러 개(SubState: 함께 도는 일)를 든다.
// 부품 목록이 아니라 주인이 직접 Tick한다(부품 Tick → 상태[코어 Step → 서브(Timed → Unit) → 요청 정리 → 코어 Think] → 부품 LateTick). Def.SetFeatures보다 먼저 만들어져 조건부 코어(대시)·기본 코어·AI 코어를 받는다.
// 코어는 요청으로만 바뀌고, 행동(공격·대시 등)이 끝나면 마지막 기본 층 코어(플레이어 FreeCore, AI는 추적 등)로, 피격이 끝나면 피격 뒤 코어(SetAfterHitCore)로 간다.
// 요청 정리: 서브가 미뤄 둔 요청(Request, 프레임에 하나) → 예약 공격(ReserveAttack, 끊을 수 있게 되면 바로).
// 서브·요청·Think를 코어 Step 다음에 두는 건 판단(AI·입력)이 코어 다음에 돌던 예전 순서 그대로라서다(새 코어는 그 프레임 Begin, 다음 프레임부터 Step)

public class StateFeature : UnitFeature
{
    // core
    private readonly AttackCore m_Attack;
    private readonly HitCore m_Hit;
    private readonly DeadCore m_Dead;

    // conditional core
    private DashCore m_Dash;        // 대시를 쓰는 유닛만(EnableDash)
    private CoreState m_Default;    // 꺼낼 때·멈출 때 들어가는 코어. 플레이어 FreeCore, AI 대기(펫 따라가기). Def.SetFeatures가 정한다(SetDefaultCore)
    private CoreState m_Return;     // 행동이 끝나면 돌아갈 코어: 마지막 기본 층 코어(공격을 낸 추적 등)
    private CoreState m_AfterHit;   // 피격이 끝나면 갈 코어(AIDef.AfterHitCore). 없으면 m_Return
    private CoreState m_Current;
    private readonly Dictionary<ECoreState, CoreState> m_DicCore = new(); // ChangeCore로 바꿀 AI 코어(AddCore)

    // sub
    private readonly List<SubState> m_ListSub = new();
    private bool m_IsStopped;       // Stop ~ Resume. 서브가 빠져 있고 요청을 정리하지 않는다

    // request
    private CoreState m_Deferred;           // 서브가 이번 프레임 미뤄 둔 요청 하나
    private AttackSpec m_ReservedAttack;    // 끊을 수 있게 되면 낼 공격 하나(다음 타 버퍼)
    private Vector3 m_ReservedDirection;
    private Transform m_ReservedFaceTarget;

    public event Action<ECoreState> Started; // 코어를 시작할 때(Begin 직후). HUD 공격·대시 아이콘이 받는다

    // get
    public ECoreState Current => m_Current.Type;
    public float TimeInAction => m_Current.Time;
    public bool IsIdle => m_Current == m_Default;           // 기본 코어에 있을 때
    public AttackCore Attack => m_Attack;     // 지금 공격(Spec)·다음 타 버퍼 가능 여부를 볼 때
    public HitCore Hit => m_Hit;              // 피격 요청 가능 여부를 볼 때(CanRequest)
    public AttackSpec ComboNext => m_Attack.Spec?.NextAttack; // 마지막 공격의 콤보 다음 타(AttackSpec.Next). 없으면 null
    public AttackSpec ReservedAttack => m_ReservedAttack;  // 예약된 공격. 없으면 null

    // ------------------------------------------------

    public StateFeature(UnitController owner, UnitDef def) : base(owner)
    {
        m_Attack = new AttackCore(owner, def);
        m_Hit = new HitCore(owner, def.HitStunTime);
        m_Dead = new DeadCore(owner);
    }

    // 코어 Step → 서브(Timed 먼저: 러시가 그 프레임 상태를 정한 뒤 입력이 읽는다) → 요청 정리 → 코어 Think(AI 판단: 대상·쿨·보스 감시 다음).
    // 히트스톱(deltaTime 0)·멈춤 중에는 서브·요청 정리·Think를 쉰다
    public override void Tick(float deltaTime)
    {
        if (m_Dash != null)
            m_Dash.TickCooldown(deltaTime);

        m_Current.Step(deltaTime);

        if (m_Current.IsFinished)
            Change(GetNextCore(), true);

        if (m_IsStopped || deltaTime <= 0f)
            return;

        TickSubs(ESubLifetime.Timed, deltaTime);
        TickSubs(ESubLifetime.Unit, deltaTime);
        ResolveRequests();

        m_Current.Think(deltaTime);
    }

    // 풀에서 꺼낼 때. 유닛 수명 서브는 여기서 시작한다
    public override void Reset()
    {
        if (m_Dash != null)
            m_Dash.ResetCooldown();

        m_Attack.ClearSpec();
        ClearRequests();
        
        m_IsStopped = false;
        m_Current = m_Default;
        m_Return = m_Default;
        m_Current.Begin();

        for (int i = 0; i < m_ListSub.Count; ++i)
        {
            m_ListSub[i].Begin();
        }
    }

    // 풀로 돌아갈 때. 서브는 모두 Exit하고 Timed는 뺀다
    public override void Clear()
    {
        m_Current.Exit();
        m_Current = m_Default;
        m_Return = m_Default;
        ClearRequests();

        // 멈춘 동안은 유닛 수명 서브도 이미 Exit했다
        ExitSubs(m_IsStopped == false);
    }

    // core ---

    // 코어는 여기로만 바뀐다. 끊기 규칙(CanInterruptBy)을 통과해야 한다. 즉시 바꾼다(AI·피격·사망은 바로 결과가 필요하다)
    private bool RequestCore(CoreState next)
    {
        if (m_Current.CanInterruptBy(next) == false)
            return false;

        Change(next);
        return true;
    }

    // 다음 코어를 먼저 시작하고 알린다(isFinished: 지난 코어가 끝까지 했으면 true, 끊겼으면 false). 서브는 알림 안에서 요청하지 말고 예약한다(ReserveAttack)
    private void Change(CoreState next, bool isFinished = false)
    {
        CoreState prev = m_Current;
        prev.Exit();
        m_Current = next;
        m_Current.Begin();

        if (next.Kind == ECoreKind.Base)
            m_Return = next;

        for (int i = 0; i < m_ListSub.Count; ++i)
        {
            m_ListSub[i].OnCoreChanged(prev.Type, next.Type, isFinished);
        }

        Started?.Invoke(m_Current.Type);
    }

    // AI 코어 사이 이동(판단 코어가 스스로, 보스 감시가 행동이 없을 때). 끊기 규칙 없이 바로 바꾼다. 같은 코어면 다시 시작한다
    public void ChangeCore(ECoreState type)
    {
        Change(GetCore(type));
    }

    // 하던 코어를 정리하고 기본 코어로. 사망은 그대로 둔다
    public void Interrupt()
    {
        if (m_Current != m_Dead)
            Change(m_Default);
    }

    // 전투 정지(UnitService.StopBrains). 코어를 끊어 기본 코어로 두고 서브를 모두 Exit하고(Timed는 뺀다) 제자리에 세운다. 중력만 계속 받는다.
    // 멈춘 동안은 Think가 돌지 않으니 다시 켜면 기본 코어(대기·따라가기)를 처음부터 하는 것과 같다
    public void Stop()
    {
        if (m_IsStopped)
            return;

        m_IsStopped = true;
        
        Interrupt();
        ExitSubs(true);
        ClearRequests();

        Owner.MoveF.Stop();
        Owner.AnimF.SetSpeed(1f);

        if (Owner.IsDead == false)
            Owner.AnimF.Play(AnimKey.Idle);
    }

    // 멈춘 상태를 다시 켠다(UnitService.ResumeBrains). 유닛 수명 서브를 처음부터 시작한다. 죽은 유닛은 멈춘 채 둔다(풀로 돌아가면 Reset이 푼다)
    public void Resume()
    {
        if (m_IsStopped == false || Owner.IsDead)
            return;

        m_IsStopped = false;
        for (int i = 0; i < m_ListSub.Count; ++i)
        {
            m_ListSub[i].Begin();
        }
    }

    // request ---

    public bool CanRequest(CoreState core)
    {
        return m_Current.CanInterruptBy(core);
    }

    // direction이 0이면 지금 보는 방향 그대로. faceTarget이 있으면 예고(WindupTime) 동안 그쪽으로 돈다
    public bool RequestAttack(AttackSpec spec, Vector3 direction, Transform faceTarget = null)
    {
        if (spec == null || CanRequest(m_Attack) == false)
            return false;

        // 끊기 판단은 위에서 지금 공격 기준으로 끝냈다. Setup 뒤에 다시 물으면 새 Spec의 판정 시간으로 재서 거절되고, 도는 공격의 Spec만 바뀐 채 남는다
        m_Attack.Setup(spec, direction, faceTarget);
        Change(m_Attack);
        
        return true;
    }

    public bool RequestDash(Vector3 direction)
    {
        if (m_Dash == null || m_Dash.IsReady == false || CanRequest(m_Dash) == false)
            return false;

        m_Dash.Setup(direction);
        return RequestCore(m_Dash);
    }

    public bool RequestHit()
    {
        return RequestCore(m_Hit);
    }

    public void RequestDead()
    {
        RequestCore(m_Dead);
    }

    // 서브용: 이번 프레임 요청을 미뤄 둔다. 같은 프레임엔 하나만 남는다(먼저 든 요청을 새 요청이 끊을 수 있을 때만 바꾼다). 서브 Tick 뒤에 끊기 규칙으로 정리한다.
    // 코어 설정(Setup)은 부르기 전에 서브가 한다
    public void Request(CoreState core)
    {
        if (m_Deferred == null || m_Deferred.CanInterruptBy(core))
            m_Deferred = core;
    }

    // 다음에 낼 공격 하나를 예약한다(덮어쓴다). 공격을 끊을 수 있게 되면(CanRequest) 그 프레임 서브 Tick 뒤에 낸다
    public void ReserveAttack(AttackSpec spec, Vector3 direction, Transform faceTarget = null)
    {
        m_ReservedAttack = spec;
        m_ReservedDirection = direction;
        m_ReservedFaceTarget = faceTarget;
    }

    public void ClearReservedAttack()
    {
        m_ReservedAttack = null;
        m_ReservedFaceTarget = null;
    }

    // 사망에서 기본 코어로 되돌린다(부활). 사망은 무엇에도 끊기지 않아서 따로 연다
    public void Revive()
    {
        if (m_Current == m_Dead)
            Change(m_Default);
    }

    // 기본 코어를 정한다(PlayerDef: FreeCore, MonsterDef: 대기·따라가기). Reset에서 이 코어로 시작한다
    public void SetDefaultCore(CoreState core)
    {
        m_Default = core;
        m_Current = core;
        m_Return = core;
    }

    // 등록한 AI 코어(AddCore)를 기본으로(MonsterDef.SetAI)
    public void SetDefaultCore(ECoreState type)
    {
        SetDefaultCore(GetCore(type));
    }

    // ChangeCore로 바꿀 AI 코어 등록(MonsterDef.SetAI). 종류마다 하나
    public T AddCore<T>(T core) where T : CoreState
    {
        m_DicCore[core.Type] = core;
        return core;
    }

    // 피격이 끝나면 갈 AI 코어(MonsterDef.SetAI). 정하지 않으면 피격 전 기본 층 코어로 돌아간다
    public void SetAfterHitCore(ECoreState type)
    {
        m_AfterHit = GetCore(type);
    }

    // AI 유닛만(MonsterDef.SetAI). 죽으면 먼지를 내고 충돌을 끈 뒤 사라진다
    public void EnableDespawnOnDead()
    {
        m_Dead.IsDespawn = true;
    }

    // 대시를 쓰는 유닛만(PlayerDef.SetFeatures)
    public void EnableDash(DashSpec spec)
    {
        m_Dash = new DashCore(Owner, spec);
    }

    // 켜면 공격 판정이 끝나자마자 다음 공격으로 끊는다(캔슬 지연 무시). 러시(RushSub)가 켜고 끈다
    public void SetQuickCancel(bool isOn)
    {
        m_Attack.IsQuickCancel = isOn;
    }

    // 미룬 요청 먼저(러시 돌진이 예약 공격을 막는다), 그다음 예약 공격
    private void ResolveRequests()
    {
        if (m_Deferred != null)
        {
            CoreState next = m_Deferred;
            m_Deferred = null;
            
            RequestCore(next);
        }

        if (m_ReservedAttack == null || CanRequest(m_Attack) == false)
            return;

        AttackSpec spec = m_ReservedAttack;
        Transform faceTarget = m_ReservedFaceTarget;
        
        ClearReservedAttack();
        RequestAttack(spec, m_ReservedDirection, faceTarget);
    }

    // 등록하지 않은 종류면 null(AfterHitCore를 비워 둔 Move 등)
    private CoreState GetCore(ECoreState type)
    {
        m_DicCore.TryGetValue(type, out CoreState core);
        return core;
    }

    private CoreState GetNextCore()
    {
        if (m_Current == m_Hit && m_AfterHit != null)
            return m_AfterHit;

        return m_Return;
    }

    private void ClearRequests()
    {
        m_Deferred = null;
        ClearReservedAttack();
    }

    // sub ---

    // Unit 수명은 Def.SetFeatures에서 등록하고 Reset·Resume 때 시작한다. Timed는 바로 시작한다
    public void AddSub(SubState sub)
    {
        m_ListSub.Add(sub);

        if (sub.Lifetime == ESubLifetime.Timed)
            sub.Begin();
    }

    // 서브 Tick·알림을 도는 중에 부르면 터지지는 않지만 그 루프에서 서브 하나를 건너뛸 수 있다. 지금은 부르는 곳이 없다. 알림 안에서 쓰게 되면 루프가 끝난 뒤로 미뤄 뺀다
    public void RemoveSub(SubState sub)
    {
        if (m_ListSub.Remove(sub))
            sub.Exit();
    }

    // T 서브가 지금 들어 있으면 꺼낸다. 할당 없이 목록만 훑는다
    public bool TryGetSub<T>(out T sub) where T : SubState
    {
        foreach (var state in m_ListSub)
        {
            if (state is T found)
            {
                sub = found;
                return true;
            }
        }

        sub = null;
        return false;
    }

    private void TickSubs(ESubLifetime lifetime, float deltaTime)
    {
        int i = 0;
        while (i < m_ListSub.Count)
        {
            SubState sub = m_ListSub[i];
            if (sub.Lifetime != lifetime)
            {
                ++i;
                continue;
            }

            sub.Step(deltaTime);

            if (lifetime == ESubLifetime.Timed && sub.IsFinished)
            {
                m_ListSub.RemoveAt(i);
                sub.Exit();
                
                continue;
            }

            ++i;
        }
    }

    // isAll이면 유닛 수명 서브도 Exit한다(멈춤·풀 반환). Timed는 언제나 뺀다
    private void ExitSubs(bool isAll)
    {
        for (int i = m_ListSub.Count - 1; i >= 0; --i)
        {
            SubState sub = m_ListSub[i];
            if (sub.Lifetime == ESubLifetime.Timed)
            {
                m_ListSub.RemoveAt(i);
                sub.Exit();
            }
            else if (isAll)
            {
                sub.Exit();
            }
        }
    }

    // notify --- 멈춘 동안은 서브가 빠져 있어 알리지 않는다
    // 서브 목록은 인덱스로 돈다: 알림 안에서 AddSub(퍼펙트 닷지 → 러시)해도 foreach처럼 터지지 않는다

    public void OnHit(in HitParams hit)
    {
        if (m_IsStopped)
            return;

        for (int i = 0; i < m_ListSub.Count; ++i)
        {
            m_ListSub[i].OnHit(hit);
        }
    }

    public void OnDied()
    {
        if (m_IsStopped)
            return;

        for (int i = 0; i < m_ListSub.Count; ++i)
        {
            m_ListSub[i].OnDied();
        }
    }

    public void OnPerfectDodge(in HitParams hit)
    {
        if (m_IsStopped)
            return;

        for (int i = 0; i < m_ListSub.Count; ++i)
        {
            m_ListSub[i].OnPerfectDodge(hit);
        }
    }
}
