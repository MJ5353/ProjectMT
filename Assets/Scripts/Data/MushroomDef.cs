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
    [field: SerializeField, FormerlySerializedAs("m_Rarity")]
    public ERarity Rarity { get; private set; } = ERarity.Common;

    [field: SerializeField, FormerlySerializedAs("m_Tint")]
    public Color Tint { get; private set; } = Color.white;

}
