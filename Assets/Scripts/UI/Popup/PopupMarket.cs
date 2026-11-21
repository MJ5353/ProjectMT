using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// ==================================================

// 토벌 종료 뒤 현장 시장 팝업. 왼쪽 오늘 온 상인, 가운데 고른 상인, 오른쪽 위 고른 버섯·합계·[거래], 오른쪽 아래 남은 버섯.
// 계산과 결과는 HuntData가 정하고 여기서는 요청(Trade·Wait·EndMarket)만 보낸다. 시장이 닫히면(MarketClosed) 스스로 닫힌다.
// 열릴 때: 시계의 남은 시간이 화면 가운데로 날아와 주르륵 줄고, 그 아래 "지금 가면... n시간 n분은 거래할 수 있겠어!"가 후루룩 오른다.
// 연출 중에 누르면 끝 모습으로 건너뛰고, 끝 모습에서 한 번 더 누르면 연출을 닫고 본 화면이 나타난다.
// 결과 화면 전환과 저장은 MarketClosed를 받은 쪽(BattleService)이 한다

public class PopupMarket : PopupBase
{
    private const float IntroMoveTime = 0.9f;   // 남은 시간이 가운데로 날아오는 시간
    private const float IntroCountTime = 2.4f;  // 남은 시간이 줄고 시장 시간이 오르는 시간
    private const float FadeTime = 0.4f;
    private const int IntroBigSize = UIStyle.FontTitle;

    [SerializeField] private UINightClock m_Clock = null;
    [SerializeField] private CanvasGroup m_Body = null; // 시계를 뺀 나머지. 시작 연출 뒤 서서히 나타난다(시계는 HUD 자리에 그대로 있는 것처럼)

    [Header("시작 연출")]
    [SerializeField] private CanvasGroup m_Intro = null;
    [SerializeField] private TMP_Text m_IntroTimer = null;   // 가운데 큰 mm:ss
    [SerializeField] private TMP_Text m_IntroMessage = null; // 그 아래 "지금 가면... n시간 n분은 거래할 수 있겠어!"

    [Header("상인")]
    [SerializeField] private Image m_MerchantIcon = null;
    [SerializeField] private TMP_Text m_MerchantName = null;
    [SerializeField] private TMP_Text m_MerchantDesc = null;
    [SerializeField] private TMP_Text m_MerchantInfo = null;          // 신뢰도·오늘 시세·좋아하는 버섯

    [Header("거래")]
    [SerializeField] private TMP_Text m_TotalText = null;
    [SerializeField] private TMP_Text m_MessageText = null;
    [SerializeField] private TMP_Text m_SoldText = null;
    [SerializeField] private Button m_TradeButton = null;
    [SerializeField] private Button m_WaitButton = null;
    [SerializeField] private TMP_Text m_WaitLabel = null;
    [SerializeField] private Button m_CloseButton = null;

    [Header("목록")]
    [SerializeField] private UIListView<PopupMarket_MerchantRow> m_RowPool = new(); // 상인(세로)
    [SerializeField] private UIListView<UICommonSlot> m_StockPool = new();          // 남은 버섯(격자, 스크롤 내용 높이도 맞춘다)
    [SerializeField] private UIListView<UICommonSlot> m_PickPool = new();           // 고른 버섯(가로)

    private readonly List<LootKey> m_ListStockKey = new();  // m_StockPool 순서
    private readonly List<MarketItem> m_ListItem = new();   // HuntData 요청용
    private readonly List<LootKey> m_ListPickKey = new();   // 고른 순서. m_PickPool 순서
    private readonly Dictionary<LootKey, int> m_DicPick = new();
    private readonly UISequence m_IntroSequence = new();    // 시작 연출. 움직이는 중에 누르면 끝 모습으로, 끝 모습에서 누르면 닫는다

    private PopupMarketCounter _counterPopup; // 떠 있는 개수 조절 팝업
    private bool m_IsIntroContinue; // 끝 모습에서 눌렀다
    private int m_SelectedIndex;

