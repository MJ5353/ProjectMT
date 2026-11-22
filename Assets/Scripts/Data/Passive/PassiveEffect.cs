using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 패시브 규칙의 효과. 하위 클래스 하나 = 효과 하나. [Serializable]과 SubclassLabel(드롭다운 글)을 붙인다.
// 능력치 효과는 GetRate(Attack·Taken 훅에서 읽음), 일을 하는 효과는 Execute(Hit·Pickup 훅에서 실행)를 덮어쓴다
[Serializable]
public abstract class PassiveEffect
{
    public virtual void Execute(in PassiveContext context) { }

    public virtual float GetRate(EStatType stat) => 0f;
}

// ==================================================

// 그 순간에만 더하는 능력치(Rate만 본다). 예: 공격력 Rate 0.5 = 피해 x1.5, 방어 Rate 0.3 = 받는 피해 -30%(방어 능력치와 더해짐)
[Serializable, SubclassLabel("순간 능력치")]
public class PassiveEffect_Stat : PassiveEffect
{
    [SerializeField] private List<StatModifier> m_ListModifier = new();

    public PassiveEffect_Stat() { }
    public PassiveEffect_Stat(List<StatModifier> listModifier) => m_ListModifier = listModifier;

    public override float GetRate(EStatType stat)
    {
        float rate = 0f;
        foreach (StatModifier modifier in m_ListModifier)
        {
            if (modifier.Stat == stat && modifier.Type == EStatModType.Rate)
                rate += modifier.Value;
        }

        return rate;
    }
}

// 주인 발밑에 픽업을 떨군다(버섯·재화). 주워야 들어온다
[Serializable, SubclassLabel("픽업 떨구기")]
public class PassiveEffect_DropPickup : PassiveEffect
{
    [field: SerializeField, Tooltip("버섯(MushroomDef) 또는 재화(CurrencyDef)")]
    public DataDef Item { get; private set; } = null;

    [field: SerializeField]
    public Vector2Int CountRange { get; private set; } = new(1, 1);     // 떨구는 개수(최소~최대)

    public PassiveEffect_DropPickup() { }
    public PassiveEffect_DropPickup(DataDef item, Vector2Int countRange)
    {
        Item = item;
        CountRange = countRange;
    }

    public override void Execute(in PassiveContext context)
    {
        if (Item is not IPickupItem item)
            return;

        MushroomPickup prefab = ResourceManager.Instance.Load<MushroomPickup>(item.PickupPath, "데이터 편집기 > 버섯 > 픽업 빌드");
        if (prefab == null)
            return;

        int count = UnityEngine.Random.Range(CountRange.x, CountRange.y + 1);
        MushroomPickup.S_Drop(prefab, context.Owner.transform.position, count, EFreshness.High, ERarity.Common);
    }
}

// 체력 회복. Pickup 훅이면 주운 개수만큼 곱할 수 있다
[Serializable, SubclassLabel("체력 회복")]
public class PassiveEffect_Heal : PassiveEffect
{
    [field: SerializeField]
    public int Amount { get; private set; } = 1;

    [field: SerializeField, Tooltip("주운 개수만큼 곱한다(Pickup 훅)")]
    public bool PerPickAmount { get; private set; } = false;

    public PassiveEffect_Heal() { }
    public PassiveEffect_Heal(int amount, bool perPickAmount)
    {
        Amount = amount;
        PerPickAmount = perPickAmount;
    }

    public override void Execute(in PassiveContext context)
    {
        int amount = PerPickAmount ? Amount * Mathf.Max(1, context.Loot.Amount) : Amount;
        context.Owner.HealthF.Heal(amount);
    }
}
