using UnityEngine;

// ==================================================

// 재화 정의. 재화도 인벤토리에 ID와 개수로 들어가서 아이템과 같은 방식으로 다룬다.
// 땅에 떨어지는 픽업(동전)은 이름 규칙으로 찾는다: Currency_Gold → Pickup_Gold
[CreateAssetMenu(menuName = "MushTaker/Data/Currency", fileName = "Currency_")]
public class CurrencyDef : DataDef, IPickupItem
{
    public const string GoldID = "Currency_Gold";
    private const string KeyPrefix = "Currency_";

    public string PickupPath => S_GetPickupPath(ID);

    // 재화 ID → 픽업 프리팹 경로(Resources 기준). 데이터 편집기 > 버섯 > 픽업 빌드가 굽는다
    public static string S_GetPickupPath(string currencyID)
    {
        string key = currencyID != null && currencyID.StartsWith(KeyPrefix) ? currencyID.Substring(KeyPrefix.Length) : currencyID;
        return CommonPath.PickupDir + CommonPath.PickupPrefix + key;
    }
}
