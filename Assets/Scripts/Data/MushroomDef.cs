using UnityEngine;
using UnityEngine.Serialization;

// ==================================================

public enum ERarity
{
    Common,
    Rare,
    Epic,
}

// ==================================================

// 버섯 정의. 재배 시간, 수확량, 강화치 등은 기획 표가 정해지면 추가한다
[CreateAssetMenu(menuName = "MushTaker/Data/Mushroom", fileName = "Mushroom_")]
public class MushroomDef : DataDef
{
    [field: SerializeField, Tooltip("도감 번호. 이름의 NNN과 같게 둔다(Mushroom_001_Songi → 1)")]
    public int DexNo { get; private set; } = 0;

    [field: SerializeField, FormerlySerializedAs("m_Rarity")]
    public ERarity Rarity { get; private set; } = ERarity.Common;

    [field: SerializeField, FormerlySerializedAs("m_Tint")]
    public Color Tint { get; private set; } = Color.white;

    // ------------------------------------------------

#if UNITY_EDITOR
    public void EditorSetDexNo(int dexNo) => DexNo = dexNo;
#endif

}
