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

    // 피격 반응(넉백·번쩍·히트스톱·조각·무적) 수치. 여러 유닛이 같은 에셋을 같이 쓴다
    [field: SerializeField]
    public HitDef Hit { get; private set; } = null; 

    // 공격 목록 --- 플레이어는 첫 칸만, 몬스터는 거리·쿨·가중치로 고른다
    [field: SerializeField]
    public List<AttackSpec> ListAttack { get; private set; } = new() { new AttackSpec() };
    
    public AttackSpec MainAttack => ListAttack.Count > 0 ? ListAttack[0] : null; // 첫 칸. 없으면 null

    // ------------------------------------------------

#if UNITY_EDITOR
    // BattleSetup 메뉴가 처음 만들 때만 쓴다
    public void EditorSetAttack(AttackSpec attack)
    {
        ListAttack = new List<AttackSpec> { attack };
    }
#endif
}
