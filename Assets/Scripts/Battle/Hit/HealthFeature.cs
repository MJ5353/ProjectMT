using System;
using UnityEngine;

// 체력 반응. 피격 반응 중 가장 먼저 넣는다
public class HealthFeature : IHitReaction
{
    private readonly int m_MaxHp;

    public int MaxHp => m_MaxHp;
    public int Current { get; private set; }
    public bool IsDead => Current <= 0;

    public event Action Died;

    // ==================================================

    public HealthFeature(int maxHp)
    {
        m_MaxHp = maxHp;
        Current = maxHp;
    }

    public void OnHit(in HitParams hit)
    {
        if (IsDead)
            return;

        Current = Mathf.Max(0, Current - hit.Damage);
        if (IsDead)
            Died?.Invoke();
    }
}
