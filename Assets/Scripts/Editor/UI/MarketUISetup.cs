using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 현장 시장 팝업 프리팹 생성과 전투 HUD 연결. 이미 있으면 덮어쓰지 않는다(대장이 손본 프리팹 보호).
// HUD 프리팹이 이미 있으면 m_MarketPopupPrefab 칸만 채운다
// 배치: 좌상단 밤 시계(전투 HUD와 같은 자리), 왼쪽 상인 목록, 가운데 고른 상인, 오른쪽 위 팔 버섯·[거래], 오른쪽 아래 남은 버섯, 아래 [기다리기]·[시장 닫기]
public static class MarketUISetup
{
    private const float RowHeight = 130f;
    private const float SlotSize = 120f;
    private const float SlotNameHeight = 40f; // UIItemSlot 이름(값) 줄
    private const float Gap = 16f;

    // ------------------------------------------------

    [MenuItem("MushTaker/시장/시장 팝업 만들기 + HUD 연결")]
    public static void CreateMarketPopup()
    {
        CreateIfMissing();
        LinkToBattleHud();
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
        UIBuilder.AddImage(root, new Color(UIStyle.Background.r, UIStyle.Background.g, UIStyle.Background.b, 0.94f), true); // 뒤 화면 터치를 막는다

        UINightClock clock = BattleUISetup.BuildNightClock(root);

        // 왼쪽 상인 목록 ---
        RectTransform left = UIBuilder.Rect("Merchants", root, Vector2.zero, new Vector2(0, 1), new Vector2(40, 180), new Vector2(440, -360));
        Title(root, "MerchantTitle", "오늘 온 상인", new Vector2(40, -300));
        VerticalLayoutGroup vertical = left.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.spacing = Gap;
        vertical.childAlignment = TextAnchor.UpperLeft;
        vertical.childControlWidth = vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;
        UIMerchantRow rowTemplate = MerchantRow(root);

        // 가운데 고른 상인 ---
        RectTransform center = UIBuilder.Rect("Merchant", root, Vector2.zero, new Vector2(0, 1), new Vector2(480, 180), new Vector2(1000, -40));
        UIBuilder.AddImage(center, UIStyle.Panel);
        RectTransform iconRect = UIBuilder.Anchored("Icon", center, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(300, 300));
        Image icon = UIBuilder.AddImage(iconRect, Color.white);
        icon.preserveAspect = true;
        TMP_Text merchantName = TopLabel(center, "Name", "상인", UIStyle.FontTitle, -360, 80);
        merchantName.color = UIStyle.Gold;
        TMP_Text desc = TopLabel(center, "Desc", "설명", UIStyle.FontNormal, -450, 110);
        desc.textWrappingMode = TextWrappingModes.Normal;
        desc.color = UIStyle.TextDim;
        TMP_Text info = TopLabel(center, "Info", "신뢰도 0  ·  오늘 시세 보통", UIStyle.FontSmall, -580, 40);
        info.richText = true;

        // 오른쪽 위 팔 버섯 ---
        RectTransform right = UIBuilder.Rect("Trade", root, Vector2.zero, Vector2.one, new Vector2(1040, 180), new Vector2(-40, -40));
        RectTransform pickPanel = UIBuilder.Rect("Pick", right, new Vector2(0, 1), Vector2.one, new Vector2(0, -380), Vector2.zero);
        UIBuilder.AddImage(pickPanel, UIStyle.Panel);
        Title(pickPanel, "Title", "팔 버섯 (눌러서 빼기)", new Vector2(20, -10));
        RectTransform pickList = UIBuilder.Rect("List", pickPanel, new Vector2(0, 1), Vector2.one, new Vector2(20, -70 - SlotSize - SlotNameHeight), new Vector2(-20, -70));
        HorizontalLayoutGroup horizontal = pickList.gameObject.AddComponent<HorizontalLayoutGroup>();
        horizontal.spacing = Gap;
        horizontal.childAlignment = TextAnchor.UpperLeft;
        horizontal.childControlWidth = horizontal.childControlHeight = false;
        horizontal.childForceExpandWidth = horizontal.childForceExpandHeight = false;

        RectTransform messageRect = UIBuilder.Rect("Message", pickPanel, Vector2.zero, new Vector2(1, 0), new Vector2(20, 20), new Vector2(-340, 70));
        TMP_Text message = UIBuilder.Label(messageRect, string.Empty, UIStyle.FontSmall, TextAlignmentOptions.Left);
        message.color = UIStyle.Accent;
        RectTransform totalRect = UIBuilder.Rect("Total", pickPanel, Vector2.zero, new Vector2(1, 0), new Vector2(20, 70), new Vector2(-340, 120));
        TMP_Text total = UIBuilder.Label(totalRect, "합계 0G  (0/5)", UIStyle.FontNormal, TextAlignmentOptions.Left);
        total.color = UIStyle.Gold;

        Button trade = UIBuilder.Button(pickPanel, "TradeButton", "거래", new Vector2(280, 100), UIStyle.Accent);
        PlaceCorner(trade, new Vector2(1, 0), new Vector2(-20, 20));

        // 오른쪽 아래 남은 버섯(스크롤) ---
        RectTransform stockPanel = UIBuilder.Rect("Stock", right, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -400));
        UIBuilder.AddImage(stockPanel, UIStyle.Panel);
        Title(stockPanel, "Title", "남은 버섯 (눌러서 담기 · 아래는 이 상인의 한 개 값)", new Vector2(20, -10));
        RectTransform stockList = ScrollGrid(stockPanel);

