using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 현장 시장 팝업·개수 조절 팝업·상인 줄 프리팹을 만든다. 메뉴는 UISetup의 "모든 UI 다시 만들기" 하나다.
// 배치(1920x1080 기준): 왼쪽 위 밤 시계(전투 HUD와 같은 자리), 그 아래 상인 목록(5칸)·[기다리기]·[시장 닫기].
// 가운데 고른 상인 카드와 오른쪽 칸은 화면 위 여백부터 쓴다. 오른쪽 위 팔 버섯·판 골드·[거래], 남은 공간은 오른쪽 아래 남은 버섯(스크롤).
// 맨 위에 시작 연출(가운데 큰 남은 시간 + 시장 시간). 글자는 외곽선 없이 기본 글꼴만 쓴다
public static class MarketUISetup
{
    private const float Margin = 40f;         // 화면 가장자리
    private const float Pad = 24f;            // 패널 안쪽 여백
    private const float Gap = 16f;            // 칸 사이
    private const float Column = 24f;         // 세 칸 사이
    private const float ContentTop = -300f;   // 왼쪽 칸은 시계(+시장 시간 두 줄) 아래부터 쓴다
    private const float LeftWidth = 380f;
    private const float CenterWidth = 440f;
    private const float PickHeight = 280f;
    private const float TradeWidth = 240f;
    private const float RowHeight = 74f;      // 상인 5칸이 [기다리기] 위에 들어가는 높이
    private const float SlotSize = 120f;
    private const float SlotNameHeight = 40f; // UICommonSlot 이름(값) 줄
    private const float SlotCaptionHeight = 34f; // UICommonSlot 위 글자("일괄판매(x17)") 줄

    // ------------------------------------------------

    // 상인 줄 → 개수 조절 팝업 → 시장 팝업 순서로 만든다. isOverwrite면 있어도 다시 만든다(GUID 유지).
    // 상인 줄 원본은 따로 프리팹(Resources)으로 둔다. PopupMarket이 EUIComponentType.PopupMarket_MerchantRow로 UIListView에서 꺼낸다
    public static void BuildPrefabs(bool isOverwrite)
    {
        UIBuilder.SaveAsPrefab(MerchantRow().gameObject, EditorPath.MarketMerchantRowPrefab, isOverwrite);
        UIBuilder.SaveAsPrefab(BuildCountPopup().gameObject, EditorPath.PopupMarketCountPrefab, isOverwrite);
        UIBuilder.SaveAsPrefab(BuildMarketPopup().gameObject, EditorPath.PopupMarketPrefab, isOverwrite);
    }

    // ==================================================

