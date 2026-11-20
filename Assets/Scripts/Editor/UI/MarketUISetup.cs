using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 현장 시장 팝업 프리팹 생성과 전투 HUD 연결. 이미 있으면 덮어쓰지 않는다(대장이 손본 프리팹 보호).
// HUD 프리팹이 이미 있으면 m_MarketPopupPrefab 칸만 채운다
// 배치(1920x1080 기준): 좌상단 밤 시계(전투 HUD와 같은 자리), 우상단 판 골드.
// 아래 세 칸: 왼쪽 상인 목록·[기다리기]·[시장 닫기] / 가운데 고른 상인 카드 / 오른쪽 위 팔 버섯·[거래], 오른쪽 아래 남은 버섯.
// 맨 위에 시작 연출(가운데 큰 남은 시간 + 시장 시간). 글자는 외곽선 없이 기본 글꼴만 쓴다
public static class MarketUISetup
{
    private const float Margin = 40f;         // 화면 가장자리
    private const float Pad = 24f;            // 패널 안쪽 여백
    private const float Gap = 16f;            // 칸 사이
    private const float Column = 24f;         // 세 칸 사이
    private const float ContentTop = -300f;   // 시계(+시장 시간 두 줄) 아래부터 쓴다
    private const float LeftWidth = 380f;
    private const float CenterWidth = 440f;
    private const float PickHeight = 280f;
    private const float TradeWidth = 240f;
    private const float RowHeight = 110f;
    private const float SlotSize = 120f;
    private const float SlotNameHeight = 40f; // UIItemSlot 이름(값) 줄
    private const int IntroTimerSize = 88;    // 갈무리는 11의 배수 크기가 또렷하다

    // ------------------------------------------------

    [MenuItem("MushTaker/시장/시장 팝업 만들기 + HUD 연결")]
    public static void CreateMarketPopup()
    {
        CreateIfMissing();
        LinkToBattleHud();
    }

