using System;
using UnityEngine;

// ==================================================

// 패시브 규칙의 조건. 하위 클래스 하나 = 조건 하나. [Serializable]과 SubclassLabel(드롭다운 글)을 붙인다
[Serializable]
public abstract class PassiveCondition
{
    public abstract bool Check(in PassiveContext context);
}

// ==================================================

// 정한 공격(스킬)일 때. Attack 훅용
[Serializable, SubclassLabel("스킬이 이것일 때")]
public class PassiveCondition_Skill : PassiveCondition
{
    [field: SerializeField]
    public SkillDef Skill { get; private set; } = null;

    public PassiveCondition_Skill() { }
    public PassiveCondition_Skill(SkillDef skill) => Skill = skill;

    public override bool Check(in PassiveContext context) => Skill != null && context.Spec != null && Skill.Attack == context.Spec;
}

// 체력 비율이 이 값 이하일 때
[Serializable, SubclassLabel("체력이 비율 이하일 때")]
public class PassiveCondition_HpBelow : PassiveCondition
{
    [field: SerializeField, UnityEngine.Range(0f, 1f)]
    public float Ratio { get; private set; } = 0.3f;

    public PassiveCondition_HpBelow() { }
    public PassiveCondition_HpBelow(float ratio) => Ratio = ratio;

    public override bool Check(in PassiveContext context) => context.Owner.HealthF.Ratio <= Ratio;
}

// 주운 것이 버섯일 때(재화 제외). Pickup 훅용
[Serializable, SubclassLabel("주운 것이 버섯일 때")]
public class PassiveCondition_PickMushroom : PassiveCondition
{
    public override bool Check(in PassiveContext context) => GameDatabase.S_TryGet(context.Loot.ItemID, out MushroomDef _);
}

// 확률
[Serializable, SubclassLabel("확률")]
public class PassiveCondition_Chance : PassiveCondition
{
    [field: SerializeField, UnityEngine.Range(0f, 1f)]
    public float Chance { get; private set; } = 0.5f;

    public PassiveCondition_Chance() { }
    public PassiveCondition_Chance(float chance) => Chance = chance;

    public override bool Check(in PassiveContext context) => UnityEngine.Random.value < Chance;
}
