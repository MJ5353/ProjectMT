using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 상태(StateFeature). 코어 하나(CoreState: 몸을 차지하는 일, 끊기 우선순위)와 서브 여러 개(SubState: 함께 도는 일)를 든다.
// 부품 목록이 아니라 주인이 직접 Tick한다(부품 Tick → 상태[서브 → 코어] → 판단 → 부품 LateTick). Def.SetFeatures보다 먼저 만들어져 조건부 코어(대시)를 받는다.
// 코어는 요청(RequestCore)으로만 바뀌고, 끝나면 이동(대기)으로 돌아간다. 그 뒤 할 일은 판단 쪽이 다시 요청한다

public class StateFeature : UnitFeature
{
    // core
    private readonly MoveCore m_Move;
    private readonly AttackCore m_Attack;
    private readonly HitCore m_Hit;
    private readonly DeadCore m_Dead;

    // conditional core
    private DashCore m_Dash;   // 대시를 쓰는 유닛만(EnableDash)
    private CoreState m_Current;

    // sub
    private readonly List<SubState> m_ListSub = new();

    // 코어가 끝날 때(끝까지 했으면 isFinished, 다른 코어에 끊겼으면 false). 그 뒤 할 일은 판단 쪽이 정한다
    public event Action<ECoreState, bool> Ended;
    public event Action<ECoreState> Started; // 코어를 시작할 때(Begin 직후). HUD 공격·대시 아이콘이 받는다

    // get
    public ECoreState Current => m_Current.Type;
    public float TimeInAction => m_Current.Time;
    public bool IsIdle => m_Current == m_Move;
    public AttackCore Attack => m_Attack;     // 지금 공격(Spec)·다음 타 버퍼 가능 여부를 볼 때
    public AttackSpec ComboNext => m_Attack.Spec?.NextAttack; // 마지막 공격의 콤보 다음 타(AttackSpec.Next). 없으면 null

    // ------------------------------------------------

    public StateFeature(UnitController owner, UnitDef def) : base(owner)
    {
        m_Move = new MoveCore(owner);
        m_Attack = new AttackCore(owner, def);
        m_Hit = new HitCore(owner, def.HitStunTime);
        m_Dead = new DeadCore(owner);
        m_Current = m_Move;
    }

    // 서브 → 코어 순서
    public override void Tick(float deltaTime)
    {
        if (m_Dash != null)
            m_Dash.TickCooldown(deltaTime);

        int i = 0;
        while (i < m_ListSub.Count)
        {
            SubState sub = m_ListSub[i];
            sub.Step(deltaTime);

            if (sub.Lifetime == ESubLifetime.Timed && sub.IsFinished)
            {
                m_ListSub.RemoveAt(i);
                sub.Exit();
                continue;
            }

            ++i;
        }

        m_Current.Step(deltaTime);

        if (m_Current.IsFinished)
            Change(m_Move, true);
    }

    // 풀에서 꺼낼 때. 유닛 수명 서브는 여기서 시작한다
    public override void Reset()
    {
        if (m_Dash != null)
            m_Dash.ResetCooldown();

        m_Attack.ClearSpec();
        m_Current = m_Move;
        m_Move.Begin();

        foreach (SubState sub in m_ListSub)
        {
            sub.Begin();
        }
    }

    // 풀로 돌아갈 때. 서브는 모두 Exit하고 Timed는 뺀다
    public override void Clear()
    {
        m_Current.Exit();
        m_Current = m_Move;

        ExitSubs(true);
    }

    // core ---

    // 코어는 여기로만 바뀐다. 끊기 규칙(CanInterruptBy)을 통과해야 한다.
    // 지금은 요청 즉시 바꾼다(호출 쪽이 성공 여부·콤보 연속 요청에 기댄다). 프레임 단위로 모아 우선순위로 고르는 건 서브가 들어오는 묶음 2에서
    private bool RequestCore(CoreState next)
    {
        if (m_Current.CanInterruptBy(next.Type) == false)
            return false;

        Change(next);
        return true;
    }

