using System;
using UnityEngine;

// ==================================================

// 체력 상태. 피격 반응이 아니라 HitFeature가 반응 처리 전에 직접 깎는다

public class HealthFeature : UnitFeature
{
    public int MaxHp => Mathf.Max(1, Mathf.RoundToInt(m_BaseMaxHp * Owner.Stat.MaxHpRate)); // 강화(UnitStat) 반영
    private readonly int m_BaseMaxHp;    // UnitDef.MaxHp

    public event Action Died;
    public int Current { get; private set; }
    public bool IsDead => Current <= 0;
    public float Ratio => (float)Current / MaxHp;
    
    // ------------------------------------------------
    
    public HealthFeature(UnitController owner, int maxHp) : base(owner)
    {
        m_BaseMaxHp = maxHp;
        Current = maxHp;
    }

    // 체력을 가득 채운다. 강화 값이 바뀐 뒤(UnitController.RefreshStat)에도 부른다
    public override void Reset()
    {
        Current = MaxHp;
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        Current = Mathf.Min(MaxHp, Current + amount);
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
