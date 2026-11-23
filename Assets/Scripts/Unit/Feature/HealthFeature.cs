using System;
using UnityEngine;

// ==================================================

// 체력 상태. 피격 반응이 아니라 HitFeature가 반응 처리 전에 직접 깎는다.
// 최대 체력 = UnitDef.MaxHp에 능력치(StatFeature) 반영. 능력치가 바뀌면 늘어난 만큼 현재 체력도 채운다.
// 체력이 바뀌면(피해·회복·Reset·능력치) UnitHpChanged를 보낸다. HUD 체력바가 매 프레임 읽지 않고 받는다

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
        int previous = Current;
        MaxHp = CalcMaxHp();
        Current = MaxHp;
        Publish(previous);
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        int previous = Current;
        Current = Mathf.Min(MaxHp, Current + amount);
        Publish(previous);
    }

    private void OnStatChanged()
    {
        int before = MaxHp;
        int previous = Current;
        MaxHp = CalcMaxHp();
        if (IsDead == false)
            Current = Mathf.Clamp(Current + MaxHp - before, 1, MaxHp);

        Publish(previous);
    }

    private int CalcMaxHp() => Mathf.Max(1, Mathf.RoundToInt(Owner.StatF.Get(EStatType.MaxHp, m_BaseMaxHp)));

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        int previous = Current;
        Current = Mathf.Max(0, Current - damage);
        Publish(previous);

        if (IsDead)
            Died?.Invoke();
    }

    private void Publish(int previous) => SEventManager.Publish(new UnitHpChanged(Owner, previous));
}
