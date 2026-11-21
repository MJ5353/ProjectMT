using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 아이콘 + 개수 칸. DataDef면 무엇이든(버섯·아이템·재화) 담는다. 바탕색은 희귀도(기본 Common 회색).
// 덜 신선하면 칸 모서리에 곰팡이 얼룩(m_Mold)을 띄운다. 뱃지는 짧은 글자 표시용이다. 아이콘이 없으면 버섯은 버섯 색(Tint)으로 칠하고, 그 밖은 바탕색만 보인다
public class UICommonSlot : PoolableMonoUI
{
    [SerializeField] private Image m_Background = null; // 루트 Image(버튼 targetGraphic). 희귀도 색
    [SerializeField] private Image m_Icon = null;
    [SerializeField] private GameObject m_Mold = null; // 곰팡이 얼룩. 덜 신선할 때만 켠다
    [SerializeField] private GameObject m_Dim = null;  // 칸 위를 덮는 반투명 검정(아이콘·곰팡이 위, 위 글자 아래)
    [SerializeField] private TMP_Text m_Caption = null; // 칸 바로 위 작은 글자
    [SerializeField] private TMP_Text m_Count = null;
    [SerializeField] private TMP_Text m_NameText = null;
    [SerializeField] private GameObject m_Badge = null;
    [SerializeField] private Image m_BadgeImage = null;
    [SerializeField] private TMP_Text m_BadgeText = null;
    [SerializeField] private Button m_Button = null;

    private Action<UICommonSlot> m_OnClick;

    public string ItemID { get; private set; }
    public int Count { get; private set; }

    // ==================================================

    private void Awake()
    {
        if (m_Button != null)
            m_Button.onClick.AddListener(OnClickSlot);
    }

    public void Set(DataDef def, int count)
    {
        if (def == null)
        {
            Clear();
            return;
        }

        Color tint = def is MushroomDef mushroom ? mushroom.Tint : Color.clear;
        Set(def.ID, def.Icon, def.DisplayName, count, tint);
    }

    public void Set(string itemID, Sprite icon, string displayName, int count, Color tint)
    {
        ItemID = itemID;
        Count = count;

        m_Icon.enabled = true;
        m_Icon.sprite = icon;
        m_Icon.color = icon != null ? Color.white : tint;
        SetRarity(ERarity.Common);
        SetFreshness(EFreshness.High);
        SetDim(false);
        SetCaption(null);

        SetCount(count);

        if (m_NameText != null)
            m_NameText.text = displayName;
    }

    public void SetCount(int count)
    {
        Count = count;

        if (m_Count == null)
            return;

        m_Count.text = count > 0 ? $"x{count:N0}" : string.Empty;
    }

    public void SetFreshness(EFreshness freshness)
    {
        if (m_Mold != null)
            m_Mold.SetActive(freshness == EFreshness.Low);
    }

    public void SetDim(bool isDim)
    {
        if (m_Dim != null)
            m_Dim.SetActive(isDim);
    }

    // 빈 문자열이면 숨긴다
    public void SetCaption(string text)
    {
        if (m_Caption == null)
            return;

        bool isShow = text.E_IsNullOrEmpty() == false;
        m_Caption.gameObject.SetActive(isShow);
        if (isShow)
            m_Caption.text = text;
    }

    public void SetRarity(ERarity rarity)
    {
        if (m_Background != null)
            m_Background.color = UIStyle.GetRarityColor(rarity);
    }

    // 빈 문자열이면 뱃지를 숨긴다
    public void SetBadge(string text, Color color)
    {
        if (m_Badge == null)
            return;

        bool isShow = text.E_IsNullOrEmpty() == false;
        m_Badge.SetActive(isShow);
        if (isShow == false)
            return;

        m_BadgeText.text = text;

        if (m_BadgeImage != null)
            m_BadgeImage.color = color;
    }

    public void SetOnClick(Action<UICommonSlot> onClick)
    {
        m_OnClick = onClick;
    }

    public void Clear()
    {
        ItemID = null;
        Count = 0;

        m_Icon.enabled = false;
        SetCount(0);
        SetRarity(ERarity.Common);
        SetFreshness(EFreshness.High);
        SetDim(false);
        SetCaption(null);
        SetBadge(null, Color.clear);

        if (m_NameText != null)
            m_NameText.text = string.Empty;
    }

    private void OnClickSlot()
    {
        m_OnClick?.Invoke(this);
    }

#if UNITY_EDITOR
    public void EditorSetup(Image background, Image icon, GameObject mold, GameObject dim, TMP_Text caption, TMP_Text count, TMP_Text displayName, GameObject badge, Image badgeImage, TMP_Text badgeText, Button button)
    {
        m_Background = background;
        m_Icon = icon;
        m_Mold = mold;
        m_Dim = dim;
        m_Caption = caption;
        m_Count = count;
        m_NameText = displayName;
        m_Badge = badge;
        m_BadgeImage = badgeImage;
        m_BadgeText = badgeText;
        m_Button = button;
    }
#endif
}
