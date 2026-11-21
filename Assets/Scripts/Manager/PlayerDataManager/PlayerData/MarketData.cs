using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 상인별 신뢰도(저장값). 저장·증감·조회만 한다.
// 신뢰도를 쓰는 판매가·방문 가중치 계산과 진행 중인 시장(오늘 온 상인, 남은 버섯, 남은 밤)은 HuntData(HuntData.Market.cs)가 맡는다
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
        if (amount == 0)
            return;

        int trust = Mathf.Clamp(GetTrust(merchantID) + amount, 0, Define.TrustMax);
        if (trust == 0)
            m_DicTrustByID.Remove(merchantID);
        else
            m_DicTrustByID[merchantID] = trust;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void DebugResetTrust() => m_DicTrustByID.Clear();
#endif

    // get ---

    public int GetTrust(string merchantID)
    {
        return m_DicTrustByID.TryGetValue(merchantID, out int trust) ? trust : 0;
    }
}
