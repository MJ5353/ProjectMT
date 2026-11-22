using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 정비 탭 강화 노드 하나. 앞 노드(ListRequire)를 모두 열고 골드(Cost)를 내면 열린다. 능력치는 수정치 목록(StatModifier)으로 든다.
// 수치 하나로 안 되는 효과는 패시브(PassiveDef) 에셋을 참조한다. 이름·설명(스페셜 효과 글)·아이콘은 DataDef 것을 쓴다. 트리 칸 자리(Slot)는 UI 생성기(LobbyUISetup)가 읽어 노드를 놓는다
[CreateAssetMenu(menuName = "MushTaker/Data/GearNode", fileName = "GearNode_")]
public class GearNodeDef : DataDef
{
    // 능력치 ---
    [SerializeField] private List<StatModifier> m_ListModifier = new(); // 열면 플레이어 능력치에 출처로 들어간다

    [field: SerializeField]
    public PassiveDef Passive { get; private set; } = null;         // 열면 플레이어 패시브에 출처로 들어간다. 없으면 null

    // 열기 ---
    [field: SerializeField]
    public int Cost { get; private set; } = 100;                    // [mj] todo) 가안. 여는 데 드는 골드

    [SerializeField] private List<GearNodeDef> m_ListRequire = new(); // 모두 열려 있어야 열 수 있다. 비면 처음부터 열 수 있음

    [field: SerializeField]
    public Vector2 Slot { get; private set; } = Vector2.zero;       // 트리 칸(열 0~8, 행 0이 아래)

    public IReadOnlyList<StatModifier> ListModifier => m_ListModifier;
    public IReadOnlyList<GearNodeDef> ListRequire => m_ListRequire;
    public bool IsSpecial => Passive != null || m_ListModifier.Exists(modifier => modifier.Stat > EStatType.AttackSpeed); // 테두리를 따로 그린다

    // ------------------------------------------------

#if UNITY_EDITOR
    public void EditorSetup(List<StatModifier> listModifier, PassiveDef passive, int cost, Vector2 slot)
    {
        m_ListModifier = listModifier;
        Passive = passive;
        Cost = cost;
        Slot = slot;
    }

    public void EditorSetRequire(List<GearNodeDef> listRequire) => m_ListRequire = listRequire;
#endif
}
