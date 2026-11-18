using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 근접 공격 판정. Begin으로 공격을 걸고, 매 프레임 Tick(공격 시작부터 지난 시간)을 부르면
// 판정 구간(HitTime~HitEndTime) 동안 공격 구체와 후보 액터의 몸을 비교한다. 한 공격에서 맞은 대상은 다시 맞지 않는다.

public class MeleeAttackFeature : UnitFeature
{
    private readonly List<ActorHitCandidate> m_ListCandidate = new();
    private readonly HashSet<IHitTarget> m_SetHit = new(); // 이번 공격에 이미 맞은 대상

    public AttackSpec Spec => m_Spec;
    private AttackSpec m_Spec;
    
    // 판정을 한 번이라도 했는지(한 프레임 판정 보장)
    private bool m_IsTicked; 

    // ------------------------------------------------

    public MeleeAttackFeature(IActor owner) : base(owner) { }

    public override void Reset()
    {
        End();
    }

    public override void Clear()
    {
        End();
    }

    public static Vector3 GetCenter(Transform owner, AttackSpec spec)
    {
        return owner.position + Vector3.up * spec.Height + owner.forward * spec.Reach;
    }

    public void Begin(AttackSpec spec)
    {
        m_Spec = spec;
        m_IsTicked = false;
        m_SetHit.Clear();
    }

    public void End()
    {
        m_Spec = null;
        m_IsTicked = false;
        m_SetHit.Clear();
    }

    // 판정 구간 안인지. 끝 프레임이 시작 이하면 첫 판정 한 프레임만
    public bool IsActive(float time)
    {
        if (m_Spec == null || time < m_Spec.HitTime)
            return false;

        return m_IsTicked == false || time <= m_Spec.HitEndTime;
    }

    // 판정 구간이 지났는지(걸린 공격이 없어도 true)
    public bool IsDone(float time)
    {
        return m_Spec == null || (m_IsTicked && time > m_Spec.HitEndTime);
    }

    // 공격 시작부터 지난 시간. 판정 구간이면 판정하고 새로 맞힌 수를 돌려준다
    public int Tick(float time)
    {
        if (IsActive(time) == false)
            return 0;

        m_IsTicked = true;
        return Perform(m_Spec);
    }

    // ==================================================

    private int Perform(AttackSpec spec)
    {
        Transform owner = Owner.transform;
        ActorService actors = GameManager.Instance?.Actors;

        if (actors == null)
            return 0;

        Vector3 center = GetCenter(owner, spec);
        ETeam team = Owner.HitFeature.Team;
        actors.CollectNearbyEnemies(center, spec.Radius, team, m_ListCandidate);

        int hitCount = 0;
        foreach (ActorHitCandidate candidate in m_ListCandidate)
        {
            if (m_SetHit.Contains(candidate.Actor))
                continue;

            CharacterController controller = candidate.Actor.HurtBody;
            if (controller == null || controller.enabled == false || controller.gameObject.activeInHierarchy == false)
                continue;

            if (candidate.OverlapsSphere(center, spec.Radius, out Vector3 point) == false)
                continue;

            Vector3 direction = candidate.Actor.transform.position - owner.position;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : owner.forward;

            // 무적 등으로 막히면 구간 안에서 다시 노린다
            HitParams hit = new(owner.gameObject, team, spec.Damage, direction, spec.Knockback, spec.HitStop, point);
            if (candidate.Actor.HitFeature.Receive(hit) == false)
                continue;

            m_SetHit.Add(candidate.Actor);
            hitCount++;
        }

        m_ListCandidate.Clear();
        return hitCount;
    }
}
