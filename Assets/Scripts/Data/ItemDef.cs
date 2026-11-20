using UnityEngine;

// ==================================================

// 버섯·재화가 아닌 일반 아이템(썩은 버섯 등). 인벤토리에 ID와 개수로 들어간다
[CreateAssetMenu(menuName = "MushTaker/Data/Item", fileName = "Item_")]
public class ItemDef : DataDef
{
    public const string RottenID = "Item_Rotten"; // 팔지 못하고 밤이 끝난 버섯
}
