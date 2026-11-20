using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 토벌 종료 뒤 현장 시장 팝업. 왼쪽 오늘 온 상인, 가운데 고른 상인, 오른쪽 위 고른 버섯·합계·[거래], 오른쪽 아래 남은 버섯.
// 계산과 결과는 HuntData가 정하고 여기서는 요청(Trade·Wait·EndMarket)만 보낸다. 시장이 닫히면(MarketClosed) 스스로 닫힌다.
// 결과 화면 전환과 저장은 MarketClosed를 받은 쪽(BattleService)이 한다

public class MarketPopup : Popup
{
    [SerializeField] private UINightClock m_Clock = null;

    [Header("상인")]
    [SerializeField] private RectTransform m_MerchantList = null;     // 세로 목록(VerticalLayoutGroup)
    [SerializeField] private UIMerchantRow m_MerchantTemplate = null; // 목록 밖에 꺼 둔 줄 원본
    [SerializeField] private Image m_MerchantIcon = null;
    [SerializeField] private TMP_Text m_MerchantName = null;
    [SerializeField] private TMP_Text m_MerchantDesc = null;
    [SerializeField] private TMP_Text m_MerchantInfo = null;          // 신뢰도·오늘 시세

    [Header("거래")]
    [SerializeField] private RectTransform m_PickList = null;  // 고른 버섯(HorizontalLayoutGroup)
    [SerializeField] private RectTransform m_StockList = null; // 남은 버섯(GridLayoutGroup)
    [SerializeField] private UIItemSlot m_SlotTemplate = null; // 목록 밖에 꺼 둔 칸 원본
    [SerializeField] private TMP_Text m_TotalText = null;
    [SerializeField] private TMP_Text m_MessageText = null;
    [SerializeField] private TMP_Text m_SoldText = null;
    [SerializeField] private Button m_TradeButton = null;
    [SerializeField] private Button m_WaitButton = null;
    [SerializeField] private TMP_Text m_WaitLabel = null;
    [SerializeField] private Button m_CloseButton = null;

    // 희귀도 색(뱃지). 바닥 고리 색이 정해지면 맞춘다
    private static readonly Color RarityCommon = UIStyle.Hex(0xA9B39C);
    private static readonly Color RarityRare = UIStyle.Hex(0x4FA3E0);
    private static readonly Color RarityEpic = UIStyle.Hex(0xB45EE0);

    private readonly List<UIMerchantRow> m_ListRow = new();
    private readonly List<UIItemSlot> m_ListStockSlot = new();
    private readonly List<UIItemSlot> m_ListPickSlot = new();
    private readonly List<LootKey> m_ListStockKey = new();   // m_ListStockSlot 순서
    private readonly List<LootKey> m_ListPickKey = new();    // 고른 순서. m_ListPickSlot 순서
    private readonly Dictionary<LootKey, int> m_DicPick = new();
    private readonly List<MarketItem> m_ListItem = new();    // HuntData 요청용

    private int m_SelectedIndex;

    private static HuntData Hunt => PlayerDataManager.Instance?.Hunt;
    private static DefineDef Define => GameManager.Instance.Define; // 조절값(Define.asset)

    public override bool IsCloseOnBack => false; // [시장 닫기]로만 닫는다

    // ------------------------------------------------

    private void Awake()
    {
        m_TradeButton.onClick.AddListener(OnClickTrade);
        m_WaitButton.onClick.AddListener(OnClickWait);
        m_CloseButton.onClick.AddListener(OnClickClose);
    }

    private void Update()
    {
        HuntData hunt = Hunt;
        if (hunt != null)
            m_Clock.SetTime(hunt.RemainTime, hunt.NightTime);
    }

    // virtual ---

    public override void OnOpen()
    {
        SEventManager.Subscribe<MarketChanged>(OnMarketChanged);
        SEventManager.Subscribe<MarketClosed>(OnMarketClosed);

        m_SelectedIndex = 0;
        m_DicPick.Clear();
        m_ListPickKey.Clear();
        m_WaitLabel.text = $"{Define.MarketActionMinutes}분 기다리기";
        SetMessage(string.Empty);

        HuntData hunt = Hunt;
        if (hunt != null)
            m_Clock.SetTime(hunt.RemainTime, hunt.NightTime);

        m_Clock.SetMarket(true, true);
        Refresh();
    }

    public override void OnClose()
    {
        SEventManager.Unsubscribe<MarketChanged>(OnMarketChanged);
        SEventManager.Unsubscribe<MarketClosed>(OnMarketClosed);
    }

    // ---

    private MerchantDef SelectedMerchant
    {
        get
        {
            HuntData hunt = Hunt;
            if (hunt == null || m_SelectedIndex < 0 || m_SelectedIndex >= hunt.ListMerchant.Count)
                return null;

            return hunt.ListMerchant[m_SelectedIndex];
        }
    }