    // 다음 코어를 먼저 시작하고 알린다. 알림을 받은 쪽이 곧바로 또 요청해도(콤보) 순서가 꼬이지 않는다
    private void Change(CoreState next, bool isFinished = false)
    {
        CoreState prev = m_Current;
        prev.Exit();
        m_Current = next;
        m_Current.Begin();

        for (int i = 0; i < m_ListSub.Count; ++i)
        {
            m_ListSub[i].OnCoreChanged(prev.Type, next.Type);
        }

        Started?.Invoke(m_Current.Type);
        Ended?.Invoke(prev.Type, isFinished);
    }

    // 하던 코어를 정리하고 대기로. 사망은 그대로 둔다
    public void Interrupt()
    {
        if (m_Current != m_Dead)
            Change(m_Move);
    }

    // 전투 종료 등(BrainBase.Stop). 코어를 끊고 Timed 서브를 빼고 제자리에 세운다. 중력만 계속 받는다
    public void Stop()
    {
        Interrupt();
        ExitSubs(false);

        Owner.MoveF.Stop();
        Owner.AnimF.SetSpeed(1f);

        if (Owner.IsDead == false)
            Owner.AnimF.Play(AnimKey.Idle);
    }

    // request ---

    public bool CanRequest(ECoreState type)
    {
        return m_Current.CanInterruptBy(type);
    }

    // direction이 0이면 지금 보는 방향 그대로. faceTarget이 있으면 예고(WindupTime) 동안 그쪽으로 돈다
    public bool RequestAttack(AttackSpec spec, Vector3 direction, Transform faceTarget = null)
    {
        if (spec == null || CanRequest(ECoreState.Attack) == false)
            return false;

        // 끊기 판단은 위에서 지금 공격 기준으로 끝냈다. Setup 뒤에 다시 물으면 새 Spec의 판정 시간으로 재서 거절되고, 도는 공격의 Spec만 바뀐 채 남는다
        m_Attack.Setup(spec, direction, faceTarget);
        Change(m_Attack);
        return true;
    }

    public bool RequestDash(Vector3 direction)
    {
        if (m_Dash == null || m_Dash.IsReady == false || CanRequest(ECoreState.Dash) == false)
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

    // 사망에서 대기로 되돌린다(부활). 사망은 무엇에도 끊기지 않아서 따로 연다
    public void Revive()
    {
        if (m_Current == m_Dead)
            Change(m_Move);
    }

    // 이동(대기) 중일 때만 따른다. 방향은 수평으로 펴서 정규화한다. speed가 0이면 멈춰서 Idle
    public void SetMove(Vector3 direction, float speed, bool isRun = false)
    {
        if (IsIdle == false)
            return;

        direction.y = 0f;
        m_Move.SetMove(direction.normalized * speed, isRun);
    }

    // 대시를 쓰는 유닛만(PlayerDef.SetFeatures)
    public void EnableDash(DashSpec spec)
    {
        m_Dash = new DashCore(Owner, spec);
    }

    // 켜면 공격 판정이 끝나자마자 다음 공격으로 끊는다(캔슬 지연 무시). 퍼펙트 닷지 러시가 켜고 끈다
    public void SetQuickCancel(bool isOn)
    {
        m_Attack.IsQuickCancel = isOn;
    }

    // sub ---

    // Unit 수명은 Def.SetFeatures에서 등록하고 Reset 때 시작한다. Timed는 바로 시작한다
    public void AddSub(SubState sub)
    {
        m_ListSub.Add(sub);

        if (sub.Lifetime == ESubLifetime.Timed)
            sub.Begin();
    }

    public void RemoveSub(SubState sub)
    {
        if (m_ListSub.Remove(sub))
            sub.Exit();
    }

    // isAll이면 유닛 수명 서브도 Exit한다(풀 반환). Timed는 언제나 뺀다
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

    // notify ---

    public void OnHit(in HitParams hit)
    {
        for (int i = 0; i < m_ListSub.Count; ++i)
        {
            m_ListSub[i].OnHit(hit);
        }
    }

    public void OnDied()
    {
        for (int i = 0; i < m_ListSub.Count; ++i)
        {
            m_ListSub[i].OnDied();
        }
    }

    public void OnPerfectDodge(in HitParams hit)
    {
        for (int i = 0; i < m_ListSub.Count; ++i)
        {
            m_ListSub[i].OnPerfectDodge(hit);
        }
    }
}
