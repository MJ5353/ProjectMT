using UnityEngine;

public enum ERarity
{
    Common,
    Rare,
    Epic,
}

// 버섯 정의. 재배 시간, 수확량, 강화치 등은 기획 표가 정해지면 추가한다
[CreateAssetMenu(menuName = "MushTaker/Data/Mushroom", fileName = "Mushroom_")]
public class MushroomDef : DataDef
{
    [SerializeField] private ERarity m_Rarity = ERarity.Common;
    [SerializeField] private Color m_Tint = Color.white;

    public ERarity Rarity => m_Rarity;
    public Color Tint => m_Tint;
}
