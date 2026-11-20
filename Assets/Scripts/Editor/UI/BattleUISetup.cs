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
    private const int LootFontSize = 27; // 주운 버섯 목록 글자 크기
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

        if (AssetDatabase.LoadAssetAtPath<GameObject>(EditorPath.DeathPopupPrefab) == null)
            UIBuilder.SaveAsPrefab(BuildDeathPopup().gameObject, EditorPath.DeathPopupPrefab);

        MarketUISetup.CreateIfMissing();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EditorPath.BattleHudPrefab);
        if (prefab == null)
        {
            UIBuilder.SaveAsPrefab(BuildBattleHud().gameObject, EditorPath.BattleHudPrefab);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EditorPath.BattleHudPrefab);
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

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EditorPath.ResultScreenPrefab);
        if (prefab == null)
        {
            UIBuilder.SaveAsPrefab(BuildResultScreen().gameObject, EditorPath.ResultScreenPrefab);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EditorPath.ResultScreenPrefab);
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

        // 좌상단 밤 시계
        UINightClock clock = BuildNightClock(safe);

        // 중앙 하단 체력
        UIGauge hp = UIBuilder.Gauge(safe, "Hp", new Vector2(720, 44), UIStyle.HpPlayer, true);
        RectTransform hpRect = (RectTransform)hp.transform;
        hpRect.anchorMin = hpRect.anchorMax = hpRect.pivot = new Vector2(0.5f, 0f);
        hpRect.anchoredPosition = new Vector2(0, 50);

        // 우하단 스킬 아이콘(표시만)
        RectTransform attackSlot = SkillSlot(safe, "Attack", "HUD_Attack", new Vector2(-50, 50), 150f, out GameObject attackActive);
        SkillSlot(safe, "Dash", "HUD_Dash", new Vector2(-230, 50), 110f, out GameObject dashActive);
        AddSkillChain(attackSlot);

        // 우상단 토벌 종료: 동그란 깃발 버튼 + 아래 작은 글자
        RectTransform exitRect = UIBuilder.Anchored("ExitButton", safe, new Vector2(1, 1), new Vector2(-50, -30), new Vector2(110, 110));
        Image exitBack = SpriteImage("Back", exitRect, CircleSprite, 0f);
        exitBack.color = new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.85f);
        exitBack.raycastTarget = true;
        SpriteImage("Icon", exitRect, "HUD_EndHunt", 24f);
        RectTransform exitText = UIBuilder.Rect("Text", exitRect, new Vector2(-0.5f, 0), new Vector2(1.5f, 0), new Vector2(0, -40), Vector2.zero);
        UIBuilder.SetOutline(UIBuilder.Label(exitText, "토벌 종료", 30));
        Button exit = exitRect.gameObject.AddComponent<Button>();
        exit.targetGraphic = exitBack;

        // 그 아래 주운 버섯 목록(버섯·신선도별)
        RectTransform lootList = UIBuilder.Anchored("LootList", safe, new Vector2(1, 1), new Vector2(-40, -200), new Vector2(507, 0));
        VerticalLayoutGroup layout = lootList.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperRight;
        layout.spacing = 3f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        lootList.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        UILootRow rowTemplate = LootRow(safe);

        AddUnitHpBars(safe);
        AddRoarEffect(safe);

        BattleHud hud = root.gameObject.AddComponent<BattleHud>();
        UIBuilder.SetRef(hud, "m_Clock", clock);
        UIBuilder.SetRef(hud, "m_Hp", hp);
        UIBuilder.SetRef(hud, "m_AttackActive", attackActive);
        UIBuilder.SetRef(hud, "m_DashActive", dashActive);
        UIBuilder.SetRef(hud, "m_LootList", lootList);
        UIBuilder.SetRef(hud, "m_LootRowTemplate", rowTemplate);
        UIBuilder.SetRef(hud, "m_ExitButton", exit);
        UIBuilder.SetRef(hud, "m_DeathPopupPrefab", AssetDatabase.LoadAssetAtPath<DeathPopup>(EditorPath.DeathPopupPrefab));
        UIBuilder.SetRef(hud, "m_MarketPopupPrefab", AssetDatabase.LoadAssetAtPath<MarketPopup>(EditorPath.MarketPopupPrefab));

        return hud;
    }

    // 몬스터 머리 위 체력바(원본) + 화면 위 보스 체력바. HUD 맨 뒤에 깔아 다른 HUD를 가리지 않는다.
    // 이미 만든 HUD 프리팹에 붙일 때도 쓴다
    public static UIUnitHpBars AddUnitHpBars(RectTransform safe)
    {
        RectTransform root = UIBuilder.Stretch("UnitHpBars", safe);
        root.SetAsFirstSibling();

        UIGauge template = UIBuilder.Gauge(root, "BarTemplate", new Vector2(120, 14), UIStyle.HpBoss, false);
        RectTransform templateRect = (RectTransform)template.transform;
        templateRect.anchorMin = templateRect.anchorMax = new Vector2(0.5f, 0.5f);
        templateRect.pivot = new Vector2(0.5f, 0f);

        RectTransform boss = UIBuilder.Anchored("Boss", root, new Vector2(0.5f, 1f), new Vector2(0, -36), new Vector2(900, 90));
        boss.pivot = new Vector2(0.5f, 1f);
        RectTransform nameRect = UIBuilder.Rect("Name", boss, new Vector2(0, 1), Vector2.one, new Vector2(0, -44), Vector2.zero);
        TMP_Text bossName = UIBuilder.Label(nameRect, "보스", UIStyle.FontNormal);
        UIBuilder.SetOutline(bossName);

        RectTransform enrageRect = UIBuilder.Rect("Enrage", boss, new Vector2(1, 1), Vector2.one, new Vector2(-140, -44), Vector2.zero);
        TMP_Text enrage = UIBuilder.Label(enrageRect, "분노!", UIStyle.FontNormal, TextAlignmentOptions.Right);
        UIBuilder.SetOutline(enrage);
        enrage.gameObject.SetActive(false);

        UIGauge bossBar = UIBuilder.Gauge(boss, "Bar", new Vector2(900, 32), UIStyle.HpBoss, false);
        RectTransform barRect = (RectTransform)bossBar.transform;
        barRect.anchorMin = barRect.anchorMax = barRect.pivot = new Vector2(0.5f, 0f);
        barRect.anchoredPosition = new Vector2(0, 8);

        UIUnitHpBars bars = root.gameObject.AddComponent<UIUnitHpBars>();
        UIBuilder.SetRef(bars, "m_Root", root);
        UIBuilder.SetRef(bars, "m_BarTemplate", template);
        UIBuilder.SetRef(bars, "m_BossRoot", boss.gameObject);
        UIBuilder.SetRef(bars, "m_BossBar", bossBar);
        UIBuilder.SetRef(bars, "m_BossName", bossName);
        UIBuilder.SetRef(bars, "m_EnrageIcon", enrage.gameObject);
        template.gameObject.SetActive(false);
        boss.gameObject.SetActive(false);

        return bars;
    }

    // 보스 포효 연출: 노치까지 덮는 붉은 가장자리(맨 뒤) + 가운데 위 이름 띠(맨 앞). 이미 만든 HUD 프리팹에 붙일 때도 쓴다
    public static UIRoarEffect AddRoarEffect(RectTransform safe)
    {
        RectTransform vignette = UIBuilder.Stretch("RoarVignette", safe.parent);
        vignette.SetAsFirstSibling();
        Image vignetteImage = SpriteImage(vignette, "UI_Vignette");
        vignetteImage.preserveAspect = false;
        vignetteImage.color = UIStyle.Accent;
        CanvasGroup vignetteGroup = vignette.gameObject.AddComponent<CanvasGroup>();
        vignetteGroup.blocksRaycasts = false;
        vignetteGroup.interactable = false;

        RectTransform banner = UIBuilder.Rect("RoarBanner", safe, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 150), new Vector2(0, 290));
        banner.SetAsLastSibling();
        UIBuilder.AddImage(banner, new Color(0f, 0f, 0f, 0.6f));
        TMP_Text text = UIBuilder.Label(banner, "보스", UIStyle.FontTitle);
        CanvasGroup bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
        bannerGroup.blocksRaycasts = false;
        bannerGroup.interactable = false;

        UIRoarEffect effect = banner.gameObject.AddComponent<UIRoarEffect>();
        UIBuilder.SetRef(effect, "m_Vignette", vignetteGroup);
        UIBuilder.SetRef(effect, "m_Banner", bannerGroup);
        UIBuilder.SetRef(effect, "m_BannerText", text);

        return effect;
    }

    // 밤 시계: 둥근 마스크 안에서 하늘 띠가 내려가고, 위에 흰 테두리, 아래에 남은 시간과 시장용 작은 글자("행동당 30분").
    // 전투 HUD와 시장 팝업이 같은 자리(좌상단)에 같은 모양으로 쓴다. 시장 팝업은 어두운 바탕 위라 외곽선 없는 글자를 쓴다
    public static UINightClock BuildNightClock(Transform parent, bool isOutline = true)
    {
        RectTransform clockRoot = UIBuilder.Anchored("NightClock", parent, new Vector2(0, 1), new Vector2(40, -30), new Vector2(160, 160));
        Image mask = SpriteImage("Mask", clockRoot, "HUD_NightClock_Fill", 0f);
        mask.preserveAspect = false;
        mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        RectTransform sky = UIBuilder.Rect("Sky", mask.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
        sky.pivot = new Vector2(0.5f, 0f);
        sky.sizeDelta = new Vector2(160f, 160f * 5f); // 48x240 도트 비율
        SpriteImage(sky, "HUD_NightClock_Sky").preserveAspect = false;
        SpriteImage("Frame", clockRoot, "HUD_NightClock_Frame", 0f);
        RectTransform timeRect = UIBuilder.Rect("Time", clockRoot, Vector2.zero, new Vector2(1, 0), new Vector2(-40, -56), new Vector2(40, 0));
        TMP_Text time = UIBuilder.Label(timeRect, "05:00", UIStyle.FontLarge);
        RectTransform subRect = UIBuilder.Rect("Sub", clockRoot, Vector2.zero, new Vector2(1, 0), new Vector2(-40, -90), new Vector2(40, -56));
        TMP_Text sub = UIBuilder.Label(subRect, "행동당 30분", UIStyle.FontSmall, TextAlignmentOptions.Left);
        if (isOutline)
        {
            UIBuilder.SetOutline(time);
            UIBuilder.SetOutline(sub);
        }

        sub.color = UIStyle.TextDim;
        sub.gameObject.SetActive(false);

        UINightClock clock = clockRoot.gameObject.AddComponent<UINightClock>();
        UIBuilder.SetRef(clock, "m_Sky", sky);
        UIBuilder.SetRef(clock, "m_Text", time);
        UIBuilder.SetRef(clock, "m_SubText", sub);

        return clock;
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
        RectTransform row = UIBuilder.Rect("LootRowTemplate", parent, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-507, -43), Vector2.zero);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 43f;
        UIBuilder.AddImage(row, new Color(0f, 0f, 0f, 0.6f));

        RectTransform iconRect = UIBuilder.Anchored("Icon", row, new Vector2(0, 0.5f), new Vector2(5, 0), new Vector2(35, 35));
        Image icon = SpriteImage(iconRect, "Icon_001_Songi");
        RectTransform textRect = UIBuilder.Rect("Text", row, Vector2.zero, Vector2.one, new Vector2(47, 0), new Vector2(-5, 0));
        TMP_Text text = UIBuilder.Label(textRect, "송이 신선 x1", LootFontSize, TextAlignmentOptions.Left);

        UILootRow lootRow = row.gameObject.AddComponent<UILootRow>();
        UIBuilder.SetRef(lootRow, "m_Icon", icon);
        UIBuilder.SetRef(lootRow, "m_Text", text);
        row.gameObject.SetActive(false);

        return lootRow;
    }

    // 스킬 아이콘 한 칸(반투명 원): 사용 중 금테(뒤), 바탕, 아이콘
    private static RectTransform SkillSlot(Transform parent, string name, string sprite, Vector2 position, float size, out GameObject active)
    {
        RectTransform slot = UIBuilder.Anchored(name, parent, new Vector2(1, 0), position, new Vector2(size, size));

        Image activeImage = SpriteImage("Active", slot, CircleSprite, -8f);
        activeImage.color = UIStyle.Gold;
        active = activeImage.gameObject;
        active.SetActive(false);

        SpriteImage("Back", slot, CircleSprite, 0f).color = new Color(0f, 0f, 0f, 0.6f);
        SpriteImage("Icon", slot, sprite, size * 0.22f);

        return slot;
    }

    // 공격 칸 위에 이어 쓸 수 있는 스킬 줄(작은 원 아이콘). 큰 아이콘은 칸의 Icon을 지금 공격으로 바꿔 쓴다.
    // 이미 만든 HUD 프리팹에 붙일 때도 쓴다
    public static UISkillChain AddSkillChain(RectTransform attackSlot)
    {
        const float iconSize = 64f;

        RectTransform chainRoot = UIBuilder.Rect("Chain", attackSlot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-200, 16), new Vector2(200, 16 + iconSize));
        HorizontalLayoutGroup layout = chainRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.spacing = 10f;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        // 원본은 줄 밖에 꺼 둔다(줄 자식이면 레이아웃이 원본까지 세운다)
        RectTransform template = UIBuilder.Anchored("ChainIconTemplate", attackSlot, new Vector2(0.5f, 1f), new Vector2(0, 16), new Vector2(iconSize, iconSize));
        SpriteImage("Back", template, CircleSprite, 0f).color = new Color(0f, 0f, 0f, 0.6f);
        Image icon = SpriteImage("Icon", template, "HUD_Attack", iconSize * 0.2f);
        CanvasGroup group = template.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        UISkillIcon skillIcon = template.gameObject.AddComponent<UISkillIcon>();
        UIBuilder.SetRef(skillIcon, "m_Group", group);
        UIBuilder.SetRef(skillIcon, "m_Icon", icon);
        template.gameObject.SetActive(false);

        UISkillChain chain = attackSlot.gameObject.AddComponent<UISkillChain>();
        UIBuilder.SetRef(chain, "m_MainIcon", attackSlot.Find("Icon").GetComponent<Image>());
        UIBuilder.SetRef(chain, "m_ChainRoot", chainRoot);
        UIBuilder.SetRef(chain, "m_ChainTemplate", skillIcon);

        return chain;
    }

    private static Image SpriteImage(string name, Transform parent, string sprite, float inset)
    {
        return SpriteImage(UIBuilder.Stretch(name, parent, inset), sprite);
    }

    // 도트 스프라이트는 Art/UI/Sprites의 이름(확장자 없이)으로 찾는다
    private static Image SpriteImage(RectTransform rt, string sprite)
    {
        Image image = UIBuilder.AddImage(rt, Color.white);
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(EditorPath.UISpriteDir + sprite + SpriteExtension);
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
