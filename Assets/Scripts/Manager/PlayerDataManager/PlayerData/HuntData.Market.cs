using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 시장 요청 결과. Success가 아니면 아무것도 바뀌지 않는다
public enum EMarketResult
{
    Success,
    Closed,     // 시장이 열려 있지 않거나 밤이 다 갔다
    NoMerchant, // 오늘 온 상인이 아니다
    Empty,      // 고른 버섯이 없다
    OverLimit,  // 거래 한 번 묶음 상한(Define.MarketTradeLimit)을 넘었다
    NotEnough,  // 가진 것보다 많이 골랐다
    NotBuying,  // 이 상인이 사지 않는 버섯이 섞였다
}

// ==================================================

// 주운 버섯 묶음 키. 같은 버섯도 신선도·희귀도가 다르면 따로 센다(HUD 목록, 시장 판매가)
public readonly struct LootKey : System.IEquatable<LootKey>
{
    public readonly string ItemID;
    public readonly EFreshness Freshness;
    public readonly ERarity Rarity;

    public LootKey(string itemID, EFreshness freshness, ERarity rarity)
    {
        ItemID = itemID;
        Freshness = freshness;
        Rarity = rarity;
    }

    public bool Equals(LootKey other) => ItemID == other.ItemID && Freshness == other.Freshness && Rarity == other.Rarity;
    public override bool Equals(object obj) => obj is LootKey other && Equals(other);
    public override int GetHashCode() => System.HashCode.Combine(ItemID, Freshness, Rarity);
}

// ==================================================

// 거래 한 번에 파는 버섯 한 묶음
public readonly struct MarketItem
{
    public readonly LootKey Key;
    public readonly int Count;

    public MarketItem(LootKey key, int count)
    {
        Key = key;
        Count = count;
    }
}

// ==================================================

// HuntData의 현장 시장 부분(하룻밤 임시값). UI는 요청(Trade·Wait·EndMarket)만 보내고 계산과 결과는 여기서 정한다(나중에 서버로 옮길 부분).
// 시장 상태는 저장하지 않는다. 상인 신뢰도 저장은 MarketData가 맡는다
public partial class HuntData
{
    private readonly Dictionary<LootKey, int> m_DicHarvest = new();   // 아직 못 판 버섯
    private readonly List<MerchantDef> m_ListMerchant = new();        // 오늘 와 있는 상인
    private readonly Dictionary<string, int> m_DicTrustGain = new();  // 이번 시장에서 오른 신뢰도. 닫힐 때 반영하고, 결과 화면이 읽도록 다음 시장까지 남긴다
    private readonly Dictionary<string, float> m_DicSwingRate = new(); // 상인별 오늘 시세 배율. 기다릴 때마다 MerchantDef.PriceSwing 폭으로 바뀐다
    private readonly Dictionary<LootKey, int> m_DicTradeBuffer = new(); // Validate가 채우는 거래 묶음. 같은 버섯을 나눠 골라도 합쳐 센다
    private readonly List<MerchantDef> m_ListCandidate = new();       // 상인 뽑기용

    public bool IsMarketOpen { get; private set; }
    public float RemainTime { get; private set; }   // 남은 밤(초)
    public float NightTime { get; private set; }    // 밤 전체 길이(초). 시계 표시용
    public int SoldGold { get; private set; }       // 이번 시장에서 판 골드. 닫힐 때 인벤토리에 넣는다
    public int RottenCount { get; private set; }    // 닫힐 때 썩은 버섯 수

    public IReadOnlyDictionary<LootKey, int> DicHarvest => m_DicHarvest;
    public IReadOnlyList<MerchantDef> ListMerchant => m_ListMerchant;
    public IReadOnlyDictionary<string, int> DicTrustGain => m_DicTrustGain;
    public bool CanAct => IsMarketOpen && RemainTime > 0f;
    private static DefineDef Define => GameManager.Instance.Define; // 조절값(Define.asset)

    // ------------------------------------------------

    // 토벌 종료 버튼. 주운 버섯과 남은 밤을 받아 시장을 연다
    public void BeginMarket(IReadOnlyDictionary<LootKey, int> dicLoot, float remainTime, float nightTime)
    {
        ClearMarket();

        foreach (var pair in dicLoot)
        {
            if (pair.Value > 0)
                m_DicHarvest[pair.Key] = pair.Value;
        }

        RemainTime = Mathf.Max(0f, remainTime);
        NightTime = nightTime;
        IsMarketOpen = true;

        RollMerchants();
        SEventManager.Publish(new MarketChanged());
    }

