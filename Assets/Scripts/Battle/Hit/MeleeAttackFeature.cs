using System.Collections.Generic;
using UnityEngine;

// 공격 프레임에 공격 구체와 후보 액터의 몸을 비교한다.
public class MeleeAttackFeature
{
    private readonly IHitTarget m_Owner;
    private readonly ActorManagement m_Actors;
    private readonly List<ActorHitCandidate> m_ListCandidate = new();

    // ==================================================

    public MeleeAttackFeature(IHitTarget owner, ActorManagement actors)
    {
        m_Owner = owner;
        m_Actors = actors;
    }

    public static Vector3 GetCenter(Transform owner, AttackSpec spec)
    {
        return owner.position + Vector3.up * spec.Height + owner.forward * spec.Reach;
    }

    public int Perform(AttackSpec spec)
    {
        if (m_Actors == null)
            return 0;

        Transform owner = m_Owner.transform;
        Vector3 center = GetCenter(owner, spec);
        ETeam team = m_Owner.HitFeature.Team;
        m_Actors.CollectNearbyEnemies(center, spec.Radius, team, m_ListCandidate);

        int hitCount = 0;
        foreach (ActorHitCandidate candidate in m_ListCandidate)
        {
            CharacterController body = candidate.Actor.HurtBody;
            if (body == null || body.enabled == false || body.gameObject.activeInHierarchy == false)
                continue;

            if (candidate.OverlapsSphere(center, spec.Radius, out Vector3 point) == false)
                continue;

            Vector3 direction = candidate.Actor.transform.position - owner.position;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : owner.forward;

            HitParams hit = new(owner.gameObject, team, spec.Damage, direction, spec.Knockback, spec.HitStop, point);
            if (candidate.Actor.HitFeature.Receive(hit))
                hitCount++;
        }

        m_ListCandidate.Clear();
        return hitCount;
    }
}
