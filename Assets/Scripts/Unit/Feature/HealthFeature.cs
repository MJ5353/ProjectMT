using System;
using UnityEngine;

// ==================================================

// 체력 상태. 피격 반응이 아니라 HitFeature가 반응 처리 전에 직접 깎는다.
// 최대 체력 = UnitDef.MaxHp에 능력치(StatFeature) 반영. 능력치가 바뀌면 늘어난 만큼 현재 체력도 채운다

public class HealthFeature : UnitFeature
{
    public int MaxHp { get; private set; }
    private readonly int m_BaseMaxHp;    // UnitDef.MaxHp

    public event Action Died;
    public int Current { get; private set; }
    public bool IsDead => Current <= 0;
    public float Ratio => (float)Current / MaxHp;
    
    // ------------------------------------------------
    
    public HealthFeature(UnitController owner, int maxHp) : base(owner)
    {
        m_BaseMaxHp = maxHp;
        MaxHp = maxHp;
        Current = maxHp;
        owner.StatF.Changed += OnStatChanged;
    }

    // 체력을 가득 채운다
    public override void Reset()
    {
        MaxHp = CalcMaxHp();
        Current = MaxHp;
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        Current = Mathf.Min(MaxHp, Current + amount);
    }

    private void OnStatChanged()
    {
        int before = MaxHp;
        MaxHp = CalcMaxHp();
        if (IsDead)
            return;

        Current = Mathf.Clamp(Current + MaxHp - before, 1, MaxHp);
    }

    private int CalcMaxHp() => Mathf.Max(1, Mathf.RoundToInt(Owner.StatF.Get(EStatType.MaxHp, m_BaseMaxHp)));

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        Current = Mathf.Max(0, Current - damage);
        if (IsDead)
            Died?.Invoke();
    }
}
