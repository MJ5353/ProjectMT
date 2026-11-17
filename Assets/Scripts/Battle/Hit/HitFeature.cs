using System;
using System.Collections.Generic;
using UnityEngine;

// 맞는 쪽 입구. 팀·무적·사망을 거른 뒤, 넣어 둔 IHitReaction들을 넣은 순서대로 실행한다.
// 새 반응(갓 파괴, 포자 등)은 이 파일을 고치지 않고 AddReaction으로 더한다
public class HitFeature
{
    private readonly ETeam m_Team;
    private readonly float m_InvincibleTime;   // 맞은 뒤 무적 시간(초)
    private readonly HealthFeature m_Health;
    private readonly List<IHitReaction> m_ListReaction = new();

    private float m_InvincibleRemain;

    public ETeam Team => m_Team;
    public bool IsDead => m_Health.IsDead;
    public bool IsInvincible => m_InvincibleRemain > 0f;

    public event Action<HitParams> Hit;

    // ==================================================

    public HitFeature(ETeam team, float invincibleTime, HealthFeature health)
    {
        m_Team = team;
        m_InvincibleTime = invincibleTime;
        m_Health = health;
    }

    public void AddReaction(IHitReaction reaction)
    {
        m_ListReaction.Add(reaction);
    }

    public void Tick(float deltaTime)
    {
        if (m_InvincibleRemain > 0f)
            m_InvincibleRemain -= deltaTime;
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
