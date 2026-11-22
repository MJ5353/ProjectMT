using System;
using UnityEngine;

// ==================================================

// 실행기(UnitExecutor). Feature(부품)가 아니라 주인이 직접 들고 Tick 순서대로 돌린다(부품 Tick → 행동 → 판단 → 부품 LateTick).
// 지금 행동 하나(이동·공격·대시·피격·사망)를 돌리고, 판단 쪽(입력·AI)의 요청을 끊기 규칙에 맞춰 받는다.
// 행동은 부품(Move·Anim·Hit 등)을 도구로 쓴다. 행동이 끝나면 이동(대기)으로 돌아가고, 그 뒤 할 일은 판단 쪽이 다시 요청한다

public class UnitExecutor
{
    private readonly UnitController m_Owner;
    
    // action
    private readonly MoveAction m_Move;
    private readonly AttackAction m_Attack;
    private readonly HitAction m_Hit;
    private readonly DeadAction m_Dead;

    // conditional action
    private DashAction m_Dash;   // 대시를 쓰는 유닛만(EnableDash)
    private UnitAction m_Current;

    // 행동이 끝날 때(끝까지 했으면 isFinished, 다른 행동에 끊겼으면 false). 그 뒤 할 일은 판단 쪽이 정한다
    public event Action<EActionType, bool> Ended;

    // get
    public EActionType Current => m_Current.Type;
    public float TimeInAction => m_Current.Time;
    public bool IsIdle => m_Current == m_Move;
    public AttackAction Attack => m_Attack;     // 지금 공격(Spec)·다음 타 버퍼 가능 여부를 볼 때
    public AttackSpec ComboNext => m_Attack.Spec?.NextAttack; // 마지막 공격의 콤보 다음 타(AttackSpec.Next). 없으면 null

    // ------------------------------------------------

    public UnitExecutor(UnitController owner, UnitDef def)
    {
        m_Owner = owner;
        m_Move = new MoveAction(owner);
        m_Attack = new AttackAction(owner, def);
        m_Hit = new HitAction(owner, def.HitStunTime);
        m_Dead = new DeadAction(owner);
        m_Current = m_Move;
    }
    
    public void Tick(float deltaTime)
    {
        m_Dash?.TickCooldown(deltaTime);
        m_Current.Step(deltaTime);
        
        if (m_Current.IsFinished)
            Change(m_Move, true);
    }
    
    public void Reset()
    {
        // 풀에서 꺼낼 때
        m_Dash?.ResetCooldown();
        m_Attack.ClearSpec();
        m_Current = m_Move;
        m_Move.Begin();
    }
    
    public void Clear()
    {
        // 풀로 돌아갈 때
        m_Current.Exit();
        m_Current = m_Move;
    }

    // core --- 
    
    // 다음 행동을 먼저 시작하고 알린다. 알림을 받은 쪽이 곧바로 또 요청해도(콤보) 순서가 꼬이지 않는다
    private void Change(UnitAction next, bool isFinished = false)
    {
        UnitAction prev = m_Current;
        prev.Exit();
        m_Current = next;
        m_Current.Begin();
        Ended?.Invoke(prev.Type, isFinished);
    }
    
    // 하던 행동을 정리하고 대기로(전투 종료 등). 사망은 그대로 둔다
    public void Interrupt()
    {
        if (m_Current != m_Dead)
            Change(m_Move);
    }
    
    // request ---

    public bool CanRequest(EActionType type)
    {
        return m_Current.CanInterruptBy(type);
    }

    // direction이 0이면 지금 보는 방향 그대로. faceTarget이 있으면 예고(WindupTime) 동안 그쪽으로 돈다
    public bool RequestAttack(AttackSpec spec, Vector3 direction, Transform faceTarget = null)
    {
        if (spec == null || CanRequest(EActionType.Attack) == false)
            return false;

        m_Attack.Setup(spec, direction, faceTarget);
        Change(m_Attack);
        return true;
    }

    public bool RequestDash(Vector3 direction)
    {
        if (m_Dash == null || m_Dash.IsReady == false || CanRequest(EActionType.Dash) == false)
            return false;

        m_Dash.Setup(direction);
        Change(m_Dash);
        return true;
    }
    
    public bool RequestHit()
    {
        if (CanRequest(EActionType.Hit) == false)
            return false;

        Change(m_Hit);
        return true;
    }

    public void RequestDead()
    {
        if (CanRequest(EActionType.Dead))
            Change(m_Dead);
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
    
    public void EnableDash(float speed, float duration, float cooldown, bool isInvincible, float animSpeed, float perfectDodgeWindow)
    {
        m_Dash = new DashAction(m_Owner, speed, duration, cooldown, isInvincible, animSpeed, perfectDodgeWindow);
    }
}
