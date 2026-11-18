using System;
using UnityEngine;

// ==================================================

// 체력 상태. 피격 반응이 아니라 HitFeature가 반응 처리 전에 직접 깎는다
public class HealthFeature : UnitFeature
{
    public int MaxHp => m_MaxHp;
    private readonly int m_MaxHp;

    public event Action Died;
    public int Current { get; private set; }
    public bool IsDead => Current <= 0;
    
    // ------------------------------------------------
    
    public HealthFeature(IActor owner, int maxHp) : base(owner)
    {
        m_MaxHp = maxHp;
        Current = maxHp;
    }

    public override void Reset()
    {
        Current = m_MaxHp;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        Current = Mathf.Max(0, Current - damage);
        if (IsDead)
            Died?.Invoke();
    }
}