    private static PopupMarket BuildMarketPopup()
    {
        RectTransform root = new GameObject("PopupMarket", typeof(RectTransform)).GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        UIBuilder.AddImage(root, new Color(UIStyle.Background.r, UIStyle.Background.g, UIStyle.Background.b, 0.96f), true); // 뒤 화면 터치를 막는다

        UINightClock clock = BattleUISetup.BuildNightClock(root, false);

        RectTransform body = UIBuilder.Stretch("Body", root);
        CanvasGroup bodyGroup = body.gameObject.AddComponent<CanvasGroup>();

        // 왼쪽: 상인 목록 + 아래 버튼 ---
        Caption(body, "MerchantCaption", "오늘 온 상인", new Vector2(Margin, ContentTop));
        RectTransform left = UIBuilder.Rect("Merchants", body, Vector2.zero, new Vector2(0, 1), new Vector2(Margin, 280), new Vector2(Margin + LeftWidth, ContentTop - 50));
        VerticalLayoutGroup vertical = left.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.spacing = Gap;
        vertical.childAlignment = TextAnchor.UpperLeft;
        vertical.childControlWidth = vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;

        Button close = UIBuilder.Button(body, "CloseButton", "시장 닫기", new Vector2(LeftWidth, 100), UIStyle.AccentDark);
        PlaceCorner(close, Vector2.zero, new Vector2(Margin, Margin));
        Button wait = UIBuilder.Button(body, "WaitButton", "30분 기다리기", new Vector2(LeftWidth, 100), UIStyle.PanelLight);
        PlaceCorner(wait, Vector2.zero, new Vector2(Margin, Margin + 100 + Gap));

        // 가운데: 고른 상인 카드(그림·이름·설명·특징) ---
        float centerLeft = Margin + LeftWidth + Column;
        RectTransform center = UIBuilder.Rect("Merchant", body, Vector2.zero, new Vector2(0, 1), new Vector2(centerLeft, Margin), new Vector2(centerLeft + CenterWidth, -Margin));
        UIBuilder.AddImage(center, UIStyle.Panel);
        RectTransform iconRect = UIBuilder.Anchored("Icon", center, new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(180, 180));
        Image icon = UIBuilder.AddImage(iconRect, Color.white);
        icon.preserveAspect = true;
        TMP_Text merchantName = TopLabel(center, "Name", "상인", UIStyle.FontLarge, -224, 56, TextAlignmentOptions.Center);
        merchantName.color = UIStyle.Gold;
        TMP_Text desc = TopLabel(center, "Desc", "설명", UIStyle.FontSmall, -288, 72, TextAlignmentOptions.Top);
        desc.textWrappingMode = TextWrappingModes.Normal;
        desc.color = UIStyle.TextDim;
        RectTransform line = UIBuilder.Rect("Line", center, new Vector2(0, 1), Vector2.one, new Vector2(Pad, -376), new Vector2(-Pad, -374));
        UIBuilder.AddImage(line, UIStyle.PanelLight);
        RectTransform infoRect = UIBuilder.Rect("Info", center, Vector2.zero, Vector2.one, new Vector2(Pad, Pad), new Vector2(-Pad, -396));
        TMP_Text info = UIBuilder.Label(infoRect, "신뢰도  0\n오늘 시세  보통\n희귀 이상  x2.0\n그 밖  x0.5", UIStyle.FontSmall, TextAlignmentOptions.TopLeft);
        info.richText = true;
        info.lineSpacing = 40f;

        // 오른쪽 위: 팔 버섯 + 합계 + [거래] ---
        float rightLeft = centerLeft + CenterWidth + Column;
        RectTransform right = UIBuilder.Rect("Trade", body, Vector2.zero, Vector2.one, new Vector2(rightLeft, Margin), new Vector2(-Margin, -Margin));
        RectTransform pickPanel = UIBuilder.Rect("Pick", right, new Vector2(0, 1), Vector2.one, new Vector2(0, -PickHeight), Vector2.zero);
        UIBuilder.AddImage(pickPanel, UIStyle.Panel);
        Caption(pickPanel, "Caption", "팔 버섯  ·  눌러서 묶음 빼기", new Vector2(Pad, -12));
        RectTransform soldRect = UIBuilder.Rect("Sold", pickPanel, new Vector2(0, 1), Vector2.one, new Vector2(Pad, -52), new Vector2(-Pad, -12));
        TMP_Text sold = UIBuilder.Label(soldRect, "판 골드 0G", UIStyle.FontSmall, TextAlignmentOptions.Right);
        sold.color = UIStyle.Gold;
        RectTransform pickList = UIBuilder.Rect("List", pickPanel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(Pad, -60 - SlotSize - SlotNameHeight), new Vector2(-TradeWidth - Pad, -60));
        HorizontalLayoutGroup horizontal = pickList.gameObject.AddComponent<HorizontalLayoutGroup>();
        horizontal.spacing = Gap;
        horizontal.childAlignment = TextAnchor.UpperLeft;
        horizontal.childControlWidth = horizontal.childControlHeight = false;
        horizontal.childForceExpandWidth = horizontal.childForceExpandHeight = false;

        RectTransform totalRect = UIBuilder.Rect("Total", pickPanel, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-TradeWidth - Pad, -110), new Vector2(-Pad, -60));
        TMP_Text total = UIBuilder.Label(totalRect, "합계 0G  (0/5묶음)", UIStyle.FontNormal, TextAlignmentOptions.Right);
        total.color = UIStyle.Gold;
        Button trade = UIBuilder.Button(pickPanel, "TradeButton", "거래", new Vector2(TradeWidth, 96), UIStyle.Accent);
        PlaceCorner(trade, new Vector2(1, 1), new Vector2(-Pad, -124));

        RectTransform messageRect = UIBuilder.Rect("Message", pickPanel, Vector2.zero, new Vector2(1, 0), new Vector2(Pad, 10), new Vector2(-Pad, 44));
        TMP_Text message = UIBuilder.Label(messageRect, string.Empty, UIStyle.FontSmall, TextAlignmentOptions.Left);
        message.color = UIStyle.Accent;

