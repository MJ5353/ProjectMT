using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 공격 하나의 퍼펙트 닷지 알림(AttackAction이 들고 부른다). 피할 수 있는 적(PerfectDodgeFeature)에게 알려 주기만 하고, 판단은 받는 쪽이 한다.
// 알림: 판정 전 범위 안이면 매 프레임 OnThreatened, 맞으면·공격이 끊기면 OnAttackEnded, 판정이 지나갔는데 안 맞으면 OnAttackMissed.
// 일반 공격은 첫 판정 때 한 번에, 돌진은 판정 구가 그 적 자리를 지나갈 때(못 지나가면 판정 구간 끝) 적마다 알린다

public class AttackDodgeNotifier
{
    private readonly UnitController m_Owner;
    private readonly AttackAction m_Attack; // 받는 쪽이 공격별로 기록하는 열쇠
    private readonly List<UnitHitCandidate> m_ListCandidate = new();
    private readonly HashSet<UnitController> m_SetThreat = new(); // 범위에 들어와 알린 적(아직 판정이 안 지나감)
    private readonly HashSet<UnitController> m_SetDone = new();   // 이번 공격에서 알림을 끝낸 적. 다시 알리지 않는다
    private readonly List<UnitController> m_ListPassed = new();   // 이번에 판정이 지나간 적(순회 중 지우지 않으려고)

    // ------------------------------------------------

    public AttackDodgeNotifier(UnitController owner, AttackAction attack)
    {
        m_Owner = owner;
        m_Attack = attack;
    }

    // 공격 시작
    public void Begin()
    {
        m_SetDone.Clear();
    }

    // 공격이 끝나거나 끊김. 아직 판정이 안 지나간 적의 기록을 지운다
    public void End()
    {
        foreach (UnitController unit in m_SetThreat)
        {
            unit.PerfectDodgeF?.OnAttackEnded(m_Attack);
        }

        m_SetThreat.Clear();
    }

    // 판정이 닿기 전 매 프레임. toHitStart: 판정 시작까지 남은 실제 시간, chargeSpeed: 돌진 속도(일반 공격 0)
    public void Track(in AttackArea area, float toHitStart, float chargeSpeed)
    {
        UnitService units = FlowManager.Instance.Battle?.UnitS;
        if (units == null)
            return;

        units.CollectNearbyEnemies(area.BoundCenter, area.BoundRadius, m_Owner.Team, m_ListCandidate);
        
        foreach (UnitHitCandidate candidate in m_ListCandidate)
        {
            UnitController unit = candidate.Unit;
            CharacterController body = unit.HurtBody;
            
            if (unit.PerfectDodgeF == null || body == null || m_SetDone.Contains(unit))
                continue;

            if (area.Contains(unit.transform.position, body.radius * unit.transform.lossyScale.x, out float distance) == false)
                continue;

            float toHit = toHitStart + (chargeSpeed > 0f ? Mathf.Max(0f, distance) / chargeSpeed : 0f);
            unit.PerfectDodgeF.OnThreatened(m_Attack, toHit);
            m_SetThreat.Add(unit);
        }

        m_ListCandidate.Clear();
    }

    // 맞은 적은 알림을 끝낸다(AttackHitter.TargetHit)
    public void OnTargetHit(UnitController unit)
    {
        if (m_SetThreat.Remove(unit))
            unit.PerfectDodgeF.OnAttackEnded(m_Attack);

        m_SetDone.Add(unit);
    }

    // 판정 뒤: 안 맞은 적에게 알린다. isAll이면 남은 적 모두, 아니면 판정 구가 지나간 적만
    public void Resolve(in AttackArea area, AttackSpec spec, bool isAll)
    {
        if (m_SetThreat.Count == 0)
            return;

        foreach (UnitController unit in m_SetThreat)
        {
            if (isAll || area.HasPassed(unit.transform.position))
                m_ListPassed.Add(unit);
        }

        Transform owner = m_Owner.transform;
        foreach (UnitController unit in m_ListPassed)
        {
            m_SetThreat.Remove(unit);
            m_SetDone.Add(unit);

            Vector3 position = unit.transform.position;
            HitParams hit = new(m_Owner, m_Owner.Team, spec, AttackHitter.GetDirection(owner, position), position, -1);
            unit.PerfectDodgeF.OnAttackMissed(m_Attack, hit);
        }

        m_ListPassed.Clear();
    }
}
