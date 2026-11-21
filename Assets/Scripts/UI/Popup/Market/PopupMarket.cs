using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Button m_IntroButton = null;    // 연출 전체를 덮는 투명 버튼. 누르면 끝 모습으로 넘기거나 닫는다
    [SerializeField] private TMP_Text m_IntroTimer = null;   // 가운데 큰 mm:ss
    [SerializeField] private TMP_Text m_IntroMessage = null; // 그 아래 "지금 가면... n시간 n분은 거래할 수 있겠어!"

    [Header("상인")]
    [SerializeField] private PopupMarket_MerchantInfo m_MerchantInfo = null; // 가운데 고른 상인 카드

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

    private readonly PopupMarket_Pick m_Pick = new();       // 고른 버섯. 표시 순서는 m_PickPool 순서
    private readonly List<LootKey> m_ListStockKey = new();  // m_StockPool 순서. Refresh 때 다시 만든다
    private readonly UISequence m_IntroSequence = new();    // 시작 연출. 움직이는 중에 누르면 끝 모습으로, 끝 모습에서 누르면 닫는다

    private PopupMarketCounter m_CounterPopup; // 떠 있는 개수 조절 팝업
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
        m_IntroButton.E_SetOnClick(OnClickIntro);
    }

    // virtual ---

    protected override void OnOpen()
    {
        SEventManager.Subscribe<MarketChanged>(OnMarketChanged);
        SEventManager.Subscribe<MarketClosed>(OnMarketClosed);

        // 다시 여는 팝업이라 지난번 상태를 되돌린다(시계는 남은 시간 표시로)
        m_Clock.SetMarket(false);
        m_SelectedIndex = 0;
        m_Pick.Clear();
        m_WaitLabel.E_SetString(EStringKey.Market_WaitFormat, Define.MarketActionMinutes);

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
            if (m_SelectedIndex < 0 || m_SelectedIndex >= hunt.ListMerchant.Count)
                return null;

            return hunt.ListMerchant[m_SelectedIndex];
        }
    }

    // 시장 중에는 시간이 멈춰 있어 시계도 여기서만 바뀐다(열 때, MarketChanged)
    private void Refresh()
    {
        HuntData hunt = Hunt;
        m_Clock.SetTime(hunt.RemainTime, hunt.NightTime);

        m_Pick.Sync(hunt.DicHarvest, CompareLoot);
        RefreshMerchants(hunt);
        RefreshStock(hunt);
        RefreshPick();
        RefreshTotal(hunt);

        m_SoldText.E_SetString(EStringKey.Market_SoldFormat, hunt.SoldGold);
        m_WaitButton.E_SetInteractable(hunt.CanAct);
    }

    private void RefreshMerchants(HuntData hunt)
    {
        m_RowPool.Refresh(hunt.ListMerchant, SetMerchantRow);

        MerchantDef merchant = SelectedMerchant;
        if (merchant == null)
        {
            m_MerchantInfo.SetEmpty();
            return;
        }

        int trust = PlayerDataManager.Instance.Market.GetTrust(merchant.ID);
        m_MerchantInfo.Set(merchant, trust, hunt.GetTrustGain(merchant.ID), hunt.GetSwingRate(merchant));
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

    private void RefreshPick()
    {
        m_PickPool.Refresh(m_Pick.ListKey, SetPickSlot);
    }

    // 합계와 [거래] 버튼, 지금 고른 상태의 안내 글귀. 판정은 HuntData.CheckTrade에 맡긴다
    private void RefreshTotal(HuntData hunt)
    {
        EMarketResult result = hunt.CheckTrade(SelectedMerchant, m_Pick.BuildRequest(), out int gold);

        m_TotalText.E_SetString(EStringKey.Market_TotalFormat, gold, m_Pick.Count, Define.MarketTradeLimit);
        m_TradeButton.E_SetInteractable(result == EMarketResult.Success);
        SetMessage(GetResultText(result));
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
        int picked = m_Pick.GetCount(key);
        int have = hunt.DicHarvest[key];
        SetLootSlot(slot, key, have - picked, GetPriceText(hunt, key));

        bool isAll = picked >= have;
        slot.SetDim(isAll);
        slot.SetCaption(picked <= 0 ? null : isAll ? StringDef.S_Format(EStringKey.Market_SellAllFormat, have) : $"{have}→{have - picked}");
    }

    private void SetPickSlot(UICommonSlot slot, LootKey key)
    {
        SetLootSlot(slot, key, m_Pick.GetCount(key), GetPriceText(Hunt, key));
    }

    // 안내 글귀. Refresh가 지금 상태의 글귀를 쓰고, 거래·기다리기 결과는 그 뒤에 덮어쓴다(다음 Refresh까지 남는다)
    private void SetMessage(string text)
    {
        m_MessageText.E_SetText(text);
    }
    
    // 고른 상인의 한 개 값. 안 사면 "안 삼"
    private string GetPriceText(HuntData hunt, LootKey key)
    {
        MerchantDef merchant = SelectedMerchant;
        int price = 0;
        if (merchant != null)
            price = hunt.GetPrice(merchant, key);
        return price > 0 ? StringDef.S_Format(EStringKey.Market_PriceFormat, price) : StringDef.S_Get(EStringKey.Market_NotBuying);
    }

    // intro ---

    // !!) 시장 중에는 시간이 멈춰 있어(TimeManager) 연출은 unscaled 시간으로 돈다(UISequence)
    private void PlayIntro()
    {
        float remain = Hunt.RemainTime;
        int seconds = Mathf.CeilToInt(remain);
        int minutes = UINightClock.ToGameMinutes(remain);

        m_IsIntroContinue = false;
        m_Body.E_SetVisible(false);
        m_Intro.E_SetActive(true);
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
                m_IntroMessage.E_SetText($"{FormatIntro(minutes)}\n<size={UIStyle.FontSmall}>{UIStyle.ColorTag(UIStyle.TextDim, StringDef.S_Get(EStringKey.Market_IntroContinue))}</size>");
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
                m_Intro.E_SetActive(false);
                m_Body.E_SetVisible(true);
            })
            .Play(this);
    }

    // "지금 가면... 4시간 30분은 거래할 수 있겠어!" (시간은 크게, 금색)
    private static string FormatIntro(int minutes)
    {
        int hour = minutes / 60;
        int minute = minutes % 60;
        string time = hour > 0 && minute == 0 ? StringDef.S_Format(EStringKey.Market_HourFormat, hour)
            : hour > 0 ? StringDef.S_Format(EStringKey.Market_HourMinuteFormat, hour, minute)
            : StringDef.S_Format(EStringKey.Market_MinuteFormat, minute);

        return StringDef.S_Format(EStringKey.Market_IntroFormat, $"<size={IntroBigSize}>{UIStyle.ColorTag(UIStyle.Gold, time)}</size>");
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

        m_CounterPopup = GameManager.Instance.PopupS.Open<PopupMarketCounter>(EPopupType.PopupMarketCounter);
        m_CounterPopup.Setup(key, have, picked, price, count => OnCountConfirm(key, count), OnCountClosed);
    }

    private void CloseCountPopup()
    {
        if (m_CounterPopup != null)
            m_CounterPopup.Close();

        m_CounterPopup = null;
    }
    
    // 0개면 팔 목록에서 뺀다
    private void OnCountConfirm(LootKey key, int count)
    {
        m_Pick.Set(key, count);
        Refresh();
    }

    // 확인·취소·뒤로 가기 어느 쪽으로 닫혀도 팝업이 알려준다. 닫힌 팝업(다시 쓰는 인스턴스)을 붙잡고 있지 않게 비운다
    private void OnCountClosed()
    {
        m_CounterPopup = null;
    }

    #region on click

    private void OnClickMerchant(PopupMarket_MerchantRow row)
    {
        int index = m_RowPool.IndexOf(row);
        if (index < 0)
            return;

        m_SelectedIndex = index;
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
        int picked = m_Pick.GetCount(key);

        if (IsIntro || hunt.DicHarvest.TryGetValue(key, out int have) == false || have <= 0)
            return;

        if (picked > 0)
        {
            OpenCountPopup(hunt, key, have, picked);
            return;
        }

        if (m_Pick.Count >= Define.MarketTradeLimit)
        {
            SetMessage(GetResultText(EMarketResult.OverLimit));
            return;
        }

        m_Pick.Set(key, have);
        Refresh();
    }

    private void OnClickPick(UICommonSlot slot)
    {
        int index = m_PickPool.IndexOf(slot);
        if (index < 0 || index >= m_Pick.ListKey.Count)
            return;

        // 누르면 그 묶음을 통째로 뺀다(남은 버섯 칸의 딤·위 글자도 풀린다)
        m_Pick.Remove(m_Pick.ListKey[index]);
        Refresh();
    }

    private void OnClickTrade()
    {
        // 고른 목록은 요청으로 옮기고 먼저 비운다. 성공하면 Trade가 보내는 MarketChanged로 한 번만 갱신된다(밤이 다 가면 MarketClosed로 닫힌다)
        // !!) request는 m_Pick이 다시 쓰는 목록이라, 성공해서 MarketChanged로 Refresh가 돌면 바뀐다. 실패하면 이벤트가 없어 그대로다
        IReadOnlyList<MarketItem> request = m_Pick.BuildRequest();
        m_Pick.Clear();

        EMarketResult result = Hunt.Trade(SelectedMerchant, request, out int gold);
        if (result == EMarketResult.Success)
        {
            SetMessage(StringDef.S_Format(EStringKey.Market_TradeGoldFormat, gold));
            return;
        }

        // 실패하면 아무것도 바뀌지 않았으니 고른 목록을 되돌린다
        m_Pick.Restore(request);

        Refresh();
        SetMessage(GetResultText(result));
    }

    private void OnClickWait()
    {
        EMarketResult result = Hunt.Wait();
        SetMessage(result == EMarketResult.Success ? StringDef.S_Get(EStringKey.Market_WaitDone) : GetResultText(result));
    }

    private void OnClickClose()
    {
        Hunt.EndMarket();
    }
    
    private void OnClickIntro()
    {
        if (IsIntro == false)
            return;

        if (m_IntroSequence.IsWaiting)
            m_IsIntroContinue = true;
        else
            m_IntroSequence.Skip();
    }

    #endregion

    #region static
    
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
    
    // 칸: 아이콘 + 개수, 아래에 값, 바탕 = 희귀도, 곰팡이 얼룩 = 덜 신선
    private static void SetLootSlot(UICommonSlot slot, LootKey key, int count, string priceText)
    {
        GameDatabase.S_TryGet(key.ItemID, out MushroomDef def); // 없으면 def는 null
        slot.SetLoot(key, def, count, priceText);
    }

    // 요청 결과 → 안내 글귀. 성공과 아직 안 고름(Empty)은 빈 글
    private static string GetResultText(EMarketResult result)
    {
        return result switch
        {
            EMarketResult.Closed => StringDef.S_Get(EStringKey.Market_Result_Closed),
            EMarketResult.NoMerchant => StringDef.S_Get(EStringKey.Market_Result_NoMerchant),
            EMarketResult.OverLimit => StringDef.S_Format(EStringKey.Market_Result_OverLimitFormat, Define.MarketTradeLimit),
            EMarketResult.NotEnough => StringDef.S_Get(EStringKey.Market_Result_NotEnough),
            EMarketResult.NotBuying => StringDef.S_Get(EStringKey.Market_Result_NotBuying),
            _ => string.Empty,
        };
    }
    
    #endregion
}
