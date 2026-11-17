using System;
using System.Collections.Generic;
using UnityEngine;

// 맞는 쪽 입구. 팀·무적·사망을 거른 뒤, 붙어 있는 IHitReaction들을 Order 순으로 실행한다.
// 새 반응(갓 파괴, 포자 등)은 이 파일을 고치지 않고 컴포넌트만 추가한다.
[DisallowMultipleComponent]
public class HitFeature : MonoBehaviour
{
    [SerializeField] private ETeam m_Team = ETeam.Mob;
    [SerializeField, Tooltip("맞은 뒤 무적 시간(초)")] private float m_InvincibleTime = 0f;

    private readonly List<IHitReaction> m_ListReaction = new();
    private HealthFeature m_Health;
    private float m_InvincibleRemain;

    public ETeam Team => m_Team;
    public bool IsDead => m_Health != null && m_Health.IsDead;
    public bool IsInvincible => m_InvincibleRemain > 0f;

    public event Action<HitParams> Hit;

    // ==================================================

    private void Awake()
    {
        Init();
    }

    private void Update()
    {
        if (m_InvincibleRemain > 0f)
            m_InvincibleRemain -= Time.deltaTime;
    }

    public void Init()
    {
        m_Health = GetComponent<HealthFeature>();
        m_ListReaction.Clear();
        GetComponents(m_ListReaction);
        m_ListReaction.Sort((a, b) => a.Order.CompareTo(b.Order));
        m_InvincibleRemain = 0f;
    }

    // 대시 같은 무적 구간. 남은 무적보다 길 때만 늘린다
    public void SetInvincible(float duration)
    {
        m_InvincibleRemain = Mathf.Max(m_InvincibleRemain, duration);
    }

    // 맞았으면 true
    public bool Receive(in HitParams hit)
    {
        if (hit.Team == m_Team)
            return false;

        if (IsDead || IsInvincible)
            return false;

        foreach (IHitReaction reaction in m_ListReaction)
        {
            reaction.OnHit(hit);
        }

        m_InvincibleRemain = m_InvincibleTime;
        Hit?.Invoke(hit);
        return true;
    }
}
