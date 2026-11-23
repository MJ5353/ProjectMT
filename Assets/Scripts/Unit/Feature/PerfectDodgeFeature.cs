using System;
using System.Collections.Generic;

// ==================================================

// 퍼펙트 닷지(피하는 쪽). 플레이어만 붙는다(PlayerDef.SetFeatures → UnitController.SetPerfectDodgeFeature).
// 몬스터 공격(AttackCore)은 알려 주기만 한다: 범위 안에 들어옴(판정까지 남은 시간) · 판정에 맞음 · 판정이 지나갔는데 안 맞음 · 공격이 중간에 끊김.
// 기록과 판단은 여기서 한다. 조건: 판정 전 창(Window) 안에 그 공격 범위에 있었고, 대시한 지 창 안이며, 안 맞음 → PerfectDodged. 대시 한 번에 한 번

public class PerfectDodgeFeature : UnitFeature
{
    public event Action<HitParams> PerfectDodged; // 반격(러시)·패시브는 주인이 이어받는다(UnitController.OnPerfectDodged)

    private readonly float m_Window; // 판정 창(초, PlayerDef.Dash.PerfectDodgeWindow). 0이면 끔
    private readonly Dictionary<AttackCore, float> m_DictThreat = new(); // 범위에 든 공격 → 들어와 있던 가장 늦은 시점의 판정까지 남은 시간
    private float m_DashRemain; // 대시 뒤 창 남은 시간. 한 번 터지면 0

    // ------------------------------------------------

    public PerfectDodgeFeature(UnitController owner, float window) : base(owner)
    {
        m_Window = window;
    }

    public override void Reset()
    {
        m_DashRemain = 0f;
        m_DictThreat.Clear();
    }

    public override void Clear()
    {
        m_DictThreat.Clear();
    }

    // 무적은 게임 시간 기준(슬로우 무시 유닛은 제 시간). 히트스톱 중에는 같이 멈춘다
    public override void Tick(float deltaTime)
    {
        if (m_DashRemain > 0f)
            m_DashRemain -= deltaTime;
    }

    // 대시가 부른다(DashCore.Enter)
    public void OnDash()
    {
        m_DashRemain = m_Window;
    }

    // 공격 알림 ---

    // 판정 전 그 공격 범위 안에 있음. 매 프레임 덮어써서 판정 직전까지 있었을수록 작다
    public void OnThreatened(AttackCore attack, float toHit)
    {
        m_DictThreat[attack] = toHit;
    }

    // 맞았거나 공격이 끊겼으면 기록만 지운다
    public void OnAttackEnded(AttackCore attack)
    {
        m_DictThreat.Remove(attack);
    }

    // 판정이 지나갔는데 안 맞음
    public void OnAttackMissed(AttackCore attack, in HitParams hit)
    {
        if (m_DictThreat.Remove(attack, out float toHit) == false)
            return;

        if (Owner.IsDead || Owner.HitF.IsHidden || m_DashRemain <= 0f || toHit > m_Window)
            return;

        m_DashRemain = 0f;
        PerfectDodged?.Invoke(hit);
    }
}
