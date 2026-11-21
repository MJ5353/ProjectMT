using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 시장 남은 버섯 칸을 한 번 더 누르면 뜨는 개수 조절 팝업. 0~가진 개수 사이에서 판매 개수를 정한다.
// 확인하면 콜백으로 개수만 돌려준다. 계산·판정은 HuntData.CheckTrade가 한다. 뒤로 가기로 닫으면 취소

public class PopupMarketCounter : PopupBase
{
    private const int StepSmall = 1;
    private const int StepLarge = 10;

    [SerializeField] private UICommonSlot m_Slot = null;
    [SerializeField] private TMP_Text m_NameText = null;
    [SerializeField] private TMP_Text m_CountText = null;  // "판매 10 / 17"
    [SerializeField] private TMP_Text m_ChangeText = null; // "17→10"
    [SerializeField] private TMP_Text m_PriceText = null;  // 한 개 값 · 합계
    [SerializeField] private Button m_Minus10Button = null;
    [SerializeField] private Button m_Minus1Button = null;
    [SerializeField] private Button m_Plus1Button = null;
    [SerializeField] private Button m_Plus10Button = null;
    [SerializeField] private Button m_AllButton = null;
    [SerializeField] private Button m_CancelButton = null;
    [SerializeField] private Button m_ConfirmButton = null;

    private int m_Have;
    private int m_Count;
    private int m_Price;
    private Action<int> m_OnConfirm;

    // ------------------------------------------------

    private void Awake()
    {
        m_Minus10Button.E_SetOnClick(() => AddCount(-StepLarge));
        m_Minus1Button.E_SetOnClick(() => AddCount(-StepSmall));
        m_Plus1Button.E_SetOnClick(() => AddCount(StepSmall));
        m_Plus10Button.E_SetOnClick(() => AddCount(StepLarge));
        m_AllButton.E_SetOnClick(() => SetCount(m_Have));
        m_CancelButton.E_SetOnClick(Close);
        m_ConfirmButton.E_SetOnClick(OnClickConfirm);
    }

    // price가 0 이하면 이 상인이 사지 않는 버섯이다
    public void Setup(LootKey key, int have, int count, int price, Action<int> onConfirm)
    {
        m_Have = Mathf.Max(0, have);
        m_Price = price;
        m_OnConfirm = onConfirm;

        GameDatabase.S_TryGet(key.ItemID, out MushroomDef def); // 없으면 def는 null
        m_NameText.E_SetText(def.E_Alive()?.DisplayName ?? key.ItemID);
        m_Slot.SetLoot(key, def, m_Have, string.Empty);

        SetCount(count);
    }

    // ---

    private void AddCount(int delta)
    {
        SetCount(m_Count + delta);
    }

    private void SetCount(int count)
    {
        m_Count = Mathf.Clamp(count, 0, m_Have);

        m_CountText.E_SetText($"판매 {m_Count} / {m_Have}");
        m_ChangeText.E_SetText($"{m_Have}→{m_Have - m_Count}");
        m_PriceText.E_SetText(m_Price > 0 ? $"한 개 {m_Price}G  ·  합계 {m_Price * m_Count:N0}G" : UIStyle.ColorTag(UIStyle.Negative, "이 상인은 사지 않는다"));

        bool canMinus = m_Count > 0;
        bool canPlus = m_Count < m_Have;
        m_Minus10Button.E_SetInteractable(canMinus);
        m_Minus1Button.E_SetInteractable(canMinus);
        m_Plus1Button.E_SetInteractable(canPlus);
        m_Plus10Button.E_SetInteractable(canPlus);
        m_AllButton.E_SetInteractable(canPlus);
    }

    private void OnClickConfirm()
    {
        Action<int> onConfirm = m_OnConfirm;
        int count = m_Count;
        Close();
        onConfirm?.Invoke(count);
    }
}
