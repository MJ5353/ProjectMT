using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 정비 강화 능력치. 노드·엠블렘 보너스가 UnitStat에 더하는 값의 종류. 값은 모두 비율(0.1 = 10%)이고 BreakResist만 경직 수치다.
// 앞 5개(Attack~AttackSpeed)가 트리의 줄이고, 뒤는 스페셜 노드가 올리는 값이다
public enum EGearStat
{
    None,
    Attack,         // 주는 피해 +
    MaxHp,          // 최대 체력 +
    Dodge,          // 대시 쿨 -
    Defense,        // 받는 피해 -
    AttackSpeed,    // 공격 빠르기 +
    MoveSpeed,      // 이동 속도 +
    PartDamage,     // 버섯 부위 피해 +
    DashDistance,   // 대시 시간(= 거리) +
    BreakResist,    // 경직 저항(UnitDef.BreakImmune에 더함)
}

// 수치 하나로 안 되는 스페셜. 전투 수치는 UnitStat, 골드·회복은 BattleService가 처리한다
public enum EGearSpecial
{
    None,
    Insurance,      // 상해보험: 맞으면 골드 SpecialRange(최소~최대)
    SkillBoost,     // 공격 강화: TargetSkill 피해 ×SpecialValue, 범위 ×SpecialValue2
    LastStand,      // 든든한 뿌리: 체력이 SpecialValue2 비율 이하면 받는 피해 -SpecialValue
    SporeRegen,     // 포자 재생: 버섯을 주울 때 체력 SpecialValue 회복
}

// ==================================================

// 정비 탭 강화 노드 하나. 앞 노드(ListRequire)를 모두 열고 골드(Cost)를 내면 열린다.
// 이름·설명(스페셜 효과 글)·아이콘은 DataDef 것을 쓴다. 트리 칸 자리(Slot)는 UI 생성기(LobbyUISetup)가 읽어 노드를 놓는다
[CreateAssetMenu(menuName = "MushTaker/Data/GearNode", fileName = "GearNode_")]
public class GearNodeDef : DataDef
{
    // 능력치 ---
    [field: SerializeField]
    public EGearStat Stat { get; private set; } = EGearStat.None;

    [field: SerializeField]
    public float StatValue { get; private set; } = 0f;             // 0.1 = 10%. BreakResist는 경직 수치

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

    public IReadOnlyList<GearNodeDef> ListRequire => m_ListRequire;
    public bool IsSpecial => Special != EGearSpecial.None || Stat > EGearStat.AttackSpeed; // 테두리를 따로 그린다

    // ------------------------------------------------

#if UNITY_EDITOR
    public void EditorSetup(EGearStat stat, float statValue, EGearSpecial special, float specialValue, float specialValue2, Vector2Int specialRange, SkillDef targetSkill, int cost, Vector2 slot)
    {
        Stat = stat;
        StatValue = statValue;
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
