using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 시장 남은 버섯 칸을 한 번 더 누르면 뜨는 개수 조절 팝업. 0~가진 개수 사이에서 판매 개수를 정한다.
// 확인하면 콜백으로 개수만 돌려준다. 계산·판정은 HuntData.CheckTrade가 한다. 뒤로 가기로 닫으면 취소

public class PopupMarket_Count : Popup
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
        m_Minus10Button.onClick.AddListener(() => AddCount(-StepLarge));
        m_Minus1Button.onClick.AddListener(() => AddCount(-StepSmall));
        m_Plus1Button.onClick.AddListener(() => AddCount(StepSmall));
        m_Plus10Button.onClick.AddListener(() => AddCount(StepLarge));
        m_AllButton.onClick.AddListener(() => SetCount(m_Have));
        m_CancelButton.onClick.AddListener(Close);
        m_ConfirmButton.onClick.AddListener(OnClickConfirm);
    }

    // price가 0 이하면 이 상인이 사지 않는 버섯이다
    public void Setup(LootKey key, int have, int count, int price, Action<int> onConfirm)
    {
        m_Have = Mathf.Max(0, have);
        m_Price = price;
        m_OnConfirm = onConfirm;

        MushroomDef def = null;
        GameManager.Instance?.Database?.TryGet(key.ItemID, out def);
        m_NameText.text = def != null ? def.DisplayName : key.ItemID;
        PopupMarket.SetLootSlot(m_Slot, key, m_Have, string.Empty);

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

        m_CountText.text = $"판매 {m_Count} / {m_Have}";
        m_ChangeText.text = $"{m_Have}→{m_Have - m_Count}";
        m_PriceText.text = m_Price > 0 ? $"한 개 {m_Price}G  ·  합계 {m_Price * m_Count:N0}G" : UIStyle.ColorTag(UIStyle.Negative, "이 상인은 사지 않는다");

        m_Minus10Button.interactable = m_Minus1Button.interactable = m_Count > 0;
        m_Plus1Button.interactable = m_Plus10Button.interactable = m_AllButton.interactable = m_Count < m_Have;
    }

    private void OnClickConfirm()
    {
        Action<int> onConfirm = m_OnConfirm;
        int count = m_Count;
        Close();
        onConfirm?.Invoke(count);
    }
}