        // 오른쪽 아래: 남은 버섯(스크롤 격자) ---
        RectTransform stockPanel = UIBuilder.Rect("Stock", right, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -PickHeight - Column));
        UIBuilder.AddImage(stockPanel, UIStyle.Panel);
        Caption(stockPanel, "Caption", "남은 버섯  ·  눌러서 묶음 담기  ·  칸 아래는 이 상인의 한 개 값", new Vector2(Pad, -12));
        RectTransform stockList = ScrollGrid(stockPanel);

        // 시작 연출(맨 위). 가운데 큰 남은 시간 + 아래 시장 시간 ---
        RectTransform intro = UIBuilder.Stretch("Intro", root);
        CanvasGroup introGroup = intro.gameObject.AddComponent<CanvasGroup>();
        introGroup.blocksRaycasts = false;
        RectTransform timerRect = UIBuilder.Anchored("Timer", intro, new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(600, 140));
        timerRect.pivot = new Vector2(0.5f, 0.5f);
        TMP_Text introTimer = UIBuilder.Label(timerRect, "05:00", UIStyle.FontHuge);
        RectTransform introMessageRect = UIBuilder.Anchored("Message", intro, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(1400, 110));
        introMessageRect.pivot = new Vector2(0.5f, 0.5f);
        TMP_Text introMessage = UIBuilder.Label(introMessageRect, "지금 가면... 4시간 30분은 거래할 수 있겠어!", UIStyle.FontNormal);
        introMessage.richText = true;
        intro.gameObject.SetActive(false);

        PopupMarket popup = root.gameObject.AddComponent<PopupMarket>();
        UIBuilder.SetRef(popup, "m_Clock", clock);
        UIBuilder.SetRef(popup, "m_Body", bodyGroup);
        UIBuilder.SetRef(popup, "m_Intro", introGroup);
        UIBuilder.SetRef(popup, "m_IntroTimer", introTimer);
        UIBuilder.SetRef(popup, "m_IntroMessage", introMessage);
        UIBuilder.SetRef(popup, "m_MerchantList", left);
        UIBuilder.SetRef(popup, "m_MerchantIcon", icon);
        UIBuilder.SetRef(popup, "m_MerchantName", merchantName);
        UIBuilder.SetRef(popup, "m_MerchantDesc", desc);
        UIBuilder.SetRef(popup, "m_MerchantInfo", info);
        UIBuilder.SetRef(popup, "m_PickList", pickList);
        UIBuilder.SetRef(popup, "m_StockList", stockList);
        UIBuilder.SetRef(popup, "m_TotalText", total);
        UIBuilder.SetRef(popup, "m_MessageText", message);
        UIBuilder.SetRef(popup, "m_SoldText", sold);
        UIBuilder.SetRef(popup, "m_TradeButton", trade);
        UIBuilder.SetRef(popup, "m_WaitButton", wait);
        UIBuilder.SetRef(popup, "m_WaitLabel", wait.GetComponentInChildren<TMP_Text>());
        UIBuilder.SetRef(popup, "m_CloseButton", close);

        return popup;
    }

    // 상인 한 줄 원본(독립 프리팹): 고른 테두리(뒤), 바탕 버튼, 이름·신뢰도 숫자, 아래 신뢰도 게이지
    private static PopupMarket_MerchantRow MerchantRow()
    {
        RectTransform row = UIBuilder.Anchored("PopupMarket_MerchantRow", null, new Vector2(0, 1), Vector2.zero, new Vector2(LeftWidth, RowHeight));
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = RowHeight;

        Image selected = UIBuilder.Panel("Selected", row, UIStyle.Gold);
        selected.rectTransform.offsetMin = new Vector2(-4, -4);
        selected.rectTransform.offsetMax = new Vector2(4, 4);

        Image back = UIBuilder.Panel("Back", row, UIStyle.Panel, true);
        Button button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = back;

        RectTransform nameRect = UIBuilder.Rect("Name", row, new Vector2(0, 1), Vector2.one, new Vector2(Pad, -48), new Vector2(-Pad, -8));
        TMP_Text nameText = UIBuilder.Label(nameRect, "상인", UIStyle.FontNormal, TextAlignmentOptions.Left);

        TMP_Text trustText = UIBuilder.Label(nameRect, "신뢰 0", UIStyle.FontSmall, TextAlignmentOptions.Right, "Trust");
        trustText.color = UIStyle.TextDim;

        UIGauge trust = UIBuilder.Gauge(row, "TrustGauge", new Vector2(LeftWidth - Pad * 2f, 18), UIStyle.Gold, false);
        RectTransform trustRect = trust.RectTransform;
        if (trustRect != null)
        {
            trustRect.anchorMin = new Vector2(0, 0);
            trustRect.anchorMax = new Vector2(1, 0);
            trustRect.pivot = new Vector2(0.5f, 0f);
            trustRect.offsetMin = new Vector2(Pad, 10);
            trustRect.offsetMax = new Vector2(-Pad, 24);
        }

        PopupMarket_MerchantRow merchantRow = row.gameObject.AddComponent<PopupMarket_MerchantRow>();
        UIBuilder.SetRef(merchantRow, "m_Button", button);
        UIBuilder.SetRef(merchantRow, "m_NameText", nameText);
        UIBuilder.SetRef(merchantRow, "m_TrustText", trustText);
        UIBuilder.SetRef(merchantRow, "m_Trust", trust);
        UIBuilder.SetRef(merchantRow, "m_Selected", selected.gameObject);

        return merchantRow;
    }

    // 개수 조절 팝업: 어두운 바탕 + 가운데 창. 칸·이름 / 판매 개수·변화 / 값 / [-10][-1][+1][+10][전부] / [취소][확인]
    private static PopupMarket_Count BuildCountPopup()
    {
        RectTransform root = new GameObject("PopupMarket_Count", typeof(RectTransform)).GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        UIBuilder.AddImage(root, UIStyle.Dim, true); // 뒤 화면 터치를 막는다

        RectTransform window = UIBuilder.Anchored("Window", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 560));
        window.pivot = new Vector2(0.5f, 0.5f);
        UIBuilder.AddImage(window, UIStyle.Panel, true);

        UICommonSlot slot = UIBuilder.CommonSlot(window, "Slot", SlotSize);
        RectTransform slotRect = slot.RectTransform;
        if (slotRect != null)
        {
            slotRect.anchorMin = slotRect.anchorMax = slotRect.pivot = new Vector2(0, 1);
            slotRect.anchoredPosition = new Vector2(Pad * 2f, -Pad * 2f);
        }

        float textLeft = Pad * 3f + SlotSize;
        TMP_Text nameText = WindowLabel(window, "Name", "버섯", UIStyle.FontLarge, textLeft, -Pad * 2f, 56);
        nameText.color = UIStyle.Gold;
        TMP_Text count = WindowLabel(window, "Count", "판매 0 / 0", UIStyle.FontNormal, textLeft, -Pad * 2f - 64, 44);
        TMP_Text change = WindowLabel(window, "Change", "0→0", UIStyle.FontNormal, textLeft, -Pad * 2f - 112, 44);
        change.color = UIStyle.TextDim;
        TMP_Text price = WindowLabel(window, "Price", "한 개 0G  ·  합계 0G", UIStyle.FontSmall, Pad * 2f, -230, 40);
        price.richText = true;

        // 개수 버튼 줄
        Vector2 stepSize = new Vector2(120, 90);
        Button minus10 = StepButton(window, "Minus10", "-10", stepSize, -260);
        Button minus1 = StepButton(window, "Minus1", "-1", stepSize, -130);
        Button plus1 = StepButton(window, "Plus1", "+1", stepSize, 0);
        Button plus10 = StepButton(window, "Plus10", "+10", stepSize, 130);
        Button all = StepButton(window, "All", "전부", stepSize, 260);

        Button cancel = UIBuilder.Button(window, "CancelButton", "취소", new Vector2(260, 100), UIStyle.PanelLight);
        PlaceCorner(cancel, new Vector2(0.5f, 0f), new Vector2(-150, Pad * 2f));
        Button confirm = UIBuilder.Button(window, "ConfirmButton", "확인", new Vector2(260, 100), UIStyle.Accent);
        PlaceCorner(confirm, new Vector2(0.5f, 0f), new Vector2(150, Pad * 2f));

        PopupMarket_Count popup = root.gameObject.AddComponent<PopupMarket_Count>();
        UIBuilder.SetRef(popup, "m_Group", root.gameObject.AddComponent<CanvasGroup>()); // 여는 연출(Popup)
        UIBuilder.SetRef(popup, "m_Window", window);
        UIBuilder.SetRef(popup, "m_Slot", slot);
        UIBuilder.SetRef(popup, "m_NameText", nameText);
        UIBuilder.SetRef(popup, "m_CountText", count);
        UIBuilder.SetRef(popup, "m_ChangeText", change);
        UIBuilder.SetRef(popup, "m_PriceText", price);
        UIBuilder.SetRef(popup, "m_Minus10Button", minus10);
        UIBuilder.SetRef(popup, "m_Minus1Button", minus1);
        UIBuilder.SetRef(popup, "m_Plus1Button", plus1);
        UIBuilder.SetRef(popup, "m_Plus10Button", plus10);
        UIBuilder.SetRef(popup, "m_AllButton", all);
        UIBuilder.SetRef(popup, "m_CancelButton", cancel);
        UIBuilder.SetRef(popup, "m_ConfirmButton", confirm);

        return popup;
    }

    // 창 위에서 top만큼 내려온 왼쪽 정렬 한 줄(오른쪽 끝까지)
    private static TMP_Text WindowLabel(Transform window, string name, string text, int size, float left, float top, float height)
    {
        RectTransform rt = UIBuilder.Rect(name, window, new Vector2(0, 1), Vector2.one, new Vector2(left, top - height), new Vector2(-Pad * 2f, top));
        return UIBuilder.Label(rt, text, size, TextAlignmentOptions.Left);
    }

    // 창 가운데 기준 x에 놓는 개수 버튼(창 아래에서 위로 190)
    private static Button StepButton(Transform window, string name, string text, Vector2 size, float x)
    {
        Button button = UIBuilder.Button(window, name, text, size, UIStyle.PanelLight);
        PlaceCorner(button, new Vector2(0.5f, 0f), new Vector2(x, 190));
        return button;
    }

    // 세로 스크롤 + 격자. 칸이 늘면 아래로 길어진다. 격자(Content)를 돌려준다
    private static RectTransform ScrollGrid(RectTransform panel)
    {
        RectTransform viewport = UIBuilder.Rect("Viewport", panel, Vector2.zero, Vector2.one, new Vector2(Pad, Pad), new Vector2(-Pad, -60));
        viewport.gameObject.AddComponent<RectMask2D>();
        UIBuilder.AddImage(viewport, Color.clear, true); // 빈 곳을 끌어도 스크롤되게

        RectTransform content = UIBuilder.Rect("Content", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
        content.pivot = new Vector2(0.5f, 1f);
        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        // 칸 위에 위 글자, 아래에 값 줄이 붙으니 그만큼 세로 간격과 위 여백을 둔다
        grid.cellSize = new Vector2(SlotSize, SlotSize);
        grid.spacing = new Vector2(Gap * 2f, Gap + SlotNameHeight + SlotCaptionHeight);
        grid.padding = new RectOffset(0, 0, (int)SlotCaptionHeight + 4, (int)SlotNameHeight + 4);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.childAlignment = TextAnchor.UpperLeft;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = panel.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        return content;
    }

    // ---

    // 패널 머리글(작고 흐린 글자)
    private static TMP_Text Caption(Transform parent, string name, string text, Vector2 topLeft)
    {
        RectTransform rt = UIBuilder.Anchored(name, parent, new Vector2(0, 1), topLeft, new Vector2(900, 40));
        TMP_Text label = UIBuilder.Label(rt, text, UIStyle.FontSmall, TextAlignmentOptions.Left);
        label.color = UIStyle.TextDim;

        return label;
    }

    private static TMP_Text TopLabel(Transform parent, string name, string text, int size, float top, float height, TextAlignmentOptions align)
    {
        RectTransform rt = UIBuilder.Rect(name, parent, new Vector2(0, 1), Vector2.one, new Vector2(Pad, top - height), new Vector2(-Pad, top));
        return UIBuilder.Label(rt, text, size, align);
    }

    private static void PlaceCorner(Button button, Vector2 anchor, Vector2 position)
    {
        RectTransform rt = button.transform as RectTransform;
        if (rt == null)
            return;

        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = position;
    }
}
