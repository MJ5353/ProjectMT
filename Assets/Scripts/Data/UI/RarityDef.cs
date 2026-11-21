using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 희귀도 → 칸 바탕 그림·색·이름 표. Resources/Data/UI/Rarity.asset 하나만 두고 UICommonSlot이 읽는다.
// 에셋이 없으면 "UI 프리팹 표 갱신"(UISetup)이 만들고 빈 희귀도를 채운다. 값은 에셋에서 고친다

public class RarityDef : DataDef
{
    public const string RarityID = "Rarity";

    [Serializable]
    public class Entry
    {
        [field: SerializeField] public ERarity Rarity { get; private set; }
        [field: SerializeField] public Sprite SlotSprite { get; private set; } // 칸 바탕. 없으면 Color만 칠한다
        [field: SerializeField] public Color Color { get; private set; } = Color.white;
        [field: SerializeField] public string DisplayName { get; private set; }

        public Entry(ERarity rarity, Sprite slotSprite, Color color, string displayName)
        {
            Rarity = rarity;
            SlotSprite = slotSprite;
            Color = color;
            DisplayName = displayName;
        }
    }

    [SerializeField] private List<Entry> m_ListEntry = new();
    private readonly Dictionary<ERarity, Entry> m_DicEntry = new(); // 런타임 조회용. 처음 찾을 때 채운다

    private static bool s_IsWarned; // 표가 없다는 경고는 한 번만

    // ------------------------------------------------

    // 표에 없으면 null
    public Entry Get(ERarity rarity)
    {
        if (m_DicEntry.Count == 0)
        {
            foreach (Entry entry in m_ListEntry)
            {
                if (entry != null)
                    m_DicEntry[entry.Rarity] = entry;
            }
        }

        return m_DicEntry.E_GetOrDefault(rarity);
    }

    public static Entry S_Get(ERarity rarity)
    {
        if (GameDatabase.S_TryGet(RarityID, out RarityDef def))
            return def.Get(rarity);

        if (s_IsWarned == false)
        {
            s_IsWarned = true;
            CLogger.Warning("RarityDef", "RarityDef가 DB에 없습니다. MushTaker/UI/UI 프리팹 표 갱신을 실행하세요.");
        }

        return null;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        m_DicEntry.Clear();
    }

    public IReadOnlyList<Entry> EditorList => m_ListEntry;

    // UISetup 메뉴 전용
    public void EditorSetList(List<Entry> listEntry)
    {
        m_ListEntry = listEntry;
        m_DicEntry.Clear();
    }
#endif
}