    private void Refresh()
    {
        HuntData hunt = Hunt;
        if (hunt == null)
            return;

        RemoveMissingPick(hunt);
        RefreshMerchants(hunt);
        RefreshStock(hunt);
        RefreshPick(hunt);
        RefreshTotal(hunt);

        m_SoldText.text = $"판 골드 {hunt.SoldGold}";
        m_WaitButton.interactable = hunt.CanAct;
    }

    private void RefreshMerchants(HuntData hunt)
    {
        MarketData market = PlayerDataManager.Instance.Market;
        IReadOnlyList<MerchantDef> listMerchant = hunt.ListMerchant;

        for (int i = 0; i < listMerchant.Count; ++i)
        {
            if (i >= m_ListRow.Count)
            {
                UIMerchantRow row = Instantiate(m_MerchantTemplate, m_MerchantList);
                row.SetOnClick(OnClickMerchant);
                m_ListRow.Add(row);
            }

            m_ListRow[i].gameObject.SetActive(true);
            m_ListRow[i].Set(i, listMerchant[i], market.GetTrust(listMerchant[i].ID), Define.TrustMax, i == m_SelectedIndex);
        }

        for (int i = listMerchant.Count; i < m_ListRow.Count; ++i)
        {
            m_ListRow[i].gameObject.SetActive(false);
        }

        MerchantDef merchant = SelectedMerchant;
        if (merchant == null)
        {
            m_MerchantIcon.enabled = false;
            m_MerchantName.text = "오늘은 상인이 없다";
            m_MerchantDesc.text = string.Empty;
            m_MerchantInfo.text = string.Empty;
            return;
        }

        m_MerchantIcon.enabled = merchant.Icon != null;
        m_MerchantIcon.sprite = merchant.Icon;
        m_MerchantName.text = merchant.DisplayName;
        m_MerchantDesc.text = merchant.Description;

        int swing = Mathf.RoundToInt((hunt.GetSwingRate(merchant) - 1f) * 100f);
        string swingText = swing > 0 ? $"<color=#7BC950>+{swing}%</color>" : swing < 0 ? $"<color=#D9573B>{swing}%</color>" : "보통";
        m_MerchantInfo.text = $"신뢰도 {market.GetTrust(merchant.ID)}  ·  오늘 시세 {swingText}";
    }

    // 남은 버섯 = 가진 수 - 고른 수. 칸 아래에 고른 상인의 한 개 값을 쓴다
    private void RefreshStock(HuntData hunt)
    {
        m_ListStockKey.Clear();
        foreach (var pair in hunt.DicHarvest)
        {
            m_ListStockKey.Add(pair.Key);
        }

        // 버섯 → 희귀도 높은 순 → 신선한 순
        m_ListStockKey.Sort((a, b) =>
        {
            int compare = string.CompareOrdinal(a.ItemID, b.ItemID);
            if (compare != 0)
                return compare;
            if (a.Rarity != b.Rarity)
                return b.Rarity.CompareTo(a.Rarity);

            return a.Freshness.CompareTo(b.Freshness);
        });

        MerchantDef merchant = SelectedMerchant;
        for (int i = 0; i < m_ListStockKey.Count; ++i)
        {
            LootKey key = m_ListStockKey[i];
            m_DicPick.TryGetValue(key, out int picked);
            int price = merchant != null ? hunt.GetPrice(merchant, key) : 0;

            UIItemSlot slot = GetSlot(m_ListStockSlot, m_StockList, i, OnClickStock);
            SetSlot(slot, key, hunt.DicHarvest[key] - picked, price > 0 ? $"{price}G" : "안 삼");
        }

        HideFrom(m_ListStockSlot, m_ListStockKey.Count);
    }

    private void RefreshPick(HuntData hunt)
    {
        MerchantDef merchant = SelectedMerchant;
        for (int i = 0; i < m_ListPickKey.Count; ++i)
        {
            LootKey key = m_ListPickKey[i];
            int price = merchant != null ? hunt.GetPrice(merchant, key) : 0;

            UIItemSlot slot = GetSlot(m_ListPickSlot, m_PickList, i, OnClickPick);
            SetSlot(slot, key, m_DicPick[key], price > 0 ? $"{price}G" : "안 삼");
        }

        HideFrom(m_ListPickSlot, m_ListPickKey.Count);
    }

    // 합계와 [거래] 버튼. 판정은 HuntData.CheckTrade에 맡긴다
    private void RefreshTotal(HuntData hunt)
    {
        BuildRequest();
        EMarketResult result = hunt.CheckTrade(SelectedMerchant, m_ListItem, out int gold);

        m_TotalText.text = $"합계 {gold}G  ({PickCount()}/{Define.MarketTradeLimit})";
        m_TradeButton.interactable = result == EMarketResult.Success;

        if (result == EMarketResult.NotBuying)
            SetMessage("이 상인이 사지 않는 버섯이 섞여 있다");
    }