    // 거래. 성공하면 버섯이 빠지고 골드·신뢰도가 쌓이며 밤이 Define.MarketActionTime만큼 줄어든다(MarketChanged).
    // gold는 이번 거래로 받은 골드. 실패하면 0이고 아무것도 바뀌지 않는다
    public EMarketResult Trade(MerchantDef merchant, IReadOnlyList<MarketItem> listItem, out int gold)
    {
        EMarketResult result = Validate(merchant, listItem, out gold);
        if (result != EMarketResult.Success)
            return result;

        foreach (var pair in m_DicTradeBuffer)
        {
            int count = m_DicHarvest[pair.Key] - pair.Value;
            if (count <= 0)
                m_DicHarvest.Remove(pair.Key);
            else
                m_DicHarvest[pair.Key] = count;
        }

        SoldGold += gold;
        m_DicTrustGain.TryGetValue(merchant.ID, out int gain);
        m_DicTrustGain[merchant.ID] = gain + Define.TrustPerTrade;

        SpendTime();
        return EMarketResult.Success;
    }

    // 거래할 수 있는지와 받을 골드. UI가 [거래] 버튼 상태와 합계 표시에 미리 쓴다
    public EMarketResult CheckTrade(MerchantDef merchant, IReadOnlyList<MarketItem> listItem, out int gold)
    {
        return Validate(merchant, listItem, out gold);
    }

    // 거래 판정. 고른 묶음을 m_DicTradeBuffer에 합쳐 두고, 성공이면 Trade가 그 버퍼로 버섯을 뺀다
    private EMarketResult Validate(MerchantDef merchant, IReadOnlyList<MarketItem> listItem, out int gold)
    {
        gold = 0;
        if (CanAct == false)
            return EMarketResult.Closed;
        if (merchant == null || m_ListMerchant.Contains(merchant) == false)
            return EMarketResult.NoMerchant;

        m_DicTradeBuffer.Clear();
        int total = 0;
        foreach (MarketItem item in listItem)
        {
            if (item.Count <= 0)
                continue;

            m_DicTradeBuffer.TryGetValue(item.Key, out int count);
            m_DicTradeBuffer[item.Key] = count + item.Count;
            total += item.Count;
        }

        if (total == 0)
            return EMarketResult.Empty;
        if (m_DicTradeBuffer.Count > Define.MarketTradeLimit) // 묶음(LootKey) 수로 센다. 묶음마다 개수 제한은 없다
            return EMarketResult.OverLimit;

        int sum = 0;
        foreach (var pair in m_DicTradeBuffer)
        {
            m_DicHarvest.TryGetValue(pair.Key, out int have);
            if (have < pair.Value)
                return EMarketResult.NotEnough;

            int price = GetPrice(merchant, pair.Key);
            if (price <= 0)
                return EMarketResult.NotBuying;

            sum += price * pair.Value;
        }

        gold = sum;
        return EMarketResult.Success;
    }

    // 30분 기다리기. 상인마다 시세가 바뀐다
    public EMarketResult Wait()
    {
        if (CanAct == false)
            return EMarketResult.Closed;

        foreach (MerchantDef merchant in m_ListMerchant)
        {
            m_DicSwingRate[merchant.ID] = 1f + CRandom.Range(-merchant.PriceSwing, merchant.PriceSwing);
        }

        SpendTime();
        return EMarketResult.Success;
    }

    // 시장을 닫는다. 판 골드와 신뢰도를 반영하고 못 판 버섯은 썩은 버섯으로 넣는다.
    // 밤이 다 가면 스스로 닫힌다. 저장과 결과 화면 전환은 MarketClosed를 받은 쪽이 한다
    public void EndMarket()
    {
        if (IsMarketOpen == false)
            return;

        IsMarketOpen = false;

        RottenCount = 0;
        foreach (var pair in m_DicHarvest)
        {
            RottenCount += pair.Value;
        }

        m_DicHarvest.Clear();
        m_Inventory.Add(CurrencyDef.GoldID, SoldGold);
        m_Inventory.Add(ItemDef.RottenID, RottenCount);

        foreach (var pair in m_DicTrustGain)
        {
            m_Market.AddTrust(pair.Key, pair.Value);
        }

        SEventManager.Publish(new MarketClosed(SoldGold, RottenCount));
    }

