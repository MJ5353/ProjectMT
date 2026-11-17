using System.Collections.Generic;
using UnityEngine;

// 판정 프레임에 한 번 구 판정을 해서 맞힌 대상의 HitFeature에 타격을 넘긴다. 한 번에 같은 대상은 한 번만 맞는다.
public class MeleeAttackFeature
{
    private static readonly Collider[] s_Buffer = new Collider[32];

    private readonly IHitTarget m_Owner;
    private readonly ETeam m_Team;
    private readonly List<IHitTarget> m_ListHitTarget = new();

    // ==================================================

    public MeleeAttackFeature(IHitTarget owner, ETeam team)
    {
        m_Owner = owner;
        m_Team = team;
    }

    public static Vector3 GetCenter(Transform owner, AttackSpec spec)
    {
        return owner.position + Vector3.up * spec.Height + owner.forward * spec.Reach;
    }

    // 맞힌 수를 돌려준다
    public int Perform(AttackSpec spec)
    {
        Transform owner = m_Owner.transform;
        Vector3 center = GetCenter(owner, spec);
        int count = Physics.OverlapSphereNonAlloc(center, spec.Radius, s_Buffer, ~0, QueryTriggerInteraction.Collide);

        m_ListHitTarget.Clear();
        for (int i = 0; i < count; i++)
        {
            // !!) 보류: 콜라이더에서 대상 찾기(GetComponentInParent). 액터 관리 + 좌표 탐색으로 바꿀지 검토
            IHitTarget target = s_Buffer[i].GetComponentInParent<IHitTarget>();
            if (target == null || target.HitFeature == null || target == m_Owner || m_ListHitTarget.Contains(target))
                continue;

            m_ListHitTarget.Add(target);

            Vector3 dir = target.transform.position - owner.position;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : owner.forward;

            Vector3 point = s_Buffer[i].ClosestPoint(center);
            HitParams hit = new(owner.gameObject, m_Team, spec.Damage, dir, spec.Knockback, spec.HitStop, point);
            if (target.HitFeature.Receive(hit) == false)
                m_ListHitTarget.Remove(target);
        }

        return m_ListHitTarget.Count;
    }
}
