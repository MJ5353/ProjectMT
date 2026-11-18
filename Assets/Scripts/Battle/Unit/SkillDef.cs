using UnityEngine;

// ==================================================

// 공격 하나. Kind로 기본 공격·스킬(쿨 있음)을 나눈다. 지금은 기본 공격 수치는 UnitDef.ListAttack, 스킬은 UnitDef.ListSkill에서 쓴다.
// 이름·ID: 스킬 Monster_NNN_Skill_NN, 기본 공격 Monster_NNN_Attack_NN(번호는 순서 의미 없음). 애니 상태 = 클립 이름 = Attack.Clip
// !!) 기본 공격 SkillDef는 데이터 이전 전까지 ListSkill에 넣지 않는다(ListAttack과 후보 중복)
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
