using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 패시브가 끼어드는 때(훅).
// Attack·Taken은 피해 계산 중이라 능력치 효과(PassiveEffect_Stat)만 읽고, Hit·Pickup은 일을 하는 효과(떨구기·회복 등)를 실행한다
public enum EPassiveTrigger
{
    Attack,     // 공격 피해·범위를 계산할 때(DamageCalculator)
    Taken,      // 받는 피해를 계산할 때(DamageCalculator)
    Hit,        // 맞은 뒤(UnitController.OnHit)
    Pickup,     // 주운 뒤(LootService)
    PerfectDodge, // 퍼펙트 닷지 직후(UnitController.OnPerfectDodged). Hit 칸에 피한 공격이 들어 있다
}

// ==================================================

// 훅 지점의 정보. 조건·효과가 읽는다. 훅마다 채워지는 칸이 다르다(Spec은 Attack, Hit은 Taken·Hit·PerfectDodge, Loot은 Pickup)
public readonly struct PassiveContext
{
    public readonly UnitController Owner;
    public readonly AttackSpec Spec;
    public readonly HitParams Hit;
    public readonly LootPicked Loot;

    private PassiveContext(UnitController owner, AttackSpec spec, in HitParams hit, in LootPicked loot)
    {
        Owner = owner;
        Spec = spec;
        Hit = hit;
        Loot = loot;
    }

    public static PassiveContext S_Attack(UnitController owner, AttackSpec spec) => new(owner, spec, default, default);
    public static PassiveContext S_Taken(UnitController owner, in HitParams hit) => new(owner, null, hit, default);
    public static PassiveContext S_Hit(UnitController owner, in HitParams hit) => new(owner, null, hit, default);
    public static PassiveContext S_Pickup(UnitController owner, in LootPicked loot) => new(owner, null, default, loot);
}

// ==================================================

// 규칙 하나 = 언제(Trigger) + 조건(모두 맞아야 함, 비면 항상) + 효과.
// 조건·효과는 작은 클래스(PassiveCondition_*, PassiveEffect_*)를 에셋에서 골라 붙인다(SubclassSelector 드롭다운)
[Serializable]
public class PassiveRule
{
    [field: SerializeField]
    public EPassiveTrigger Trigger { get; private set; } = EPassiveTrigger.Attack;

    [SerializeReference, SubclassSelector] private List<PassiveCondition> m_ListCondition = new();
    [SerializeReference, SubclassSelector] private List<PassiveEffect> m_ListEffect = new();

    // ------------------------------------------------

    public PassiveRule() { }
    public PassiveRule(EPassiveTrigger trigger, List<PassiveCondition> listCondition, List<PassiveEffect> listEffect)
    {
        Trigger = trigger;
        m_ListCondition = listCondition;
        m_ListEffect = listEffect;
    }

    public bool Check(in PassiveContext context)
    {
        foreach (PassiveCondition condition in m_ListCondition)
        {
            if (condition != null && condition.Check(context) == false)
                return false;
        }

        return true;
    }

    public void Execute(in PassiveContext context)
    {
        foreach (PassiveEffect effect in m_ListEffect)
        {
            effect?.Execute(context);
        }
    }

    public float GetRate(EStatType stat)
    {
        float rate = 0f;
        foreach (PassiveEffect effect in m_ListEffect)
        {
            if (effect != null)
                rate += effect.GetRate(stat);
        }

        return rate;
    }
}

// ==================================================

// 수치 하나로 안 되는 효과(정비 스페셜 등). 규칙 목록(PassiveRule)으로만 이루어져, 새 스페셜은 대부분 에셋에서 조건·효과를 조합해 만든다.
// 새 코드는 새 조건·새 효과가 필요할 때만 PassiveCondition_*·PassiveEffect_* 클래스 하나를 더한다.
// 노드·장비 등은 에셋을 참조만 하고 PassiveFeature.AddSource로 넣는다
[CreateAssetMenu(menuName = "MushTaker/Data/Passive", fileName = "Passive_")]
public class PassiveDef : DataDef
{
    [SerializeField] private List<PassiveRule> m_ListRule = new();

    public IReadOnlyList<PassiveRule> ListRule => m_ListRule;

    // ------------------------------------------------

    // 이 훅에서 조건이 맞는 규칙의 순간 능력치(Rate) 합
    public float GetRate(EPassiveTrigger trigger, in PassiveContext context, EStatType stat)
    {
        float rate = 0f;
        foreach (PassiveRule rule in m_ListRule)
        {
            if (rule.Trigger == trigger && rule.Check(context))
                rate += rule.GetRate(stat);
        }

        return rate;
    }

    // 이 훅에서 조건이 맞는 규칙의 효과를 실행한다
    public void Execute(EPassiveTrigger trigger, in PassiveContext context)
    {
        foreach (PassiveRule rule in m_ListRule)
        {
            if (rule.Trigger == trigger && rule.Check(context))
                rule.Execute(context);
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(List<PassiveRule> listRule) => m_ListRule = listRule;
#endif
}
