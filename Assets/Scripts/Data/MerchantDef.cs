using UnityEngine;

// ==================================================

// 시장 상인 정의. 어떤 버섯을 얼마나 쳐주는지와 밤마다 올 가능성을 정한다.
// 상인별 신뢰도는 MarketData가 상인 ID로 저장한다
[CreateAssetMenu(menuName = "MushTaker/Data/Merchant", fileName = "Merchant_")]
public class MerchantDef : DataDef
{
    [field: SerializeField, Tooltip("이 희귀도 이상이면 선호 배율, 아래면 기본 배율")]
    public ERarity PreferRarity { get; private set; } = ERarity.Common;

    [field: SerializeField, Tooltip("선호 버섯 가격 배율")]
    public float PreferRate { get; private set; } = 1f;

    [field: SerializeField, Tooltip("그 밖의 버섯 가격 배율. 0이면 사지 않는다")]
    public float OtherRate { get; private set; } = 1f;

    [field: SerializeField, Tooltip("밤마다 올 가중치. 신뢰도가 오르면 더 커진다")]
    public float VisitWeight { get; private set; } = 1f;

    [field: SerializeField, Tooltip("30분 기다릴 때 가격이 흔들리는 폭. 0.2면 0.8~1.2배 사이로 바뀐다")]
    public float PriceSwing { get; private set; } = 0.2f;

    // ------------------------------------------------

    public float GetRate(ERarity rarity) => rarity >= PreferRarity ? PreferRate : OtherRate;

#if UNITY_EDITOR
    public void EditorSetup(ERarity preferRarity, float preferRate, float otherRate, float visitWeight, float priceSwing)
    {
        PreferRarity = preferRarity;
        PreferRate = preferRate;
        OtherRate = otherRate;
        VisitWeight = visitWeight;
        PriceSwing = priceSwing;
    }
#endif
}
