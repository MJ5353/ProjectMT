using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 상인별 신뢰도(저장값)와 판매가 계산. 신뢰도가 높으면 값을 더 쳐주고 다음 밤에 다시 올 확률이 오른다.
// 진행 중인 시장(오늘 온 상인, 남은 버섯, 남은 밤)은 HuntData가 임시로 갖는다
public class MarketData : PlayerDataBase
{
    private readonly Dictionary<string, int> m_DicTrustByID = new();

    public IReadOnlyDictionary<string, int> DicTrustByID => m_DicTrustByID;
    private static DefineDef Define => GameManager.Instance.Define; // 조절값(Define.asset)

    // ------------------------------------------------

    public override void Clear()
    {
        m_DicTrustByID.Clear();
    }

    public override void Load(SaveData data)
    {
        foreach (MerchantTrust entry in data.merchants)
        {
            if (entry.id.E_IsNullOrEmpty() || entry.trust <= 0)
                continue;

            m_DicTrustByID[entry.id] = entry.trust;
        }
    }

    public override void Save(SaveData data)
    {
        foreach (var pair in m_DicTrustByID)
        {
            data.merchants.Add(new MerchantTrust(pair.Key, pair.Value));
        }
    }

    public void AddTrust(string merchantID, int amount)
    {
        if (merchantID.E_IsNullOrEmpty() || amount == 0)
            return;

        int trust = Mathf.Clamp(GetTrust(merchantID) + amount, 0, Define.TrustMax);
        if (trust == 0)
            m_DicTrustByID.Remove(merchantID);
        else
            m_DicTrustByID[merchantID] = trust;
    }

    // get ---

    public int GetTrust(string merchantID)
    {
        if (merchantID.E_IsNullOrEmpty())
            return 0;

        return m_DicTrustByID.TryGetValue(merchantID, out int trust) ? trust : 0;
    }

    // 밤마다 상인을 뽑을 때 쓰는 가중치
    public float GetVisitWeight(MerchantDef merchant)
    {
        if (merchant == null)
            return 0f;

        return Mathf.Max(0f, merchant.VisitWeight) * (1f + GetTrust(merchant.ID) * Define.TrustVisitRate);
    }

    // 버섯 하나 값 = 기본가 × 상인 배율 × 신선도 배율 × 신뢰도 보정. 0이면 이 상인은 사지 않는다
    public int GetPrice(MerchantDef merchant, MushroomDef mushroom, EFreshness freshness)
    {
        if (merchant == null || mushroom == null)
            return 0;

        float rate = merchant.GetRate(mushroom.Rarity);
        if (rate <= 0f || mushroom.Price <= 0)
            return 0;

        float freshRate = freshness == EFreshness.High ? 1f : Define.MarketLowFreshRate;
        float trustRate = 1f + GetTrust(merchant.ID) * Define.TrustPriceRate;

        return Mathf.Max(1, Mathf.RoundToInt(mushroom.Price * rate * freshRate * trustRate));
    }
}
