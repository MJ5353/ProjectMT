using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// ==================================================

// 플레이어와 몬스터이 같이 쓰는 몸 수치. 몬스터은 MonsterDef, 플레이어는 PlayerDef가 이어받는다

public abstract class UnitDef : DataDef
{
    [field: SerializeField, FormerlySerializedAs("m_MaxHp")]
    public int MaxHp { get; private set; } = 3;                    // 최대 체력

    [field: SerializeField, FormerlySerializedAs("m_HitStunTime")]
    public float HitStunTime { get; private set; } = 0.35f;        // 맞고 굳는 시간(초)

    [field: SerializeField]
    public float TurnSpeed { get; private set; } = 900f;           // 초당 회전 각도. 보스는 낮춰서 옆을 노릴 틈을 준다

    // 피격 반응(넉백·번쩍·히트스톱·조각·무적) 수치. 여러 유닛이 같은 에셋을 같이 쓴다
    [field: SerializeField]
    public HitDef Hit { get; private set; } = null;

    // 공격 목록 --- 기본 공격(Kind Attack)과 스킬(Kind Skill). 몬스터는 모두 후보로 두고 거리·쿨·가중치로 고른다.
    // 플레이어는 Kind Attack 순서대로 0번 기본·1번 위로 치기, 스킬은 아직 쓰지 않는다
    [field: SerializeField]
    public List<SkillDef> ListSkill { get; private set; } = new();

    public AttackSpec MainAttack => GetBasicAttack(0); // 첫 기본 공격. 없으면 null

    // ------------------------------------------------

    // 기본 공격 index번째. ListSkill의 Kind Attack 순서
    public AttackSpec GetBasicAttack(int index)
    {
        foreach (SkillDef skill in ListSkill)
        {
            if (skill == null || skill.Kind != ESkillKind.Attack)
                continue;

            if (index == 0)
                return skill.Attack;

            index--;
        }

        return null;
    }

    // 쓸 수 있는 공격 전부(SkillDef). 판정 뼈 검사·기즈모·몬스터 후보용
    public void CollectAttacks(List<AttackSpec> results)
    {
        results.Clear();
        foreach (SkillDef skill in ListSkill)
        {
            if (skill != null)
                results.Add(skill.Attack);
        }
    }
}