    // 배치를 바꾼 뒤 다시 뽑을 때. 프리팹을 손봤다면 그 수정은 사라진다
    [MenuItem("MushTaker/시장/시장 팝업 다시 만들기 + HUD 연결")]
    public static void RebuildMarketPopup()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(EditorPath.MarketPopupPrefab) != null)
        {
            if (EditorUtility.DisplayDialog("시장 팝업 다시 만들기", "지금 시장 팝업 프리팹을 지우고 새로 만듭니다. 프리팹에서 손본 부분은 사라집니다.", "다시 만들기", "취소") == false)
                return;

            AssetDatabase.DeleteAsset(EditorPath.MarketPopupPrefab);
        }

        CreateMarketPopup();
    }

    // 없을 때만 만든다. 전투 HUD를 만들 때(BattleUISetup)도 부른다
    public static void CreateIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(EditorPath.MarketPopupPrefab) != null)
            return;

        if (UIBuilder.SaveAsPrefab(BuildMarketPopup().gameObject, EditorPath.MarketPopupPrefab))
            CLogger.Log("MarketUISetup", $"시장 팝업 생성: {EditorPath.MarketPopupPrefab}");
    }

    // 이미 있는 전투 HUD 프리팹에 시장 팝업을 물린다. 다른 칸은 건드리지 않는다
    private static void LinkToBattleHud()
    {
        MarketPopup popup = AssetDatabase.LoadAssetAtPath<MarketPopup>(EditorPath.MarketPopupPrefab);
        if (popup == null || AssetDatabase.LoadAssetAtPath<GameObject>(EditorPath.BattleHudPrefab) == null)
            return;

        GameObject contents = PrefabUtility.LoadPrefabContents(EditorPath.BattleHudPrefab);
        try
        {
            BattleHud hud = contents.GetComponentInChildren<BattleHud>(true);
            if (hud == null)
                return;

            UIBuilder.SetRef(hud, "m_MarketPopupPrefab", popup);
            PrefabUtility.SaveAsPrefabAsset(contents, EditorPath.BattleHudPrefab);
            CLogger.Log("MarketUISetup", "전투 HUD에 시장 팝업을 연결했습니다.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    // ==================================================

    private static MarketPopup BuildMarketPopup()
    {
        RectTransform root = new GameObject("MarketPopup", typeof(RectTransform)).GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        UIBuilder.AddImage(root, new Color(UIStyle.Background.r, UIStyle.Background.g, UIStyle.Background.b, 0.96f), true); // 뒤 화면 터치를 막는다

        UINightClock clock = BattleUISetup.BuildNightClock(root, false);

        RectTransform body = UIBuilder.Stretch("Body", root);
        CanvasGroup bodyGroup = body.gameObject.AddComponent<CanvasGroup>();

        // 오른쪽 위 판 골드 ---
        RectTransform soldBox = UIBuilder.Anchored("Sold", body, Vector2.one, new Vector2(-Margin, -Margin), new Vector2(340, 110));
        UIBuilder.AddImage(soldBox, UIStyle.Panel);
        Caption(soldBox, "Caption", "판 골드", new Vector2(Pad, -12));
        RectTransform soldValueRect = UIBuilder.Rect("Value", soldBox, Vector2.zero, Vector2.one, new Vector2(Pad, 8), new Vector2(-Pad, -40));
        TMP_Text sold = UIBuilder.Label(soldValueRect, "0G", UIStyle.FontLarge, TextAlignmentOptions.Right);
        sold.color = UIStyle.Gold;

        // 왼쪽: 상인 목록 + 아래 버튼 ---
        Caption(body, "MerchantCaption", "오늘 온 상인", new Vector2(Margin, ContentTop));
        RectTransform left = UIBuilder.Rect("Merchants", body, Vector2.zero, new Vector2(0, 1), new Vector2(Margin, 280), new Vector2(Margin + LeftWidth, ContentTop - 50));
        VerticalLayoutGroup vertical = left.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.spacing = Gap;
        vertical.childAlignment = TextAnchor.UpperLeft;
        vertical.childControlWidth = vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;
        UIMerchantRow rowTemplate = MerchantRow(body);

        Button close = UIBuilder.Button(body, "CloseButton", "시장 닫기", new Vector2(LeftWidth, 100), UIStyle.AccentDark);
        PlaceCorner(close, Vector2.zero, new Vector2(Margin, Margin));
        Button wait = UIBuilder.Button(body, "WaitButton", "30분 기다리기", new Vector2(LeftWidth, 100), UIStyle.PanelLight);
        PlaceCorner(wait, Vector2.zero, new Vector2(Margin, Margin + 100 + Gap));

        // 가운데: 고른 상인 카드(그림·이름·설명·특징) ---
        float centerLeft = Margin + LeftWidth + Column;
        RectTransform center = UIBuilder.Rect("Merchant", body, Vector2.zero, new Vector2(0, 1), new Vector2(centerLeft, Margin), new Vector2(centerLeft + CenterWidth, ContentTop));
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
        RectTransform right = UIBuilder.Rect("Trade", body, Vector2.zero, Vector2.one, new Vector2(rightLeft, Margin), new Vector2(-Margin, ContentTop));
        RectTransform pickPanel = UIBuilder.Rect("Pick", right, new Vector2(0, 1), Vector2.one, new Vector2(0, -PickHeight), Vector2.zero);
        UIBuilder.AddImage(pickPanel, UIStyle.Panel);
        Caption(pickPanel, "Caption", "팔 버섯  ·  눌러서 빼기", new Vector2(Pad, -12));
        RectTransform pickList = UIBuilder.Rect("List", pickPanel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(Pad, -60 - SlotSize - SlotNameHeight), new Vector2(-TradeWidth - Pad, -60));
        HorizontalLayoutGroup horizontal = pickList.gameObject.AddComponent<HorizontalLayoutGroup>();
        horizontal.spacing = Gap;
        horizontal.childAlignment = TextAnchor.UpperLeft;
        horizontal.childControlWidth = horizontal.childControlHeight = false;
        horizontal.childForceExpandWidth = horizontal.childForceExpandHeight = false;

        RectTransform totalRect = UIBuilder.Rect("Total", pickPanel, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-TradeWidth - Pad, -110), new Vector2(-Pad, -60));
        TMP_Text total = UIBuilder.Label(totalRect, "0G  (0/5)", UIStyle.FontNormal, TextAlignmentOptions.Right);
        total.color = UIStyle.Gold;
        Button trade = UIBuilder.Button(pickPanel, "TradeButton", "거래", new Vector2(TradeWidth, 96), UIStyle.Accent);
        PlaceCorner(trade, new Vector2(1, 1), new Vector2(-Pad, -124));

        RectTransform messageRect = UIBuilder.Rect("Message", pickPanel, Vector2.zero, new Vector2(1, 0), new Vector2(Pad, 10), new Vector2(-Pad, 44));
        TMP_Text message = UIBuilder.Label(messageRect, string.Empty, UIStyle.FontSmall, TextAlignmentOptions.Left);
        message.color = UIStyle.Accent;

        // 오른쪽 아래: 남은 버섯(스크롤 격자) ---
        RectTransform stockPanel = UIBuilder.Rect("Stock", right, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -PickHeight - Column));
        UIBuilder.AddImage(stockPanel, UIStyle.Panel);
        Caption(stockPanel, "Caption", "남은 버섯  ·  눌러서 담기  ·  칸 아래는 이 상인의 값", new Vector2(Pad, -12));
        RectTransform stockList = ScrollGrid(stockPanel);

        UIItemSlot slotTemplate = UIBuilder.ItemSlot(body, "SlotTemplate", SlotSize);
        slotTemplate.gameObject.SetActive(false);

        // 시작 연출(맨 위). 가운데 큰 남은 시간 + 아래 시장 시간 ---
        RectTransform intro = UIBuilder.Stretch("Intro", root);
        CanvasGroup introGroup = intro.gameObject.AddComponent<CanvasGroup>();
        introGroup.blocksRaycasts = false;
        RectTransform timerRect = UIBuilder.Anchored("Timer", intro, new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(600, 140));
        timerRect.pivot = new Vector2(0.5f, 0.5f);
        TMP_Text introTimer = UIBuilder.Label(timerRect, "05:00", IntroTimerSize);
        RectTransform introMessageRect = UIBuilder.Anchored("Message", intro, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(1400, 110));
        introMessageRect.pivot = new Vector2(0.5f, 0.5f);
        TMP_Text introMessage = UIBuilder.Label(introMessageRect, "지금 가면... 4시간 30분은 거래할 수 있겠어!", UIStyle.FontNormal);
        introMessage.richText = true;
        intro.gameObject.SetActive(false);

        MarketPopup popup = root.gameObject.AddComponent<MarketPopup>();
        UIBuilder.SetRef(popup, "m_Clock", clock);
        UIBuilder.SetRef(popup, "m_Body", bodyGroup);
        UIBuilder.SetRef(popup, "m_Intro", introGroup);
        UIBuilder.SetRef(popup, "m_IntroTimer", introTimer);
        UIBuilder.SetRef(popup, "m_IntroMessage", introMessage);
        UIBuilder.SetRef(popup, "m_MerchantList", left);
        UIBuilder.SetRef(popup, "m_MerchantTemplate", rowTemplate);
        UIBuilder.SetRef(popup, "m_MerchantIcon", icon);
        UIBuilder.SetRef(popup, "m_MerchantName", merchantName);
        UIBuilder.SetRef(popup, "m_MerchantDesc", desc);
        UIBuilder.SetRef(popup, "m_MerchantInfo", info);
        UIBuilder.SetRef(popup, "m_PickList", pickList);
        UIBuilder.SetRef(popup, "m_StockList", stockList);
        UIBuilder.SetRef(popup, "m_SlotTemplate", slotTemplate);
        UIBuilder.SetRef(popup, "m_TotalText", total);
        UIBuilder.SetRef(popup, "m_MessageText", message);
        UIBuilder.SetRef(popup, "m_SoldText", sold);
        UIBuilder.SetRef(popup, "m_TradeButton", trade);
        UIBuilder.SetRef(popup, "m_WaitButton", wait);
        UIBuilder.SetRef(popup, "m_WaitLabel", wait.GetComponentInChildren<TMP_Text>());
        UIBuilder.SetRef(popup, "m_CloseButton", close);

        return popup;
    }

    // 상인 한 줄 원본: 고른 테두리(뒤), 바탕 버튼, 이름·신뢰도 숫자, 아래 신뢰도 게이지. 목록 밖에 꺼 둔다
    private static UIMerchantRow MerchantRow(Transform parent)
    {
        RectTransform row = UIBuilder.Anchored("MerchantRowTemplate", parent, new Vector2(0, 1), new Vector2(Margin, ContentTop - 50), new Vector2(LeftWidth, RowHeight));
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = RowHeight;

        Image selected = UIBuilder.Panel("Selected", row, UIStyle.Gold);
        ((RectTransform)selected.transform).offsetMin = new Vector2(-4, -4);
        ((RectTransform)selected.transform).offsetMax = new Vector2(4, 4);

        Image back = UIBuilder.Panel("Back", row, UIStyle.Panel, true);
        Button button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = back;

        RectTransform nameRect = UIBuilder.Rect("Name", row, new Vector2(0, 1), Vector2.one, new Vector2(Pad, -64), new Vector2(-Pad, -14));
        TMP_Text nameText = UIBuilder.Label(nameRect, "상인", UIStyle.FontNormal, TextAlignmentOptions.Left);

        TMP_Text trustText = UIBuilder.Label(nameRect, "신뢰 0", UIStyle.FontSmall, TextAlignmentOptions.Right, "Trust");
        trustText.color = UIStyle.TextDim;

        UIGauge trust = UIBuilder.Gauge(row, "TrustGauge", new Vector2(LeftWidth - Pad * 2f, 18), UIStyle.Gold, false);
        RectTransform trustRect = (RectTransform)trust.transform;
        trustRect.anchorMin = new Vector2(0, 0);
        trustRect.anchorMax = new Vector2(1, 0);
        trustRect.pivot = new Vector2(0.5f, 0f);
        trustRect.offsetMin = new Vector2(Pad, 22);
        trustRect.offsetMax = new Vector2(-Pad, 40);

        UIMerchantRow merchantRow = row.gameObject.AddComponent<UIMerchantRow>();
        UIBuilder.SetRef(merchantRow, "m_Button", button);
        UIBuilder.SetRef(merchantRow, "m_NameText", nameText);
        UIBuilder.SetRef(merchantRow, "m_TrustText", trustText);
        UIBuilder.SetRef(merchantRow, "m_Trust", trust);
        UIBuilder.SetRef(merchantRow, "m_Selected", selected.gameObject);
        row.gameObject.SetActive(false);

        return merchantRow;
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
        grid.cellSize = new Vector2(SlotSize, SlotSize + SlotNameHeight);
        grid.spacing = new Vector2(Gap * 2f, Gap);
        grid.padding = new RectOffset(0, 0, 4, 4);
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
        RectTransform rt = (RectTransform)button.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = position;
    }
}
