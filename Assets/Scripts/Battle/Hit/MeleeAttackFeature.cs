using System.Collections.Generic;
using UnityEngine;

// 판정 프레임에 한 번 구 판정을 해서 HitFeature에 타격을 넘긴다. 한 번에 같은 대상은 한 번만 맞는다.
public class MeleeAttackFeature : MonoBehaviour
{
    [SerializeField] private ETeam m_Team = ETeam.Mob;
    [SerializeField, Tooltip("씬 뷰에 판정 범위 표시")] private AttackSpec m_GizmoSpec = null;

    private static readonly Collider[] s_Buffer = new Collider[32];
    private readonly List<HitFeature> m_ListHitTarget = new();

    // ==================================================

    public void Init(ETeam team)
    {
        m_Team = team;
    }

    // 맞힌 수를 돌려준다
    public int Perform(AttackSpec spec)
    {
        Vector3 center = GetCenter(spec);
        int count = Physics.OverlapSphereNonAlloc(center, spec.Radius, s_Buffer, ~0, QueryTriggerInteraction.Collide);

        m_ListHitTarget.Clear();
        for (int i = 0; i < count; i++)
        {
            HitFeature receiver = s_Buffer[i].GetComponentInParent<HitFeature>();
            if (receiver == null || receiver.gameObject == gameObject)
                continue;

            if (m_ListHitTarget.Contains(receiver))
                continue;

            m_ListHitTarget.Add(receiver);

            Vector3 dir = receiver.transform.position - transform.position;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;

            Vector3 point = s_Buffer[i].ClosestPoint(center);
            HitParams hit = new(gameObject, m_Team, spec.Damage, dir, spec.Knockback, spec.HitStop, point);
            if (receiver.Receive(hit) == false)
                m_ListHitTarget.Remove(receiver);
        }

        return m_ListHitTarget.Count;
    }

    private Vector3 GetCenter(AttackSpec spec)
    {
        return transform.position + Vector3.up * spec.Height + transform.forward * spec.Reach;
    }

    public void SetGizmoSpec(AttackSpec spec)
    {
        m_GizmoSpec = spec;
    }

    private void OnDrawGizmosSelected()
    {
        if (m_GizmoSpec == null)
            return;

        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(GetCenter(m_GizmoSpec), m_GizmoSpec.Radius);
    }
}
