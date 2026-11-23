using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 몬스터 공격 하나의 퍼펙트 닷지 판정. AttackAction이 시작(Clear)·판정 전 매 프레임(Track)·맞힘(OnHit)·판정 뒤(Resolve)에 부른다.
// 판정이 닿기 전까지 범위(AttackArea)에 든 적과 그때 판정까지 남은 시간을 적어 두고, 안 맞은 채로 판정이 지나가면 그 적에게 묻는다.
// 대시를 했는지·창 안인지는 묻는 쪽(HitFeature.TryPerfectDodge)이 본다
// 일반 공격은 첫 판정 때 한 번에, 돌진은 판정 구가 그 적 자리를 지나갈 때(못 지나가면 판정 구간이 끝날 때) 적마다 정한다

public class PerfectDodgeTracker
{
    private readonly UnitController m_Owner;
    private readonly Dictionary<UnitController, float> m_DictThreat = new(); // 범위에 들어온 적 → 들어와 있던 가장 늦은 시점의 판정까지 남은 시간
    private readonly HashSet<UnitController> m_SetResolved = new();         // 이번 공격에서 이미 정한 적(맞았거나 물었거나). 다시 적지 않는다
    private readonly List<UnitController> m_ListResolve = new();            // 이번에 정할 적(사전 순회 중 지우지 않으려고)
    private readonly List<UnitHitCandidate> m_ListCandidate = new();

    // ------------------------------------------------

    public PerfectDodgeTracker(UnitController owner)
    {
        m_Owner = owner;
    }

    public void Clear()
    {
        m_DictThreat.Clear();
        m_SetResolved.Clear();
    }

    // 판정이 닿기 전 매 프레임. toHitStart: 판정 구가 지금 자리에서 판정을 시작하기까지 남은 실제 시간, speed: 돌진 속도(일반 공격 0)
    // 남은 시간은 매 프레임 덮어써서, 판정 직전까지 범위 안에 있었을수록 작다
    public void Track(in AttackArea area, float toHitStart, float speed)
    {
        UnitService units = FlowManager.Instance.Battle?.UnitS;
        if (units == null)
            return;

        units.CollectNearbyEnemies(area.BoundCenter, area.BoundRadius, m_Owner.Team, m_ListCandidate);
        foreach (UnitHitCandidate candidate in m_ListCandidate)
        {
            UnitController unit = candidate.Unit;
            CharacterController body = unit.HurtBody;
            if (body == null || m_SetResolved.Contains(unit))
                continue;

            if (area.Contains(unit.transform.position, body.radius * unit.transform.lossyScale.x, out float distance))
                m_DictThreat[unit] = toHitStart + (speed > 0f ? Mathf.Max(0f, distance) / speed : 0f);
        }

        m_ListCandidate.Clear();
    }

    // 맞았으니 퍼펙트 닷지 아님
    public void OnHit(UnitController unit)
    {
        m_SetResolved.Add(unit);
        m_DictThreat.Remove(unit);
    }

    // 판정 뒤. isAll이면 남은 적 모두, 아니면 판정 구 중심이 지나간 적만 묻는다
    public void Resolve(in AttackArea area, AttackSpec spec, bool isAll)
    {
        foreach (UnitController unit in m_DictThreat.Keys)
        {
            if (isAll || area.HasPassed(unit.transform.position))
                m_ListResolve.Add(unit);
        }

        foreach (UnitController unit in m_ListResolve)
        {
            Ask(unit, spec, m_DictThreat[unit]);
            m_DictThreat.Remove(unit);
        }

        m_ListResolve.Clear();
    }

    // ==================================================

    private void Ask(UnitController unit, AttackSpec spec, float threatLead)
    {
        m_SetResolved.Add(unit);

        Transform owner = m_Owner.transform;
        Vector3 direction = unit.transform.position - owner.position;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : owner.forward;

        HitParams hit = new(m_Owner, m_Owner.Team, spec, direction, unit.transform.position, -1);
        unit.HitF.TryPerfectDodge(hit, threatLead);
    }
}
