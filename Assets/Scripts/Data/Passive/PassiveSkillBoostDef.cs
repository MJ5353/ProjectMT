using UnityEngine;

// ==================================================

// 공격 하나 강화: 정한 스킬의 피해·판정 범위 배율(질주 베기, 바바리안)
[CreateAssetMenu(menuName = "MushTaker/Data/Passive/SkillBoost", fileName = "Passive_")]
public class PassiveSkillBoostDef : PassiveDef
{
    [field: SerializeField]
    public SkillDef Skill { get; private set; } = null;

    [field: SerializeField]
    public float DamageRate { get; private set; } = 1f;     // 피해 배율

    [field: SerializeField]
    public float RadiusRate { get; private set; } = 1f;     // 판정 범위 배율

    // ------------------------------------------------

    public override void ModifyAttack(UnitController owner, AttackSpec spec, ref float damageRate, ref float radiusRate)
    {
        if (Skill == null || Skill.Attack != spec)
            return;

        damageRate *= DamageRate;
        radiusRate *= RadiusRate;
    }

#if UNITY_EDITOR
    public void EditorSetup(SkillDef skill, float damageRate, float radiusRate)
    {
        Skill = skill;
        DamageRate = damageRate;
        RadiusRate = radiusRate;
    }
#endif
}
