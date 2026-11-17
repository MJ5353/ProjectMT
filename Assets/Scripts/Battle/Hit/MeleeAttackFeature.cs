using System;
using System.Collections.Generic;
using UnityEngine;

// 근접 공격 한 종류의 수치. 판정 프레임은 tools/voxel/README.md의 애니 표(30fps)를 따른다.
[Serializable]
public class AttackSpec
{
    public const float Fps = 30f;

    [SerializeField] private string m_Clip = "Attack";
    [SerializeField] private int m_Damage = 1;
    [SerializeField, Tooltip("판정 프레임(30fps)")] private int m_HitFrame = 9;
    [SerializeField, Tooltip("몸 앞쪽으로 판정 중심까지 거리")] private float m_Reach = 1f;
    [SerializeField] private float m_Radius = 0.9f;
    [SerializeField, Tooltip("판정 중심 높이")] private float m_Height = 0.6f;
    [SerializeField] private float m_Knockback = 6f;
    [SerializeField] private float m_HitStop = 0.06f;
    [SerializeField, Tooltip("공격 시작 때 앞으로 내딛는 속도")] private float m_Lunge = 0f;

    public string Clip => m_Clip;
    public int Damage => m_Damage;
    public float HitTime => m_HitFrame / Fps;
    public float Reach => m_Reach;
    public float Radius => m_Radius;
    public float Height => m_Height;
    public float Knockback => m_Knockback;
    public float HitStop => m_HitStop;
    public float Lunge => m_Lunge;

    public AttackSpec() { }

    public AttackSpec(string clip, int damage, int hitFrame, float reach, float radius, float height, float knockback, float hitStop, float lunge)
    {
        m_Clip = clip;
        m_Damage = damage;
        m_HitFrame = hitFrame;
        m_Reach = reach;
        m_Radius = radius;
        m_Height = height;
        m_Knockback = knockback;
        m_HitStop = hitStop;
        m_Lunge = lunge;
    }
}

// 판정 프레임에 한 번 구 판정을 해서 HitFeature에 타격을 넘긴다. 한 번에 같은 대상은 한 번만 맞는다.
public class MeleeAttackFeature : MonoBehaviour
{
    [SerializeField] private ETeam m_Team = ETeam.Mob;
    [SerializeField, Tooltip("씬 뷰에 판정 범위 표시")] private AttackSpec m_GizmoSpec;

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