    private bool IsIntro => m_IntroSequence.IsPlaying;
    public override bool IsCloseOnBack => false; // [시장 닫기]로만 닫는다
    private static HuntData Hunt => PlayerDataManager.Instance.Hunt;
    private static DefineDef Define => GameManager.Instance.Define; // 조절값(Define.asset)

    // ------------------------------------------------

    private void Awake()
    {
        // 줄·칸은 UIFactory로 꺼낸다(UIComponentDef). 꺼낼 때마다 클릭을 대입으로 다시 연결한다
        m_RowPool.Init(EUIComponentType.PopupMarket_MerchantRow, row => row.SetOnClick(OnClickMerchant));
        m_StockPool.Init(EUIComponentType.UICommonSlot, slot => slot.SetOnClick(OnClickStock));
        m_PickPool.Init(EUIComponentType.UICommonSlot, slot => slot.SetOnClick(OnClickPick));

        m_TradeButton.E_SetOnClick(OnClickTrade);
        m_WaitButton.E_SetOnClick(OnClickWait);
        m_CloseButton.E_SetOnClick(OnClickClose);
    }

    private void Update()
    {
        if (IsIntro && IsPressed())
        {
            if (m_IntroSequence.IsWaiting)
                m_IsIntroContinue = true;
            else
                m_IntroSequence.Skip();
        }

        HuntData hunt = Hunt;
        if (hunt != null)
            m_Clock.SetTime(hunt.RemainTime, hunt.NightTime);
    }

    // virtual ---

    protected override void OnOpen()
    {
        SEventManager.Subscribe<MarketChanged>(OnMarketChanged);
        SEventManager.Subscribe<MarketClosed>(OnMarketClosed);

        // 다시 여는 팝업이라 지난번 상태를 되돌린다(시계는 남은 시간 표시로)
        m_Clock.SetMarket(false);
        m_SelectedIndex = 0;
        m_DicPick.Clear();
        m_ListPickKey.Clear();
        m_WaitLabel.E_SetText($"{Define.MarketActionMinutes}분 기다리기");
        SetMessage(string.Empty);

        HuntData hunt = Hunt;
        if (hunt != null)
            m_Clock.SetTime(hunt.RemainTime, hunt.NightTime);

        Refresh();
        PlayIntro();
    }

    protected override void OnClose()
    {
        SEventManager.Unsubscribe<MarketChanged>(OnMarketChanged);
        SEventManager.Unsubscribe<MarketClosed>(OnMarketClosed);
        CloseCountPopup();
        m_IntroSequence.Stop();
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

        m_SoldText.E_SetText($"판 골드 {hunt.SoldGold:N0}G");
        m_WaitButton.E_SetInteractable(hunt.CanAct);
    }

    private void RefreshMerchants(HuntData hunt)
    {
        MarketData market = PlayerDataManager.Instance.Market;
        m_RowPool.Refresh(hunt.ListMerchant, SetMerchantRow);

        MerchantDef merchant = SelectedMerchant;
        if (merchant == null)
        {
            m_MerchantIcon.enabled = false;
            m_MerchantName.E_SetText("오늘은 상인이 없다");
            m_MerchantDesc.E_SetText(string.Empty);
            m_MerchantInfo.E_SetText(string.Empty);
            return;
        }

        m_MerchantIcon.enabled = merchant.Icon != null;
        m_MerchantIcon.sprite = merchant.Icon;
        m_MerchantName.E_SetText(merchant.DisplayName);
        m_MerchantDesc.E_SetText(merchant.Description);

        int swing = Mathf.RoundToInt((hunt.GetSwingRate(merchant) - 1f) * 100f);
        string swingText = swing > 0 ? UIStyle.ColorTag(UIStyle.Positive, $"+{swing}%") : swing < 0 ? UIStyle.ColorTag(UIStyle.Negative, $"{swing}%") : "보통";
        int gain = hunt.GetTrustGain(merchant.ID);
        string gainText = gain > 0 ? " " + UIStyle.ColorTag(UIStyle.Positive, $"(+{gain})") : string.Empty;
        string otherText = merchant.OtherRate > 0f ? $"x{merchant.OtherRate:0.0#}" : UIStyle.ColorTag(UIStyle.Negative, "안 삼");

        m_MerchantInfo.E_SetText(
            $"신뢰도  {market.GetTrust(merchant.ID)}{gainText}\n" +
            $"오늘 시세  {swingText}\n" +
            $"{GetRarityName(merchant.PreferRarity)} 이상  x{merchant.PreferRate:0.0#}\n" +
            $"그 밖  {otherText}");
    }