    // 이 상인이 이 버섯 한 개에 쳐주는 값. 0이면 사지 않는다.
    // 기본가 × 상인 배율(희귀도 기준) × 신선도 배율 × 신뢰도 보정 × 오늘 시세
    public int GetPrice(MerchantDef merchant, LootKey key)
    {
        if (GameDatabase.S_TryGet(key.ItemID, out MushroomDef mushroom) == false)
            return 0;

        float rate = merchant.GetRate(key.Rarity);
        if (rate <= 0f || mushroom.Price <= 0)
            return 0;

        float rarityRate = key.Rarity switch
        {
            ERarity.Rare => Define.MarketRareRate,
            ERarity.Epic => Define.MarketEpicRate,
            _ => 1f,
        };
        float freshRate = key.Freshness == EFreshness.High ? 1f : Define.MarketLowFreshRate;
        float trustRate = 1f + m_Market.GetTrust(merchant.ID) * Define.TrustPriceRate;
        float swingRate = Mathf.Max(0f, GetSwingRate(merchant));

        return Mathf.Max(1, Mathf.RoundToInt(mushroom.Price * rarityRate * rate * freshRate * trustRate * swingRate));
    }

    // 이번 시장에서 이 상인에게 오른 신뢰도(닫힐 때 반영)
    public int GetTrustGain(string merchantID)
    {
        return m_DicTrustGain.TryGetValue(merchantID, out int gain) ? gain : 0;
    }

    // 상인의 오늘 시세 배율. 시장을 연 직후는 1
    public float GetSwingRate(MerchantDef merchant)
    {
        return m_DicSwingRate.TryGetValue(merchant.ID, out float rate) ? rate : 1f;
    }

    private void SpendTime()
    {
        RemainTime = Mathf.Max(0f, RemainTime - Define.MarketActionTime);
        SEventManager.Publish(new MarketChanged());

        if (RemainTime <= 0f)
            EndMarket();
    }

    // 신뢰도로 가중치를 매겨 겹치지 않게 Define.MarketMerchantCount명을 뽑는다
    private void RollMerchants()
    {
        m_ListMerchant.Clear();
        m_ListCandidate.Clear();

        foreach (MerchantDef merchant in GameManager.Instance.Database.GetAll<MerchantDef>())
        {
            m_ListCandidate.Add(merchant);
        }

        int count = Mathf.Min(Define.MarketMerchantCount, m_ListCandidate.Count);
        for (int i = 0; i < count; ++i)
        {
            int index = Mathf.Max(0, CRandom.PickIndex(m_ListCandidate, GetVisitWeight)); // 가중치가 모두 0이면 앞에서부터
            m_ListMerchant.Add(m_ListCandidate[index]);
            m_ListCandidate.RemoveAt(index);
        }

        m_ListCandidate.Clear();
    }

    // 밤마다 상인을 뽑을 때 쓰는 가중치. 신뢰도가 오를수록 커진다
    private float GetVisitWeight(MerchantDef merchant)
    {
        return Mathf.Max(0f, merchant.VisitWeight) * (1f + m_Market.GetTrust(merchant.ID) * Define.TrustVisitRate);
    }

    private void ClearMarket()
    {
        IsMarketOpen = false;
        RemainTime = 0f;
        NightTime = 0f;
        SoldGold = 0;
        RottenCount = 0;

        m_DicHarvest.Clear();
        m_ListMerchant.Clear();
        m_DicTrustGain.Clear();
        m_DicSwingRate.Clear();
        m_DicTradeBuffer.Clear();
        m_ListCandidate.Clear();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 개발용(DebugPanel) ---

    public void DebugAddTime(float seconds)
    {
        if (IsMarketOpen == false)
            return;

        RemainTime = Mathf.Max(0f, RemainTime + seconds);
        SEventManager.Publish(new MarketChanged());
    }

    public void DebugRerollMerchants()
    {
        if (IsMarketOpen == false)
            return;

        m_DicSwingRate.Clear();
        RollMerchants();
        SEventManager.Publish(new MarketChanged());
    }
#endif
}
