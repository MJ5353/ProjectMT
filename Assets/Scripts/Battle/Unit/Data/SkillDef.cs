using UnityEngine;

// ==================================================

// 공격 하나. Kind로 기본 공격·스킬(쿨 있음)을 나눈다. UnitDef.ListSkill에 기본 공격 → 스킬 순으로 둔다.
// 이름·ID: Monster_NNN_Skill_NN·Monster_NNN_Attack_NN, 플레이어 Player_Attack_NN. 이름 끝(Attack_NN·Skill_NN) = 애니 상태 = 클립 이름 = Attack.Clip
// 표시 이름·설명·아이콘은 DataDef 것을 쓴다. 쿨(Attack.Cooldown)과 몬스터 고르기 값(거리·가중치·예고)도 Attack 안에 있다

public enum ESkillKind
{
    Attack, // 기본 공격(쿨 없음, 애니 Attack_NN)
    Skill,  // 쿨이 있는 공격(애니 Skill_NN)
}

// ==================================================

[CreateAssetMenu(menuName = "MushTaker/Data/Skill", fileName = "Monster_000_Skill_01")]
public class SkillDef : DataDef
{
    [field: SerializeField]
    public ESkillKind Kind { get; private set; } = ESkillKind.Skill;

    [field: SerializeField]
    public AttackSpec Attack { get; private set; } = new AttackSpec();

    // ------------------------------------------------

#if UNITY_EDITOR
    public void EditorSetup(ESkillKind kind, AttackSpec attack)
    {
        Kind = kind;
        Attack = attack;
    }
#endif
}