    // 거래·기다리기 뒤 사라진 버섯은 고른 목록에서도 뺀다
    private void RemoveMissingPick(HuntData hunt)
    {
        for (int i = m_ListPickKey.Count - 1; i >= 0; --i)
        {
            LootKey key = m_ListPickKey[i];
            hunt.DicHarvest.TryGetValue(key, out int have);
            int count = Mathf.Min(m_DicPick[key], have);
            if (count > 0)
            {
                m_DicPick[key] = count;
                continue;
            }

            m_DicPick.Remove(key);
            m_ListPickKey.RemoveAt(i);
        }
    }

    private void BuildRequest()
    {
        m_ListItem.Clear();
        foreach (LootKey key in m_ListPickKey)
        {
            m_ListItem.Add(new MarketItem(key, m_DicPick[key]));
        }
    }

    private int PickCount()
    {
        int total = 0;
        foreach (var pair in m_DicPick)
        {
            total += pair.Value;
        }

        return total;
    }

    // slot ---

    private UIItemSlot GetSlot(List<UIItemSlot> listSlot, RectTransform parent, int index, System.Action<UIItemSlot> onClick)
    {
        if (index >= listSlot.Count)
        {
            UIItemSlot slot = Instantiate(m_SlotTemplate, parent);
            slot.SetOnClick(onClick);
            listSlot.Add(slot);
        }

        listSlot[index].gameObject.SetActive(true);
        return listSlot[index];
    }

    private static void HideFrom(List<UIItemSlot> listSlot, int index)
    {
        for (int i = index; i < listSlot.Count; ++i)
        {
            listSlot[i].gameObject.SetActive(false);
        }
    }

    // 칸: 아이콘 + 개수, 아래에 값, 뱃지 = 신선도(색은 희귀도)
    private static void SetSlot(UIItemSlot slot, LootKey key, int count, string priceText)
    {
        MushroomDef def = null;
        GameManager.Instance?.Database?.TryGet(key.ItemID, out def);

        Sprite icon = def != null ? def.Icon : null;
        Color tint = def != null ? def.Tint : UIStyle.PanelLight;
        slot.Set(key.ItemID, icon, priceText, count, tint);
        slot.SetBadge(key.Freshness == EFreshness.High ? "신선" : "덜", GetRarityColor(key.Rarity));
    }

    private static Color GetRarityColor(ERarity rarity)
    {
        return rarity switch
        {
            ERarity.Epic => RarityEpic,
            ERarity.Rare => RarityRare,
            _ => RarityCommon,
        };
    }

    private void SetMessage(string text)
    {
        m_MessageText.text = text;
    }

    // click ---

    private void OnClickMerchant(UIMerchantRow row)
    {
        m_SelectedIndex = row.Index;
        SetMessage(string.Empty);
        Refresh();
    }

    private void OnClickStock(UIItemSlot slot)
    {
        int index = m_ListStockSlot.IndexOf(slot);
        if (index < 0 || index >= m_ListStockKey.Count)
            return;

        HuntData hunt = Hunt;
        LootKey key = m_ListStockKey[index];
        m_DicPick.TryGetValue(key, out int picked);

        SetMessage(string.Empty);
        if (PickCount() >= Define.MarketTradeLimit)
        {
            SetMessage($"한 번에 {Define.MarketTradeLimit}개까지 팔 수 있다");
            return;
        }

        if (hunt == null || hunt.DicHarvest.TryGetValue(key, out int have) == false || have <= picked)
            return;

        if (picked == 0)
            m_ListPickKey.Add(key);

        m_DicPick[key] = picked + 1;
        Refresh();
    }

    private void OnClickPick(UIItemSlot slot)
    {
        int index = m_ListPickSlot.IndexOf(slot);
        if (index < 0 || index >= m_ListPickKey.Count)
            return;

        LootKey key = m_ListPickKey[index];
        int count = m_DicPick[key] - 1;
        if (count <= 0)
        {
            m_DicPick.Remove(key);
            m_ListPickKey.RemoveAt(index);
        }
        else
        {
            m_DicPick[key] = count;
        }

        SetMessage(string.Empty);
        Refresh();
    }

    private void OnClickTrade()
    {
        HuntData hunt = Hunt;
        if (hunt == null)
            return;

        BuildRequest();
        hunt.CheckTrade(SelectedMerchant, m_ListItem, out int gold);
        EMarketResult result = hunt.Trade(SelectedMerchant, m_ListItem);
        if (result != EMarketResult.Success)
        {
            SetMessage("거래할 수 없다");
            return;
        }

        m_DicPick.Clear();
        m_ListPickKey.Clear();
        SetMessage($"+{gold}G");
        Refresh(); // 시장이 닫혔으면 MarketClosed로 이미 닫혀 있다
    }

    private void OnClickWait()
    {
        if (Hunt?.Wait() == EMarketResult.Success)
            SetMessage("상인들이 값을 다시 매겼다");
    }

    private void OnClickClose()
    {
        Hunt?.EndMarket();
    }

    // event ---

    private void OnMarketChanged(MarketChanged evt)
    {
        Refresh();
    }

    private void OnMarketClosed(MarketClosed evt)
    {
        Close();
    }
}
