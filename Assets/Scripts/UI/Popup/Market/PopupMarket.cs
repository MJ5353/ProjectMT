using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 토벌 종료 뒤 현장 시장 팝업(BattleFlow Market 상태가 Flow를 넘겨 띄운다). 왼쪽 오늘 온 상인, 가운데 고른 상인, 오른쪽 위 고른 버섯·합계·[거래], 오른쪽 아래 남은 버섯.
// 계산과 결과는 HuntData가 정하고 여기서는 요청(Trade·Wait)만 보낸다. [시장 떠나기]는 BattleFlow.ExitMarket(확인 팝업 뒤 EndMarket). 시장이 닫히면(MarketClosed) 스스로 닫힌다.
// 열릴 때 시작 연출(PopupMarket_Intro)이 이어진다. 연출 중에 누르면 끝 모습으로 건너뛰고, 끝 모습에서 한 번 더 누르면 연출을 닫고 본 화면이 나타난다.
// 결과 화면 전환과 저장은 MarketClosed를 받은 쪽(BattleFlow Market 상태)이 한다

public class PopupMarket : PopupFlowBase<BattleFlow>
{
    [SerializeField] private UINightClock m_Clock = null;
    [SerializeField] private CanvasGroup m_Body = null; // 시계를 뺀 나머지. 시작 연출 뒤 서서히 나타난다(시계는 HUD 자리에 그대로 있는 것처럼)

    [Header("시작 연출")]
    [SerializeField] private PopupMarket_Intro m_Intro = null; // 시작 연출. 누르면 끝 모습으로 넘기거나 닫는다

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
    [SerializeField] private GameObject m_SoldOut = null;                            // 팔 버섯이 없으면 남은 버섯 칸을 덮는 딤 + 글자
    [SerializeField] private UIListView<UICommonSlot> m_PickPool = new();           // 고른 버섯(가로)

    // ------------------------------------------------
    
    private const float FadeTime = 0.4f;      // 시작 연출이 사라지고 본 화면이 나타나는 시간
    private const float GoldRollTime = 0.35f; // 합계·판 골드 숫자가 굴러가는 시간

    private readonly PopupMarket_Pick m_Pick = new();       // 고른 버섯. 표시 순서는 m_PickPool 순서
    private readonly List<LootKey> m_ListStockKey = new();  // m_StockPool 순서. Refresh 때 다시 만든다
    private readonly UISequence m_GoldSequence = new();     // 합계·판 골드 숫자 굴리기
    
    private PopupMarketCounter m_CounterPopup;              // 떠 있는 개수 조절 팝업
    private int m_SelectedIndex;
    private int m_ShownTotal; // 지금 보이는 합계(굴러가는 중간값)
    private int m_ShownSold;  // 지금 보이는 판 골드(굴러가는 중간값)

    public override bool IsCloseOnBack => false; // [시장 떠나기]로만 닫는다
    private static HuntData Hunt => PlayerDataManager.Instance.Hunt;
    private static DefineDef Define => GameManager.Instance.Define; // 조절값(Define.asset)
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
        m_Intro.SetOnClick(OnClickIntro);
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
        m_WaitLabel.E_SetString(EStringKey.Market_WaitFormat, Define.Market.ActionMinutes);
        m_GoldSequence.Stop();
        m_ShownTotal = 0;
        m_ShownSold = Hunt.SoldGold;

        Refresh();