        UIItemSlot slotTemplate = UIBuilder.ItemSlot(root, "SlotTemplate", SlotSize);
        slotTemplate.gameObject.SetActive(false);

        // 아래 줄 ---
        RectTransform soldRect = UIBuilder.Anchored("Sold", root, Vector2.zero, new Vector2(40, 40), new Vector2(400, 110));
        TMP_Text sold = UIBuilder.Label(soldRect, "판 골드 0", UIStyle.FontLarge, TextAlignmentOptions.Left);
        sold.color = UIStyle.Gold;

        Button wait = UIBuilder.Button(root, "WaitButton", "30분 기다리기", new Vector2(340, 110), UIStyle.PanelLight);
        PlaceCorner(wait, new Vector2(1, 0), new Vector2(-380, 40));
        Button close = UIBuilder.Button(root, "CloseButton", "시장 닫기", new Vector2(300, 110), UIStyle.AccentDark);
        PlaceCorner(close, new Vector2(1, 0), new Vector2(-40, 40));

        MarketPopup popup = root.gameObject.AddComponent<MarketPopup>();
        UIBuilder.SetRef(popup, "m_Clock", clock);
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

    // 상인 한 줄 원본: 고른 테두리(뒤), 바탕 버튼, 이름, 신뢰도 게이지. 목록 밖에 꺼 둔다
    private static UIMerchantRow MerchantRow(Transform parent)
    {
        RectTransform row = UIBuilder.Anchored("MerchantRowTemplate", parent, new Vector2(0, 1), new Vector2(40, -340), new Vector2(400, RowHeight));
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = RowHeight;

        Image selected = UIBuilder.Panel("Selected", row, UIStyle.Gold);
        ((RectTransform)selected.transform).offsetMin = new Vector2(-6, -6);
        ((RectTransform)selected.transform).offsetMax = new Vector2(6, 6);

        Image back = UIBuilder.Panel("Back", row, UIStyle.PanelLight, true);
        Button button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = back;

        RectTransform nameRect = UIBuilder.Rect("Name", row, new Vector2(0, 1), Vector2.one, new Vector2(20, -70), new Vector2(-20, -10));
        TMP_Text nameText = UIBuilder.Label(nameRect, "상인", UIStyle.FontNormal, TextAlignmentOptions.Left);

        RectTransform trustLabel = UIBuilder.Rect("TrustLabel", row, Vector2.zero, new Vector2(1, 0), new Vector2(20, 14), new Vector2(-20, 50));
        TMP_Text trustText = UIBuilder.Label(trustLabel, "신뢰", UIStyle.FontSmall, TextAlignmentOptions.Left);
        trustText.color = UIStyle.TextDim;

        UIGauge trust = UIBuilder.Gauge(row, "Trust", new Vector2(280, 24), UIStyle.Gold, false);
        RectTransform trustRect = (RectTransform)trust.transform;
        trustRect.anchorMin = trustRect.anchorMax = trustRect.pivot = new Vector2(1, 0);
        trustRect.anchoredPosition = new Vector2(-20, 20);

        UIMerchantRow merchantRow = row.gameObject.AddComponent<UIMerchantRow>();
        UIBuilder.SetRef(merchantRow, "m_Button", button);
        UIBuilder.SetRef(merchantRow, "m_NameText", nameText);
        UIBuilder.SetRef(merchantRow, "m_Trust", trust);
        UIBuilder.SetRef(merchantRow, "m_Selected", selected.gameObject);
        row.gameObject.SetActive(false);

        return merchantRow;
    }

    // 세로 스크롤 + 격자. 칸이 늘면 아래로 길어진다. 격자(Content)를 돌려준다
    private static RectTransform ScrollGrid(RectTransform panel)
    {
        RectTransform viewport = UIBuilder.Rect("Viewport", panel, Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -70));
        viewport.gameObject.AddComponent<RectMask2D>();
        UIBuilder.AddImage(viewport, Color.clear, true); // 빈 곳을 끌어도 스크롤되게

        RectTransform content = UIBuilder.Rect("Content", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
        content.pivot = new Vector2(0.5f, 1f);
        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(SlotSize, SlotSize + SlotNameHeight);
        grid.spacing = new Vector2(Gap * 2f, Gap);
        grid.padding = new RectOffset(10, 10, 10, 10);
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

    private static TMP_Text Title(Transform parent, string name, string text, Vector2 topLeft)
    {
        RectTransform rt = UIBuilder.Anchored(name, parent, new Vector2(0, 1), topLeft, new Vector2(800, 50));
        TMP_Text label = UIBuilder.Label(rt, text, UIStyle.FontSmall, TextAlignmentOptions.Left);
        label.color = UIStyle.TextDim;

        return label;
    }

    private static TMP_Text TopLabel(Transform parent, string name, string text, int size, float top, float height)
    {
        RectTransform rt = UIBuilder.Rect(name, parent, new Vector2(0, 1), Vector2.one, new Vector2(30, top - height), new Vector2(-30, top));
        return UIBuilder.Label(rt, text, size);
    }

    private static void PlaceCorner(Button button, Vector2 anchor, Vector2 position)
    {
        RectTransform rt = (RectTransform)button.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = position;
    }
}
