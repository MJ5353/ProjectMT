using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 시장 가운데 고른 상인 카드. 그림·이름·설명과 신뢰도·오늘 시세·선호 버섯 배율을 보여 준다.
// 값은 PopupMarket이 HuntData·MarketData에서 읽어 넘긴다

public class PopupMarket_MerchantInfo : MonoBehaviour
{
    [SerializeField] private Image m_Icon = null;
    [SerializeField] private TMP_Text m_NameText = null;
    [SerializeField] private TMP_Text m_DescText = null;
    [SerializeField] private TMP_Text m_InfoText = null; // 신뢰도·오늘 시세·좋아하는 버섯

    // ------------------------------------------------

    // trustGain은 이번 시장에서 오른 신뢰도(닫힐 때 반영), swingRate는 오늘 시세 배율(1이 보통)
    public void Set(MerchantDef merchant, int trust, int trustGain, float swingRate)
    {
        m_Icon.E_SetImage(merchant.Icon);
        m_NameText.E_SetText(merchant.DisplayName);
        m_DescText.E_SetText(merchant.Description);

        string gainText = trustGain > 0 ? " " + UIStyle.ColorTag(UIStyle.Positive, $"(+{trustGain})") : string.Empty;
        string otherText = merchant.OtherRate > 0f ? $"x{merchant.OtherRate:0.0#}" : UIStyle.ColorTag(UIStyle.Negative, StringDef.S_Get(EStringKey.Market_NotBuying));
        m_InfoText.E_SetString(EStringKey.Market_MerchantInfoFormat, trust, gainText, FormatSwing(swingRate), FormatPrefer(merchant), merchant.PreferRate, otherText);
    }

    // 오늘 온 상인이 없다
    public void SetEmpty()
    {
        m_Icon.E_SetImage(null);
        m_NameText.E_SetString(EStringKey.Market_NoMerchant);
        m_DescText.E_SetText(string.Empty);
        m_InfoText.E_SetText(string.Empty);
    }

    // ---

    // 선호 희귀도 범위: "희귀 이상" / "영웅만" / "일반~희귀"
    private static string FormatPrefer(MerchantDef merchant)
    {
        string minName = GetRarityName(merchant.PreferRarity);
        if (merchant.PreferRarity == merchant.PreferRarityMax)
            return StringDef.S_Format(EStringKey.Market_PreferOnlyFormat, minName);

        if (merchant.PreferRarityMax == ERarity.Epic)
            return StringDef.S_Format(EStringKey.Market_PreferAboveFormat, minName);

        return $"{minName}~{GetRarityName(merchant.PreferRarityMax)}";
    }

    // 표가 없으면 enum 이름
    private static string GetRarityName(ERarity rarity)
    {
        return RarityDef.S_Get(rarity)?.DisplayName ?? rarity.ToString();
    }

    // 오늘 시세: +12%(초록) / -8%(빨강) / 보통
    private static string FormatSwing(float swingRate)
    {
        int swing = Mathf.RoundToInt((swingRate - 1f) * 100f);

        if (swing > 0)
            return UIStyle.ColorTag(UIStyle.Positive, $"+{swing}%");
        if (swing < 0)
            return UIStyle.ColorTag(UIStyle.Negative, $"{swing}%");

        return StringDef.S_Get(EStringKey.Market_SwingNormal);
    }
}