    private static string GetRarityName(ERarity rarity)
    {
        return rarity switch
        {
            ERarity.Epic => "에픽",
            ERarity.Rare => "희귀",
            _ => "일반",
        };
    }

    // 남은 버섯 = 가진 수 - 고른 수. 칸 아래에 고른 상인의 한 개 값을 쓴다.
    // 담긴 묶음도 자리에 남는다: 전부면 딤 + "일괄판매(x17)", 일부면 "17→10"(가진 수 → 판 뒤 남는 수)
    private void RefreshStock(HuntData hunt)
    {
        m_ListStockKey.Clear();
        foreach (var pair in hunt.DicHarvest)
        {
            if (pair.Value > 0)
                m_ListStockKey.Add(pair.Key);
        }

        m_ListStockKey.Sort(CompareLoot);
        m_StockPool.Refresh(m_ListStockKey, SetStockSlot);
    }

    private void RefreshPick(HuntData hunt)
    {
        m_ListPickKey.Sort(CompareLoot);
        m_PickPool.Refresh(m_ListPickKey, SetPickSlot);
    }

    // 합계와 [거래] 버튼. 판정은 HuntData.CheckTrade에 맡긴다
    private void RefreshTotal(HuntData hunt)
    {
        BuildRequest();
        EMarketResult result = hunt.CheckTrade(SelectedMerchant, m_ListItem, out int gold);

        m_TotalText.E_SetText($"합계 {gold}G  ({m_ListPickKey.Count}/{Define.MarketTradeLimit}묶음)");
        m_TradeButton.E_SetInteractable(result == EMarketResult.Success);

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

    // 같은 버섯(도감 번호, Def가 없으면 ID) → 등급 높은 순 → 신선한 순
    private static int CompareLoot(LootKey a, LootKey b)
    {
        int compare = GetDexNo(a.ItemID).CompareTo(GetDexNo(b.ItemID));
        if (compare == 0)
            compare = string.CompareOrdinal(a.ItemID, b.ItemID);
        if (compare != 0)
            return compare;
        if (a.Rarity != b.Rarity)
            return b.Rarity.CompareTo(a.Rarity);

        return a.Freshness.CompareTo(b.Freshness);
    }

    // Def가 없으면 맨 뒤로 보내고 ID로 비교한다
    private static int GetDexNo(string itemID)
    {
        GameDatabase.S_TryGet(itemID, out MushroomDef def); // 없으면 def는 null
        return def.E_Alive()?.DexNo ?? int.MaxValue;
    }

    // slot ---

    private void SetMerchantRow(PopupMarket_MerchantRow row, MerchantDef merchant)
    {
        int trust = PlayerDataManager.Instance.Market.GetTrust(merchant.ID);
        row.Set(merchant, trust, Define.TrustMax, merchant == SelectedMerchant);
    }

    private void SetStockSlot(UICommonSlot slot, LootKey key)
    {
        HuntData hunt = Hunt;
        m_DicPick.TryGetValue(key, out int picked);
        int have = hunt.DicHarvest[key];
        SetLootSlot(slot, key, have - picked, GetPriceText(hunt, key));

        bool isAll = picked >= have;
        slot.SetDim(isAll);
        slot.SetCaption(picked <= 0 ? null : isAll ? $"일괄판매(x{have})" : $"{have}→{have - picked}");
    }

    private void SetPickSlot(UICommonSlot slot, LootKey key)
    {
        SetLootSlot(slot, key, m_DicPick[key], GetPriceText(Hunt, key));
    }

    // 고른 상인의 한 개 값. 안 사면 "안 삼"
    private string GetPriceText(HuntData hunt, LootKey key)
    {
        MerchantDef merchant = SelectedMerchant;
        int price = 0;
        if (merchant != null)
            price = hunt.GetPrice(merchant, key);
        return price > 0 ? $"{price}G" : "안 삼";
    }

    // 칸: 아이콘 + 개수, 아래에 값, 바탕 = 희귀도, 곰팡이 얼룩 = 덜 신선
    private static void SetLootSlot(UICommonSlot slot, LootKey key, int count, string priceText)
    {
        GameDatabase.S_TryGet(key.ItemID, out MushroomDef def); // 없으면 def는 null
        slot.SetLoot(key, def, count, priceText);
    }

    private void SetMessage(string text)
    {
        m_MessageText.E_SetText(text);
    }

    // click ---

    private void OnClickMerchant(PopupMarket_MerchantRow row)
    {
        int index = m_RowPool.IndexOf(row);
        if (index < 0)
            return;

        m_SelectedIndex = index;
        SetMessage(string.Empty);
        Refresh();
    }

    private void OnClickStock(UICommonSlot slot)
    {
        int index = m_StockPool.IndexOf(slot);
        if (index < 0 || index >= m_ListStockKey.Count)
            return;

        // 처음 누르면 그 묶음을 통째로 담고(일괄판매), 담긴 칸을 또 누르면 개수 조절 팝업을 띄운다
        HuntData hunt = Hunt;
        LootKey key = m_ListStockKey[index];
        m_DicPick.TryGetValue(key, out int picked);

        SetMessage(string.Empty);
        if (IsIntro || hunt == null || hunt.DicHarvest.TryGetValue(key, out int have) == false || have <= 0)
            return;

        if (picked > 0)
        {
            OpenCountPopup(hunt, key, have, picked);
            return;
        }

        if (m_ListPickKey.Count >= Define.MarketTradeLimit)
        {
            SetMessage($"한 번에 {Define.MarketTradeLimit}묶음까지 팔 수 있다");
            return;
        }

        m_ListPickKey.Add(key);
        m_DicPick[key] = have;
        Refresh();
    }

    private void OnClickPick(UICommonSlot slot)
    {
        int index = m_PickPool.IndexOf(slot);
        if (index < 0 || index >= m_ListPickKey.Count)
            return;

        // 누르면 그 묶음을 통째로 뺀다(남은 버섯 칸의 딤·위 글자도 풀린다)
        RemovePick(m_ListPickKey[index]);

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

    // intro ---

    // !!) 시장 중에는 시간이 멈춰 있어(TimeManager) 연출은 unscaled 시간으로 돈다(UISequence)
    private void PlayIntro()
    {
        HuntData hunt = Hunt;
        float remain = hunt?.RemainTime ?? 0f;
        int seconds = Mathf.CeilToInt(remain);
        int minutes = UINightClock.ToGameMinutes(remain);

        m_IsIntroContinue = false;
        m_Body.E_SetVisible(false);
        m_Intro.gameObject.SetActive(true);
        m_Intro.alpha = 1f;
        m_Clock.SetTextVisible(false);

        RectTransform timer = m_IntroTimer.rectTransform;
        Vector3 to = timer.position;
        Vector3 from = m_Clock.TextRect.position;
        float fromScale = m_Clock.TextRect.rect.height / Mathf.Max(1f, timer.rect.height);
        m_IntroTimer.E_SetText(UINightClock.FormatRealTime(seconds));
        m_IntroMessage.E_SetText(FormatIntro(0));
        m_IntroMessage.alpha = 0f;

        m_IntroSequence.Clear()
            // 1. 시계 아래 남은 시간이 가운데로 날아오며 커진다
            .Tween(IntroMoveTime, t =>
            {
                float k = UITween.EaseOutCubic(t);
                timer.position = Vector3.Lerp(from, to, k);
                timer.localScale = Vector3.one * Mathf.Lerp(fromScale, 1f, k);
                m_IntroMessage.alpha = k;
            })
            // 2. 남은 시간은 주르륵 줄고, 시장 시간은 후루룩 오른다
            .Tween(IntroCountTime, t =>
            {
                float k = UITween.EaseOutCubic(t);
                m_IntroTimer.E_SetText(UINightClock.FormatRealTime(Mathf.RoundToInt(seconds * (1f - k))));
                m_IntroMessage.E_SetText(FormatIntro(Mathf.RoundToInt(minutes * k / 10f) * 10)); // 10분 단위로 굴러간다
            })
            // 끝 모습에서 멈춰 있다가, 한 번 더 누르면 닫는다
            .Call(() =>
            {
                m_IntroTimer.E_SetText(UINightClock.FormatRealTime(0));
                m_IntroMessage.E_SetText($"{FormatIntro(minutes)}\n<size={UIStyle.FontSmall}>{UIStyle.ColorTag(UIStyle.TextDim, "눌러서 계속")}</size>");
            })
            .WaitUntil(() => m_IsIntroContinue)
            // 3. 본 화면. 시계 자리에는 시장 시간
            .Call(() =>
            {
                m_Clock.SetMarket(true);
                m_Clock.SetTextVisible(true);
            })
            .Tween(FadeTime, t =>
            {
                m_Intro.alpha = 1f - t;
                m_Body.alpha = t;
            })
            .Call(() =>
            {
                m_Intro.gameObject.SetActive(false);
                m_Body.E_SetVisible(true);
            })
            .Play(this);
    }

    // 화면(마우스·터치) 누름. 연출 넘기기용
    private static bool IsPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    // "지금 가면... 4시간 30분은 거래할 수 있겠어!" (시간은 크게, 금색)
    private static string FormatIntro(int minutes)
    {
        int hour = minutes / 60;
        int minute = minutes % 60;
        string time = hour > 0 && minute == 0 ? $"{hour}시간" : hour > 0 ? $"{hour}시간 {minute}분" : $"{minute}분";

        return $"지금 가면... <size={IntroBigSize}>{UIStyle.ColorTag(UIStyle.Gold, time)}</size>은 거래할 수 있겠어!";
    }

    // event ---

    private void OnMarketChanged(MarketChanged evt)
    {
        Refresh();
    }

    private void OnMarketClosed(MarketClosed evt)
    {
        CloseCountPopup();
        Close();
    }

    // count popup ---

    private void OpenCountPopup(HuntData hunt, LootKey key, int have, int picked)
    {
        CloseCountPopup();
        MerchantDef merchant = SelectedMerchant;
        int price = 0;
        if (merchant != null)
            price = hunt.GetPrice(merchant, key);

        _counterPopup = GameManager.Instance.PopupS?.Open<PopupMarketCounter>(EPopupType.PopupMarket_Count);
        _counterPopup.E_Alive()?.Setup(key, have, picked, price, count => OnCountConfirm(key, count));
    }

    // 0개면 팔 목록에서 뺀다
    private void OnCountConfirm(LootKey key, int count)
    {
        _counterPopup = null;
        if (count <= 0)
            RemovePick(key);
        else
            m_DicPick[key] = count;

        SetMessage(string.Empty);
        Refresh();
    }

    private void CloseCountPopup()
    {
        if (_counterPopup != null)
            _counterPopup.Close();

        _counterPopup = null;
    }

    private void RemovePick(LootKey key)
    {
        m_DicPick.Remove(key);
        m_ListPickKey.Remove(key);
    }
}
