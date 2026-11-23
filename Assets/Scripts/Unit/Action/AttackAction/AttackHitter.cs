using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 공격 하나의 판정(AttackAction이 들고 부른다). 판정 구간(HitTime~HitEndTime) 동안 범위(AttackArea)와 후보 유닛의 몸(HitFeature 모양)을 비교해 타격을 넣는다.
// 한 회차(AttackSpec.RehitFrame)에서 맞은 대상은 다시 맞지 않는다. 처음 열릴 때 판정 이펙트를 터뜨린다. 무적 등으로 막히면 구간 안에서 다시 노린다

public class AttackHitter
{
    private readonly UnitController m_Owner;
    private readonly List<UnitHitCandidate> m_ListCandidate = new();
    private readonly HashSet<UnitController> m_SetHit = new(); // 이번 회차에 이미 맞은 대상

    private int m_HitRound; // 다시 맞히기 회차. 바뀌면 맞은 대상 목록을 비운다

    public event Action<UnitController> TargetHit; // 타격이 들어감(퍼펙트 닷지 알림이 받는다)
    public bool IsTicked { get; private set; }     // 판정을 한 번이라도 했는지(한 프레임 판정 보장)

    // ------------------------------------------------

    public AttackHitter(UnitController owner)
    {
        m_Owner = owner;
    }

    // 주인에서 대상 쪽 평면 방향(넉백·퍼펙트 닷지 알림)
    public static Vector3 GetDirection(Transform owner, Vector3 target)
    {
        Vector3 direction = target - owner.position;
        direction.y = 0f;
        
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : owner.forward;
    }

    public void Clear()
    {
        IsTicked = false;
        m_HitRound = 0;
        m_SetHit.Clear();
    }

    // 판정 구간 안인지(time = 클립 시간). 끝 프레임이 시작 이하면 첫 판정 한 프레임만
    public bool IsActive(AttackSpec spec, float time)
    {
        if (time < spec.HitTime)
            return false;

        return IsTicked == false || time <= spec.HitEndTime;
    }

    // 판정 구간이 지났는지
    public bool IsDone(AttackSpec spec, float time)
    {
        return IsTicked && time > spec.HitEndTime;
    }

    // 판정 구간 안 한 프레임. 첫 판정이면 true
    public bool Tick(AttackSpec spec, float time, in AttackArea area)
    {
        bool isFirstTick = IsTicked == false;
        if (isFirstTick)
            PlayEffect(spec, area);

        IsTicked = true;
        if (spec.RehitTime > 0f)
        {
            int round = Mathf.FloorToInt((time - spec.HitTime) / spec.RehitTime);
            if (round != m_HitRound)
            {
                m_HitRound = round;
                m_SetHit.Clear();
            }
        }

        Perform(spec, area);
        return isFirstTick;
    }

    // ==================================================

    // 판정 자리(땅 이펙트는 발밑)에 주인 방향으로 터뜨리고 판정 반지름만큼 키운다
    private void PlayEffect(AttackSpec spec, in AttackArea area)
    {
        EffectService effectS = FlowManager.Instance.EffectS;
        if (spec.Effect == null || effectS == null)
            return;

        Vector3 position = spec.Effect.IsOnGround ? area.Ground : area.Center;
        effectS.Play(spec.Effect, position, m_Owner.transform.rotation, area.Radius);
    }

    // 판정 구와 겹친 적에게 타격을 넣는다
    private void Perform(AttackSpec spec, in AttackArea area)
    {
        UnitService units = FlowManager.Instance.Battle?.UnitS;
        if (units == null)
            return;

        ETeam team = m_Owner.Team;
        Vector3 center = area.Center;
        float radius = area.Radius;
        units.CollectNearbyEnemies(center, radius, team, m_ListCandidate);

        foreach (UnitHitCandidate candidate in m_ListCandidate)
        {
            UnitController unit = candidate.Unit;
            if (m_SetHit.Contains(unit))
                continue;

            CharacterController controller = unit.HurtBody;
            if (controller == null || controller.enabled == false || controller.gameObject.activeInHierarchy == false)
                continue;

            if (candidate.OverlapsSphere(center, radius, out Vector3 point, out int partIndex) == false)
                continue;

            HitParams hit = new(m_Owner, team, spec, GetDirection(m_Owner.transform, unit.transform.position), point, partIndex);
            if (unit.HitF.Receive(hit))
            {
                m_SetHit.Add(unit);
                TargetHit?.Invoke(unit);
            }
        }

        m_ListCandidate.Clear();
    }
}
