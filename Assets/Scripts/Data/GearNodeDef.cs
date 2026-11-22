using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 수치 하나로 안 되는 스페셜. 전투 수치는 StatFeature 스페셜 칸, 골드·회복은 BattleService가 처리한다(훅 구조로 옮길 예정)
public enum EGearSpecial
{
    None,
    Insurance,      // 상해보험: 맞으면 골드 SpecialRange(최소~최대)
    SkillBoost,     // 공격 강화: TargetSkill 피해 ×SpecialValue, 범위 ×SpecialValue2
    LastStand,      // 든든한 뿌리: 체력이 SpecialValue2 비율 이하면 받는 피해 -SpecialValue
    SporeRegen,     // 포자 재생: 버섯을 주울 때 체력 SpecialValue 회복
}

// ==================================================

// 정비 탭 강화 노드 하나. 앞 노드(ListRequire)를 모두 열고 골드(Cost)를 내면 열린다. 능력치는 수정치 목록(StatModifier)으로 든다.
// 이름·설명(스페셜 효과 글)·아이콘은 DataDef 것을 쓴다. 트리 칸 자리(Slot)는 UI 생성기(LobbyUISetup)가 읽어 노드를 놓는다
[CreateAssetMenu(menuName = "MushTaker/Data/GearNode", fileName = "GearNode_")]
public class GearNodeDef : DataDef
{
    // 능력치 ---
    [SerializeField] private List<StatModifier> m_ListModifier = new(); // 열면 플레이어 능력치에 출처로 들어간다

    // 스페셜 ---
    [field: SerializeField]
    public EGearSpecial Special { get; private set; } = EGearSpecial.None;

    [field: SerializeField]
    public float SpecialValue { get; private set; } = 0f;          // 종류별 뜻은 EGearSpecial 주석

    [field: SerializeField]
    public float SpecialValue2 { get; private set; } = 0f;

    [field: SerializeField]
    public Vector2Int SpecialRange { get; private set; } = Vector2Int.zero; // 상해보험 골드(최소~최대)

    [field: SerializeField]
    public SkillDef TargetSkill { get; private set; } = null;       // 공격 강화 대상

    // 열기 ---
    [field: SerializeField]
    public int Cost { get; private set; } = 100;                    // [mj] todo) 가안. 여는 데 드는 골드

    [SerializeField] private List<GearNodeDef> m_ListRequire = new(); // 모두 열려 있어야 열 수 있다. 비면 처음부터 열 수 있음

    [field: SerializeField]
    public Vector2 Slot { get; private set; } = Vector2.zero;       // 트리 칸(열 0~8, 행 0이 아래)

    public IReadOnlyList<StatModifier> ListModifier => m_ListModifier;
    public IReadOnlyList<GearNodeDef> ListRequire => m_ListRequire;
    public bool IsSpecial => Special != EGearSpecial.None || m_ListModifier.Exists(modifier => modifier.Stat > EStatType.AttackSpeed); // 테두리를 따로 그린다

    // ------------------------------------------------

#if UNITY_EDITOR
    public void EditorSetup(List<StatModifier> listModifier, EGearSpecial special, float specialValue, float specialValue2, Vector2Int specialRange, SkillDef targetSkill, int cost, Vector2 slot)
    {
        m_ListModifier = listModifier;
        Special = special;
        SpecialValue = specialValue;
        SpecialValue2 = specialValue2;
        SpecialRange = specialRange;
        TargetSkill = targetSkill;
        Cost = cost;
        Slot = slot;
    }

    public void EditorSetRequire(List<GearNodeDef> listRequire) => m_ListRequire = listRequire;
#endif
}
