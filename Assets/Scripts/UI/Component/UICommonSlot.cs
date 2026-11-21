using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 아이콘 + 개수 칸. DataDef면 무엇이든(버섯·아이템·재화) 담는다. 바탕은 희귀도별 그림(RarityDef, 기본 Common).
// 표시만 한다. 어떤 데이터인지는 칸을 꺼낸 UI가 들고 있다(클릭은 UIListView.IndexOf로 찾는다).
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
    // → RarityDef로 옮김(희귀도 → 바탕 그림·색·이름). 경로 대신 Def가 그림을 직접 참조한다

    private Action<UICommonSlot> m_OnClick;

    // ------------------------------------------------
    
    private void Awake()
    {
        m_Button.E_SetOnClick(OnClickSlot); // 버튼이 없는 칸이면 넘어간다
    }

    public void Clear()
    {
        m_Icon.enabled = false;

        SetCount(0);
        SetRarity(ERarity.Common);
        SetFreshness(EFreshness.High);
        SetDim(false);
        SetCaption(null);

        m_NameText.E_SetText(string.Empty);
    }
    
    public void Set(DataDef def, int count)
    {
        if (def == null)
        {
            Clear();
            return;
        }

        Set(def.ID, def, count);
    }

    // def가 없어도 ID로 칸을 채운다(아이콘 없이 회색 칸, 이름 자리엔 ID). displayName이 null이면 def 이름
    public void Set(string itemID, DataDef def, int count, string displayName = null)
    {
        DataDef alive = def.E_Alive();
        Color tint = UIStyle.PanelLight;
        
        // 버섯은 버섯 색, 다른 데이터는 투명, 데이터가 없으면 회색 칸
        if (alive is MushroomDef mushroom)
            tint = mushroom.Tint;
        else if (alive != null)
            tint = Color.clear;

        displayName = displayName ?? alive?.DisplayName ?? itemID;
        Set(itemID, alive?.Icon, displayName, count, tint);
    }

    // 주운 버섯 칸: 아이콘·개수 + 희귀도 바탕 + 덜 신선하면 곰팡이. bottomText는 칸 아래 글자(값 등)
    public void SetLoot(LootKey key, MushroomDef def, int count, string bottomText)
    {
        Set(key.ItemID, def, count, bottomText);
        SetRarity(key.Rarity);
        SetFreshness(key.Freshness);
    }

    public void Set(string itemID, Sprite icon, string displayName, int count, Color tint)
    {
        m_Icon.enabled = true;
        m_Icon.sprite = icon;
        m_Icon.color = icon != null ? Color.white : tint;

        SetRarity(ERarity.Common);
        SetFreshness(EFreshness.High);
        SetDim(false);
        SetCaption(null);
        SetCount(count);
        
        m_NameText.E_SetText(displayName);
    }

    // set ---
    
    public void SetCount(int count)
    {
        m_Count.E_SetText(count > 0 ? $"x{count:N0}" : string.Empty);
    }

    public void SetFreshness(EFreshness freshness)
    {
        m_Mold.E_SetActive(freshness == EFreshness.Low);
    }

    public void SetDim(bool isDim)
    {
        m_Dim.E_SetActive(isDim);
    }

    public void SetCaption(string text)
    {
        m_Caption.E_SetTextWithActive(text); // 비었으면 끈다
    }

    // 그림이 있으면 그림 그대로, 없으면 희귀도 색만 칠한다
    public void SetRarity(ERarity rarity)
    {
        RarityDef.Entry entry = RarityDef.S_Get(rarity);
        Sprite sprite = entry?.SlotSprite;
        Color color = sprite.E_Alive() ? Color.white : entry?.Color ?? Color.white;
        
        m_Background.E_SetImage(sprite, color);
    }

    public void SetOnClick(Action<UICommonSlot> onClick)
    {
        m_OnClick = onClick;
    }
    
    // virtual ---

    // 다른 화면이 쓰던 칸일 수 있다. 클릭은 꺼낸 쪽이 다시 연결한다(안 하면 눌러도 아무 일 없음)
    public override void OnPoolSpawn()
    {
        base.OnPoolSpawn();

        m_OnClick = null;
    }

    // on action ---
    
    private void OnClickSlot()
    {
        m_OnClick?.Invoke(this);
    }

#if UNITY_EDITOR
    public void EditorSetup(Image background, Image icon, GameObject mold, GameObject dim, TMP_Text caption, TMP_Text count, TMP_Text displayName, Button button)
    {
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
