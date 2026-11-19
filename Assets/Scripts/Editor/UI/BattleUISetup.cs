using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 전투 씬 UI(결과 화면 등)를 만들어 붙이는 메뉴. 프리팹이 이미 있으면 다시 만들지 않고 씬에만 넣는다
public static class BattleUISetup
{

    private const int ResultSortingOrder = 100;
    private const int HudSortingOrder = 10;
    private const string SpriteExtension = ".png";
    private const string CircleSprite = "UI_Circle"; // 반투명 원 바탕(스킬 아이콘·토벌 종료). 없으면 사각형으로 보인다

    // ==================================================

    // 전투 HUD. 사망 팝업 프리팹도 같이 만들어 HUD에 연결한다. 씬에 새로 넣었으면 true
    public static bool AddBattleHud()
    {
        if (Object.FindAnyObjectByType<BattleHud>(FindObjectsInactive.Include) != null)
        {
            CLogger.Log("BattleUISetup", "전투 HUD가 이미 씬에 있습니다.");
            return false;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(CommonPath.DeathPopupPrefab) == null)
            UIBuilder.SaveAsPrefab(BuildDeathPopup().gameObject, CommonPath.DeathPopupPrefab);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CommonPath.BattleHudPrefab);
        if (prefab == null)
        {
            UIBuilder.SaveAsPrefab(BuildBattleHud().gameObject, CommonPath.BattleHudPrefab);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CommonPath.BattleHudPrefab);
        }

        PrefabUtility.InstantiatePrefab(prefab);
        UIBuilder.CreateEventSystemIfNeeded();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        return true;
    }

    // 전투 씬 생성 도구(BattleSetup)에서도 부를 수 있게 열어 둔다. 씬에 새로 넣었으면 true
    public static bool AddResultScreen()
    {
        if (Object.FindAnyObjectByType<ResultScreen>(FindObjectsInactive.Include) != null)
        {
            CLogger.Log("BattleUISetup", "결과 화면이 이미 씬에 있습니다.");
            return false;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CommonPath.ResultScreenPrefab);
        if (prefab == null)
        {
            UIBuilder.SaveAsPrefab(BuildResultScreen().gameObject, CommonPath.ResultScreenPrefab);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CommonPath.ResultScreenPrefab);
        }

        PrefabUtility.InstantiatePrefab(prefab);
        UIBuilder.CreateEventSystemIfNeeded();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        return true;
    }

    // ==================================================

    private static BattleHud BuildBattleHud()
    {
        RectTransform safe = UIBuilder.CreateCanvas("BattleHud", HudSortingOrder);
        Transform root = safe.parent;

        // 좌상단 밤 시계: 채움(Radial) 위에 테두리, 아래에 남은 시간
        RectTransform clockRoot = UIBuilder.Anchored("NightClock", safe, new Vector2(0, 1), new Vector2(40, -30), new Vector2(160, 160));
        Image fill = SpriteImage("Fill", clockRoot, "HUD_NightClock_Fill", 0f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = false;
        SpriteImage("Frame", clockRoot, "HUD_NightClock_Frame", 0f);
        RectTransform timeRect = UIBuilder.Rect("Time", clockRoot, Vector2.zero, new Vector2(1, 0), new Vector2(-40, -56), new Vector2(40, 0));
        TMP_Text time = UIBuilder.Label(timeRect, "05:00", UIStyle.FontLarge);
        UINightClock clock = clockRoot.gameObject.AddComponent<UINightClock>();
        UIBuilder.SetRef(clock, "m_Fill", fill);
        UIBuilder.SetRef(clock, "m_Text", time);

        // 중앙 하단 체력
        UIGauge hp = UIBuilder.Gauge(safe, "Hp", new Vector2(720, 44), UIStyle.HpPlayer, true);
        RectTransform hpRect = (RectTransform)hp.transform;
        hpRect.anchorMin = hpRect.anchorMax = hpRect.pivot = new Vector2(0.5f, 0f);
        hpRect.anchoredPosition = new Vector2(0, 50);

        // 우하단 스킬 아이콘(표시만)
        SkillSlot(safe, "Attack", "HUD_Attack", new Vector2(-50, 50), 150f, out GameObject attackActive, out GameObject attackChain);
        SkillSlot(safe, "Dash", "HUD_Dash", new Vector2(-230, 50), 110f, out GameObject dashActive, out _);

        // 우상단 토벌 종료: 동그란 깃발 버튼 + 아래 작은 글자
        RectTransform exitRect = UIBuilder.Anchored("ExitButton", safe, new Vector2(1, 1), new Vector2(-50, -30), new Vector2(110, 110));
        Image exitBack = SpriteImage("Back", exitRect, CircleSprite, 0f);
        exitBack.color = new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.85f);
        exitBack.raycastTarget = true;
        SpriteImage("Icon", exitRect, "HUD_EndHunt", 24f);
        RectTransform exitText = UIBuilder.Rect("Text", exitRect, new Vector2(-0.5f, 0), new Vector2(1.5f, 0), new Vector2(0, -40), Vector2.zero);
        UIBuilder.Label(exitText, "토벌 종료", UIStyle.FontSmall);
        Button exit = exitRect.gameObject.AddComponent<Button>();
        exit.targetGraphic = exitBack;

        // 그 아래 주운 버섯: 합계 줄 + 작은 목록(버섯·신선도별)
        RectTransform loot = UIBuilder.Anchored("Loot", safe, new Vector2(1, 1), new Vector2(-40, -200), new Vector2(160, 56));
        UIBuilder.AddImage(loot, new Color(0f, 0f, 0f, 0.35f));
        RectTransform lootIcon = UIBuilder.Anchored("Icon", loot, new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(40, 40));
        SpriteImage(lootIcon, "Icon_001_Songi");
        RectTransform countRect = UIBuilder.Rect("Count", loot, Vector2.zero, Vector2.one, new Vector2(60, 0), new Vector2(-14, 0));
        TMP_Text count = UIBuilder.Label(countRect, "0", UIStyle.FontNormal, TextAlignmentOptions.Right);

        RectTransform lootList = UIBuilder.Anchored("LootList", safe, new Vector2(1, 1), new Vector2(-40, -264), new Vector2(420, 0));
        VerticalLayoutGroup layout = lootList.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperRight;
        layout.spacing = 4f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        lootList.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        UILootRow rowTemplate = LootRow(safe);

        BattleHud hud = root.gameObject.AddComponent<BattleHud>();
        UIBuilder.SetRef(hud, "m_Clock", clock);
        UIBuilder.SetRef(hud, "m_Hp", hp);
        UIBuilder.SetRef(hud, "m_AttackActive", attackActive);
        UIBuilder.SetRef(hud, "m_AttackChain", attackChain);
        UIBuilder.SetRef(hud, "m_DashActive", dashActive);
        UIBuilder.SetRef(hud, "m_LootIcon", lootIcon);
        UIBuilder.SetRef(hud, "m_LootCount", count);
        UIBuilder.SetRef(hud, "m_LootList", lootList);
        UIBuilder.SetRef(hud, "m_LootRowTemplate", rowTemplate);
        UIBuilder.SetRef(hud, "m_ExitButton", exit);
        UIBuilder.SetRef(hud, "m_DeathPopupPrefab", AssetDatabase.LoadAssetAtPath<DeathPopup>(CommonPath.DeathPopupPrefab));

        return hud;
    }

    // 사망 팝업: 뒤가 비치는 어두운 배경 + 가운데 창(부활/귀가). 팝업 루트(PopupService) 아래에 꽉 차게 뜬다
    private static DeathPopup BuildDeathPopup()
    {
        RectTransform root = new GameObject("DeathPopup", typeof(RectTransform)).GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        UIBuilder.AddImage(root, new Color(0f, 0f, 0f, 0.45f), true); // 뒤 화면 터치를 막는다

        RectTransform window = UIBuilder.Anchored("Window", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 440));
        UIBuilder.AddImage(window, UIStyle.Panel, true);

        TMP_Text title = TopLabel(window, "Title", "쓰러졌다", UIStyle.FontTitle, -40, 100);
        title.color = UIStyle.Accent;
        title.fontStyle = FontStyles.Bold;

        TMP_Text desc = TopLabel(window, "Desc", "귀가하면 주운 버섯을 모두 잃어요", UIStyle.FontNormal, -150, 50);
        desc.color = UIStyle.TextDim;

        Button revive = UIBuilder.Button(window, "ReviveButton", "부활 (100 골드)", new Vector2(320, 110), UIStyle.Accent);
        PlaceBottom(revive, -170);
        Button home = UIBuilder.Button(window, "HomeButton", "귀가", new Vector2(260, 110), UIStyle.PanelLight);
        PlaceBottom(home, 190);

        DeathPopup popup = root.gameObject.AddComponent<DeathPopup>();
        UIBuilder.SetRef(popup, "m_ReviveButton", revive);
        UIBuilder.SetRef(popup, "m_ReviveLabel", revive.GetComponentInChildren<TMP_Text>());
        UIBuilder.SetRef(popup, "m_HomeButton", home);

        return popup;
    }

    // 주운 버섯 목록 한 줄 원본. 목록 밖에 꺼 둔다(목록 자식이면 레이아웃이 원본까지 줄 세운다)
    private static UILootRow LootRow(Transform parent)
    {
        RectTransform row = UIBuilder.Rect("LootRowTemplate", parent, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-420, -36), Vector2.zero);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
        UIBuilder.AddImage(row, new Color(0f, 0f, 0f, 0.25f));

        RectTransform iconRect = UIBuilder.Anchored("Icon", row, new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(30, 30));
        Image icon = SpriteImage(iconRect, "Icon_001_Songi");
        RectTransform textRect = UIBuilder.Rect("Text", row, Vector2.zero, Vector2.one, new Vector2(44, 0), new Vector2(-8, 0));
        TMP_Text text = UIBuilder.Label(textRect, "송이 신선 x1", UIStyle.FontSmall, TextAlignmentOptions.Left);

        UILootRow lootRow = row.gameObject.AddComponent<UILootRow>();
        UIBuilder.SetRef(lootRow, "m_Icon", icon);
        UIBuilder.SetRef(lootRow, "m_Text", text);
        row.gameObject.SetActive(false);

        return lootRow;
    }

    // 스킬 아이콘 한 칸(반투명 원): 사용 중 금테(뒤), 바탕, 아이콘, 연계 가능 표시(위 + "연계" 글자)
    private static void SkillSlot(Transform parent, string name, string sprite, Vector2 position, float size, out GameObject active, out GameObject chain)
    {
        RectTransform slot = UIBuilder.Anchored(name, parent, new Vector2(1, 0), position, new Vector2(size, size));

        Image activeImage = SpriteImage("Active", slot, CircleSprite, -8f);
        activeImage.color = UIStyle.Gold;
        active = activeImage.gameObject;
        active.SetActive(false);

        SpriteImage("Back", slot, CircleSprite, 0f).color = new Color(0f, 0f, 0f, 0.4f);
        SpriteImage("Icon", slot, sprite, size * 0.22f);

        Image chainImage = SpriteImage("Chain", slot, CircleSprite, 0f);
        chainImage.color = new Color(1f, 1f, 1f, 0.25f);
        RectTransform chainRect = (RectTransform)chainImage.transform;
        RectTransform chainText = UIBuilder.Rect("Text", chainRect, new Vector2(0, 1), Vector2.one, Vector2.zero, new Vector2(0, 50));
        UIBuilder.Label(chainText, "연계", UIStyle.FontNormal).color = UIStyle.Gold;
        chain = chainRect.gameObject;
        chain.SetActive(false);
    }

    private static Image SpriteImage(string name, Transform parent, string sprite, float inset)
    {
        return SpriteImage(UIBuilder.Stretch(name, parent, inset), sprite);
    }

    // 도트 스프라이트는 Art/UI/Sprites의 이름(확장자 없이)으로 찾는다
    private static Image SpriteImage(RectTransform rt, string sprite)
    {
        Image image = UIBuilder.AddImage(rt, Color.white);
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CommonPath.UISpriteDir + sprite + SpriteExtension);
        image.preserveAspect = true;
        if (image.sprite == null)
            CLogger.Warning("BattleUISetup", $"스프라이트가 없습니다: {sprite}");

        return image;
    }

    private static ResultScreen BuildResultScreen()
    {
        RectTransform safe = UIBuilder.CreateCanvas("ResultScreen", ResultSortingOrder);
        Transform root = safe.parent;

        // 어둡게 깔기는 노치까지 덮도록 SafeArea 밖에 둔다
        Image dim = UIBuilder.Panel("Dim", root, UIStyle.Dim, true);
        dim.transform.SetAsFirstSibling();

        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();

        // 창
        RectTransform window = UIBuilder.Anchored("Window", safe, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 800));
        window.pivot = new Vector2(0.5f, 0.5f);
        UIBuilder.AddImage(window, UIStyle.Panel, true);

        RectTransform shade = UIBuilder.Rect("Shade", window, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 12));
        UIBuilder.AddImage(shade, UIStyle.Background);

        TMP_Text title = TopLabel(window, "Title", "사냥 성공", UIStyle.FontTitle, -40, 100);
        title.color = UIStyle.Gold;
        title.fontStyle = FontStyles.Bold;

        TMP_Text summary = TopLabel(window, "Summary", "시간 00:00  ·  처치 0", UIStyle.FontNormal, -145, 50);
        summary.color = UIStyle.TextDim;

        TMP_Text lootHeader = TopLabel(window, "LootHeader", "주운 버섯", UIStyle.FontNormal, -225, 50);

        // 주운 버섯 칸 영역
        RectTransform lootRoot = UIBuilder.Rect("LootRoot", window, Vector2.zero, Vector2.one, new Vector2(60, 200), new Vector2(-60, -290));
        GridLayoutGroup grid = lootRoot.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(120, 120);
        grid.spacing = new Vector2(48, 64);
        grid.childAlignment = TextAnchor.UpperCenter;

        // !!) 템플릿은 그리드 밖에 둔다. 그리드 자식이면 에디터에서 레이아웃이 RectTransform을 고쳐 씬에 오버라이드가 생긴다
        UIItemSlot template = UIBuilder.ItemSlot(window, "SlotTemplate");
        template.gameObject.SetActive(false);

        TMP_Text empty = UIBuilder.Label(lootRoot.parent, "주운 버섯이 없어요", UIStyle.FontNormal, TextAlignmentOptions.Center, "EmptyLabel");
        empty.rectTransform.offsetMin = new Vector2(60, 200);
        empty.rectTransform.offsetMax = new Vector2(-60, -290);
        empty.color = UIStyle.TextDim;

        // 아래 버튼
        Button retry = UIBuilder.Button(window, "RetryButton", "다시 하기", new Vector2(320, 110), UIStyle.PanelLight);
        PlaceBottom(retry, -190);

        Button lobby = UIBuilder.Button(window, "LobbyButton", "로비로", new Vector2(320, 110));
        PlaceBottom(lobby, 190);

        ResultScreen screen = root.gameObject.AddComponent<ResultScreen>();
        UIBuilder.SetRef(screen, "m_Group", group);
        UIBuilder.SetRef(screen, "m_Window", window);
        UIBuilder.SetRef(screen, "m_Title", title);
        UIBuilder.SetRef(screen, "m_Summary", summary);
        UIBuilder.SetRef(screen, "m_LootRoot", lootRoot);
        UIBuilder.SetRef(screen, "m_SlotTemplate", template);
        UIBuilder.SetRef(screen, "m_EmptyLabel", empty.gameObject);
        UIBuilder.SetRef(screen, "m_RetryButton", retry);
        UIBuilder.SetRef(screen, "m_LobbyButton", lobby);

        return screen;
    }

    // 창 위쪽에서 y만큼 내려온 가로 한 줄 라벨
    private static TMP_Text TopLabel(Transform parent, string name, string text, int size, float top, float height)
    {
        RectTransform rt = UIBuilder.Rect(name, parent, new Vector2(0, 1), Vector2.one, new Vector2(40, top - height), new Vector2(-40, top));
        return UIBuilder.Label(rt, text, size);
    }

    private static void PlaceBottom(Button button, float x)
    {
        RectTransform rt = (RectTransform)button.transform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(x, 50);
    }
}
