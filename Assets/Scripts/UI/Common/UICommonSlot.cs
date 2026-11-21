using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 아이콘 + 개수 칸. DataDef면 무엇이든(버섯·아이템·재화) 담는다. 바탕은 희귀도별 스프라이트(기본 Common 회색).
// 덜 신선하면 칸 모서리에 곰팡이 얼룩(m_Mold)을 띄운다. 아이콘이 없으면 버섯은 버섯 색(Tint)으로 칠하고, 그 밖은 바탕색만 보인다
public class UICommonSlot : PoolableMonoUI
{
    [SerializeField] private Button m_Button = null;
    [SerializeField] private Image m_Icon = null;
    [SerializeField] private Image m_Background = null; // 루트 Image(버튼 targetGraphic). 희귀도 색
    [SerializeField] private GameObject m_Dim = null;  // 칸 위를 덮는 반투명 검정(아이콘·곰팡이 위, 위 글자 아래)
    
    [SerializeField] private TMP_Text m_Caption = null; // 칸 바로 위 작은 글자
    [SerializeField] private TMP_Text m_Count = null;
    [SerializeField] private TMP_Text m_NameText = null;

    // [mj] todo) 일반화. slot layer table 만들어서 slot cover, slot extra bg 등을 설정할 수 있게
    [SerializeField] private GameObject m_Mold = null; // 곰팡이 얼룩. 덜 신선할 때만 켠다

    // [mj] todo) rarity table로 연결해서 path로 가져오게...
    [SerializeField] private Sprite[] m_RaritySprites = null; // ERarity 순서. 없으면 희귀도 색만 칠한다

    private Action<UICommonSlot> m_OnClick;

    public string ItemID { get; private set; }
    public int Count { get; private set; }

    // ------------------------------------------------
    
    private void Awake()
    {
        if (m_Button != null)
            m_Button.onClick.AddListener(OnClickSlot);
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

        if (m_NameText != null)
            m_NameText.text = string.Empty;
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

    // set ---
    
    public void SetCount(int count)
    {
        Count = count;

        if (m_Count == null)
            return;

        m_Count.text = count > 0 ? $"x{count:N0}" : string.Empty;
    }

    public void SetFreshness(EFreshness freshness)
    {
        // [mj] todo) common extension 추가해서 통일 - null check + activate 
        m_Mold.SetActive(freshness == EFreshness.Low);
    }

    public void SetDim(bool isDim)
    {
        // [mj] todo) common extension 추가해서 통일 - null check + activate 
        m_Dim.SetActive(isDim);
    }

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
        if (m_Background == null)
            return;

        int index = (int)rarity;
        bool hasSprite = m_RaritySprites != null && index < m_RaritySprites.Length && m_RaritySprites[index] != null;

        // [mj] todo) 이런거 null check + 할당용 common extension 만들어서 일괄 통일
        m_Background.sprite = hasSprite ? m_RaritySprites[index] : null;
        m_Background.color = hasSprite ? Color.white : UIStyle.GetRarityColor(rarity);
    }

    public void SetOnClick(Action<UICommonSlot> onClick)
    {
        m_OnClick = onClick;
    }
    
    // on action --- 
    
    private void OnClickSlot()
    {
        m_OnClick?.Invoke(this);
    }

#if UNITY_EDITOR
    public void EditorSetup(Image background, Image icon, GameObject mold, GameObject dim, TMP_Text caption, TMP_Text count, TMP_Text displayName, Button button, Sprite[] raritySprites)
    {
        m_RaritySprites = raritySprites;
        m_Background = background;
        m_Icon = icon;
        m_Mold = mold;
        m_Dim = dim;
        m_Caption = caption;
        m_Count = count;
        m_NameText = displayName;
        m_Button = button;
    }
#endif
}