        // 시작 연출 준비. 본 화면과 시계 글자는 숨기고, 남은 시간을 시계 글자 자리에 둔다
        m_Body.E_SetVisible(false);
        m_Clock.SetTextVisible(false);
        m_Intro.Ready(Hunt.RemainTime, m_Clock.TextRect);
    }

    protected override void OnClose()
    {
        SEventManager.Unsubscribe<MarketChanged>(OnMarketChanged);
        SEventManager.Unsubscribe<MarketClosed>(OnMarketClosed);
        CloseCountPopup();
        m_GoldSequence.Stop();
    }

    protected override void OnBuildOpenSequence(UISequence sequence)
    {
        // 시작 연출은 여는 연출에 이어 붙인다(BeginClose가 멈추고, 누르면 SkipOpen으로 넘긴다)
        // !!) 시장 중에는 시간이 멈춰 있어(TimeManager) 연출은 unscaled 시간으로 돈다(UISequence)
        
        m_Intro.Append(sequence) // 1~2. 날아오기 → 숫자 굴리기 → 끝 모습에서 누를 때까지 기다리기
            .Call(ShowBodyClock) // 3. 본 화면. 시계 자리에는 시장 시간
            .Tween(FadeTime, TweenIntroFade)
            .Call(EndIntro);
    }

    // refresh ---

    private void Refresh()
    {
        // 시장 중에는 시간이 멈춰 있어 시계도 여기서만 바뀐다(열 때, MarketChanged)
        
        HuntData hunt = Hunt;
        m_Clock.SetTime(hunt.RemainTime, hunt.NightTime);

        m_Pick.Sync(hunt.DicHarvest, CompareLoot);
        RefreshMerchants(hunt);
        RefreshStock(hunt);
        RefreshPick();
        RefreshTotal(hunt);

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
    
    private void RefreshStock(HuntData hunt)
    {
        // 남은 버섯 = 가진 수 - 고른 수. 칸 아래에 고른 상인의 한 개 값을 쓴다.
        // 담긴 묶음도 자리에 남는다: 딤 + 가운데에 전부면 "전체", 일부면 "17→10"(가진 수 → 판 뒤 남는 수)

        m_ListStockKey.Clear();
        foreach (var pair in hunt.DicHarvest)
        {
            if (pair.Value > 0)
                m_ListStockKey.Add(pair.Key);
        }

        m_ListStockKey.Sort(CompareLoot);
        m_StockPool.Refresh(m_ListStockKey, SetStockSlot);

        // 다 팔았거나 빈손으로 들어왔으면 덮는다
        m_SoldOut.E_SetActive(hunt.HasHarvest == false);
    }

    private void RefreshPick()
    {
        m_PickPool.Refresh(m_Pick.ListKey, SetPickSlot);
    }

    private void RefreshTotal(HuntData hunt)
    {
        // 합계와 [거래] 버튼, 지금 고른 상태의 안내 글귀. 판정은 HuntData.CheckTrade에 맡긴다
        EMarketResult result = hunt.CheckTrade(SelectedMerchant, m_Pick.BuildRequest(), out int gold);

        m_TradeButton.E_SetInteractable(result == EMarketResult.Success);
        SetMessage(GetResultText(result));
        RollGold(gold, hunt.SoldGold);
    }

    // 합계·판 골드를 지금 보이는 값에서 새 값까지 드르륵 굴린다. 굴러가는 중에 또 바뀌면 그 자리에서 다시 굴린다
    private void RollGold(int total, int sold)
    {
        int fromTotal = m_ShownTotal;
        int fromSold = m_ShownSold;

        m_GoldSequence.Clear()
            .Tween(GoldRollTime, t =>
            {
                float k = UITween.EaseOutCubic(t);
                SetGoldText(Mathf.RoundToInt(Mathf.Lerp(fromTotal, total, k)), Mathf.RoundToInt(Mathf.Lerp(fromSold, sold, k)));
            })
            .Play(this);
    }

    private void SetGoldText(int total, int sold)
    {
        m_ShownTotal = total;
        m_ShownSold = sold;

        m_TotalText.E_SetString(EStringKey.Market_TotalFormat, total, m_Pick.Count, Define.Market.TradeLimit);
        m_SoldText.E_SetString(EStringKey.Market_SoldFormat, sold);
    }

    // slot ---

    private void SetMerchantRow(PopupMarket_MerchantRow row, MerchantDef merchant)
    {
        int trust = PlayerDataManager.Instance.Market.GetTrust(merchant.ID);
        row.Set(merchant, trust, Define.Market.TrustMax, merchant == SelectedMerchant);
    }

    private void SetStockSlot(UICommonSlot slot, LootKey key)
    {
        HuntData hunt = Hunt;
        int picked = m_Pick.GetCount(key);
        int have = hunt.DicHarvest[key];
        
        SetLootSlot(slot, key, have - picked, GetPriceText(hunt, key));
        SetPreferBadge(slot, key);

        if (picked <= 0)
            return;

        // 고른 칸은 딤 + 가운데 글자. 전부면 빨간 "전체", 일부면 "7→3"(가진 수 → 판 뒤 남는 수, "7→"는 회색). 개수 뱃지는 숨긴다
        string center = picked >= have ? 
            UIStyle.ColorTag(UIStyle.Negative, StringDef.S_Get(EStringKey.Market_SellAll)) : 
            UIStyle.ColorTag(UIStyle.TextDim, $"{have}→") + (have - picked);
        
        slot.SetDim(true);
        slot.SetCount(0);
        slot.SetCenter(center);
    }

    private void SetPickSlot(UICommonSlot slot, LootKey key)
    {
        SetLootSlot(slot, key, m_Pick.GetCount(key), GetPriceText(Hunt, key));
        SetPreferBadge(slot, key);
    }

    // 고른 상인이 평소보다 비싸게 사 주는 버섯이면 하트
    private void SetPreferBadge(UICommonSlot slot, LootKey key)
    {
        MerchantDef merchant = SelectedMerchant;
        slot.SetBadge(merchant != null && merchant.HasPreferBonus(key.Rarity));
    }

    private void SetMessage(string text)
    {
        // 안내 글귀. Refresh가 지금 상태의 글귀를 쓰고, 거래·기다리기 결과는 그 뒤에 덮어쓴다(다음 Refresh까지 남는다)
        m_MessageText.E_SetText(text);
    }
    
    private string GetPriceText(HuntData hunt, LootKey key)
    {
        // 고른 상인의 한 개 값. 안 사면 "안 삼"
        MerchantDef merchant = SelectedMerchant;
        
        int price = merchant != null ? hunt.GetPrice(merchant, key) : 0;
        if (price > 0)
            return StringDef.S_Format(EStringKey.Market_PriceFormat, price);
        
        return StringDef.S_Get(EStringKey.Market_NotBuying);
    }

    // intro ---

    private void ShowBodyClock()
    {
        m_Clock.SetMarket(true);
        m_Clock.SetTextVisible(true);
    }

    private void TweenIntroFade(float t)
    {
        m_Intro.SetAlpha(1f - t);
        m_Body.alpha = t;
    }

    private void EndIntro()
    {
        m_Intro.Hide();
        m_Body.E_SetVisible(true);
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

        m_CounterPopup = FlowManager.Instance.PopupS.Open<PopupMarketCounter>(EPopupType.PopupMarketCounter);
        m_CounterPopup.Setup(key, have, picked, price, count => OnCountConfirm(key, count), OnCountClosed);
    }

    private void CloseCountPopup()
    {
        if (m_CounterPopup != null)
            m_CounterPopup.Close();

        m_CounterPopup = null;
    }
    
    private void OnCountConfirm(LootKey key, int count)
    {
        // 0개면 팔 목록에서 뺀다
        m_Pick.Set(key, count);
        Refresh();
    }

    private void OnCountClosed()
    {
        // 확인·취소·뒤로 가기 어느 쪽으로 닫혀도 팝업이 알려준다. 닫힌 팝업(다시 쓰는 인스턴스)을 붙잡고 있지 않게 비운다
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

        if (IsOpening || hunt.DicHarvest.TryGetValue(key, out int have) == false || have <= 0)
            return;

        if (picked > 0)
        {
            OpenCountPopup(hunt, key, have, picked);
            return;
        }

        if (m_Pick.Count >= Define.Market.TradeLimit)
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

    // 하루가 끝난다는 확인은 BattleFlow가 묻고, 예면 시장을 닫는다(MarketClosed로 이 팝업도 닫힌다)
    private void OnClickClose()
    {
        Flow.ExitMarket();
    }
    
    private void OnClickIntro()
    {
        if (IsOpening == false)
            return;

        if (IsOpenWaiting)
            m_Intro.Continue();
        else
            SkipOpen();
    }

    #endregion

    #region static
    
    // 등급 높은 순 → 종류(도감 번호, Def가 없으면 ID) → 신선한 순
    private static int CompareLoot(LootKey a, LootKey b)
    {
        if (a.Rarity != b.Rarity)
            return b.Rarity.CompareTo(a.Rarity);

        int compare = GetDexNo(a.ItemID).CompareTo(GetDexNo(b.ItemID));
        
        if (compare == 0)
            compare = string.CompareOrdinal(a.ItemID, b.ItemID);

        if (compare != 0)
            return compare;

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
            EMarketResult.OverLimit => StringDef.S_Format(EStringKey.Market_Result_OverLimitFormat, Define.Market.TradeLimit),
            EMarketResult.NotEnough => StringDef.S_Get(EStringKey.Market_Result_NotEnough),
            EMarketResult.NotBuying => StringDef.S_Get(EStringKey.Market_Result_NotBuying),
            _ => string.Empty,
        };
    }
    
    #endregion
}
