using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 정비 탭 왼쪽 엠블렘 한 단계. 연 강화 노드 수가 NeedCount 이상이면 이 단계가 되고, 보너스는 지난 단계 것까지 쌓인다.
// 이름은 DataDef 이름, 그림은 DataDef 아이콘(없으면 Tint만 칠한 임시 그림)
[CreateAssetMenu(menuName = "MushTaker/Data/Emblem", fileName = "Emblem_")]
public class EmblemDef : DataDef
{
    [Serializable]
    public class Bonus
    {
        [field: SerializeField] public EGearStat Stat { get; private set; } = EGearStat.None;
        [field: SerializeField] public float Value { get; private set; } = 0f; // 0.05 = 5%

        public Bonus(EGearStat stat, float value)
        {
            Stat = stat;
            Value = value;
        }
    }

    [field: SerializeField]
    public int NeedCount { get; private set; } = 0;                 // 이 단계에 필요한 연 노드 수

    [field: SerializeField]
    public Color Tint { get; private set; } = Color.white;          // 엠블렘 그림 색. 단계 그림이 나오면 흰색으로 둔다

    [SerializeField] private List<Bonus> m_ListBonus = new();

    public IReadOnlyList<Bonus> ListBonus => m_ListBonus;

    // ------------------------------------------------

#if UNITY_EDITOR
    public void EditorSetup(int needCount, Color tint, List<Bonus> listBonus)
    {
        NeedCount = needCount;
        Tint = tint;
        m_ListBonus = listBonus;
    }
#endif
}
