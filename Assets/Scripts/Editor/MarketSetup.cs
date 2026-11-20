using System.IO;
using UnityEditor;
using UnityEngine;

// ==================================================

// 시장 샘플 상인 생성. 이미 있는 상인 에셋은 건드리지 않고 없는 것만 만든 뒤 DB를 갱신한다.
// 수치는 가안이며 만든 뒤 데이터 편집기나 인스펙터에서 고친다
public static class MarketSetup
{
    private const string MerchantDir = "Assets/Resources/Data/Merchants/";

    private readonly struct Sample
    {
        public readonly string ID;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly ERarity PreferRarity;
        public readonly float PreferRate;
        public readonly float OtherRate;
        public readonly float VisitWeight;
        public readonly float PriceSwing;

        public Sample(string merchantID, string displayName, string description, ERarity preferRarity, float preferRate, float otherRate, float visitWeight, float priceSwing)
        {
            ID = merchantID;
            DisplayName = displayName;
            Description = description;
            PreferRarity = preferRarity;
            PreferRate = preferRate;
            OtherRate = otherRate;
            VisitWeight = visitWeight;
            PriceSwing = priceSwing;
        }
    }

    private static readonly Sample[] s_Samples =
    {
        new("Merchant_001_Herbalist", "약재상", "귀한 버섯만 비싸게 산다", ERarity.Rare, 2f, 0.5f, 1f, 0.15f),
        new("Merchant_002_Cook", "요리사", "아무거나 사지만 싸게 산다", ERarity.Common, 0.8f, 0.8f, 1.5f, 0.1f),
        new("Merchant_003_Collector", "수집가", "귀한 버섯만 아주 비싸게 산다. 드물게 온다", ERarity.Rare, 3f, 0f, 0.5f, 0.4f),
        new("Merchant_004_Peddler", "떠돌이 상인", "무엇이든 제값에 산다", ERarity.Common, 1f, 1f, 1f, 0.3f),
    };

    // ------------------------------------------------

    [MenuItem("MushTaker/시장/샘플 상인 만들기")]
    public static void CreateSamples()
    {
        Directory.CreateDirectory(MerchantDir);

        int created = 0;
        foreach (Sample sample in s_Samples)
        {
            string path = $"{MerchantDir}{sample.ID}.asset";
            if (AssetDatabase.LoadAssetAtPath<MerchantDef>(path) != null)
                continue;

            MerchantDef merchant = ScriptableObject.CreateInstance<MerchantDef>();
            merchant.EditorSetIdentity(sample.ID, sample.DisplayName, sample.Description);
            merchant.EditorSetup(sample.PreferRarity, sample.PreferRate, sample.OtherRate, sample.VisitWeight, sample.PriceSwing);
            AssetDatabase.CreateAsset(merchant, path);
            ++created;
        }

        AssetDatabase.SaveAssets();
        DataTools.RefreshDatabase(true);
        CLogger.Log("MarketSetup", $"샘플 상인 {created}개 생성");
    }
}
