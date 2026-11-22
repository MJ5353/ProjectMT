using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 정비 탭 왼쪽 엠블렘 한 단계. 연 강화 노드 수가 NeedCount 이상이면 이 단계가 되고, 보너스는 지난 단계 것까지 쌓인다.
// 이름은 DataDef 이름, 그림은 DataDef 아이콘(슬롯 없이 뱃지 그림만 보인다. 단계 색은 그림에 담는다)
[CreateAssetMenu(menuName = "MushTaker/Data/Emblem", fileName = "Emblem_")]
public class EmblemDef : DataDef
{
    [field: SerializeField]
    public int NeedCount { get; private set; } = 0;                 // 이 단계에 필요한 연 노드 수

    [SerializeField] private List<StatModifier> m_ListModifier = new(); // 이 단계 보너스. 이 단계 이상이면 플레이어 능력치에 출처로 들어간다

    public IReadOnlyList<StatModifier> ListModifier => m_ListModifier;

    // ------------------------------------------------

#if UNITY_EDITOR
    public void EditorSetup(int needCount, List<StatModifier> listModifier)
    {
        NeedCount = needCount;
        m_ListModifier = listModifier;
    }
#endif
}
