using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 전투 씬 UI(HUDBattle과 그 팝업) 프리팹을 만든다. 메뉴는 UISetup의 "모든 UI 다시 만들기" 하나다
public static class BattleUISetup
{

    private const int HudSortingOrder = 10;
    private const int LootFontSize = 27; // 주운 버섯 목록 글자 크기
    private const float SkillIconSize = 64f; // 이어 쓸 수 있는 스킬 줄의 작은 아이콘
    private const string CircleSprite = "UI_Circle"; // 반투명 원 바탕(스킬 아이콘·토벌 종료). 없으면 사각형으로 보인다

    // ==================================================

    // 사망·결과 팝업 → 주운 버섯 줄 → 전투 HUD 순서로 만든다. isOverwrite면 있어도 다시 만든다(GUID 유지).
    // 팝업은 HUD가 참조하지 않고 PopupService.Open<T>(EPopupType)가 PopupDef에서 꺼낸다
    public static void BuildPrefabs(bool isOverwrite)
    {
        UIBuilder.SaveAsPrefab(BuildDeathPopup().gameObject, EditorPath.PopupDeathPrefab, isOverwrite);
        UIBuilder.SaveAsPrefab(BuildResultPopup().gameObject, EditorPath.PopupResultPrefab, isOverwrite);
        UIBuilder.SaveAsPrefab(LootRow().gameObject, EditorPath.BattleLootRowPrefab, isOverwrite);
        UIBuilder.SaveAsPrefab(SkillIcon().gameObject, EditorPath.BattleSkillIconPrefab, isOverwrite);
        UIBuilder.SaveAsPrefab(UnitHpBar().gameObject, EditorPath.BattleUnitHpBarPrefab, isOverwrite);
        UIBuilder.SaveAsPrefab(BuildBattleHud().gameObject, EditorPath.HUDBattlePrefab, isOverwrite);
    }

    // 전투 씬을 만들 때(BattleSetup) 부른다. 없는 UI 프리팹을 만들고 HUD를 씬에 넣는다. 씬에 새로 넣었으면 true
    public static bool AddBattleHud()
    {
        if (Object.FindAnyObjectByType<HUDBattle>(FindObjectsInactive.Include) != null)
        {
            CLogger.Log("BattleUISetup", "전투 HUD가 이미 씬에 있습니다.");
            return false;
        }

        UISetup.BuildAllPrefabs(false);
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EditorPath.HUDBattlePrefab));
        UIBuilder.CreateEventSystemIfNeeded();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        return true;
    }

    // ==================================================

    private static HUDBattle BuildBattleHud()
    {
        RectTransform safe = UIBuilder.CreateCanvas("HUDBattle", HudSortingOrder);
        Transform root = safe.parent;

        // 좌상단 밤 시계
        UINightClock clock = BuildNightClock(safe);

        // 중앙 하단 체력
        UIGauge hp = UIBuilder.Gauge(safe, "Hp", new Vector2(720, 44), UIStyle.HpPlayer, true);
        RectTransform hpRect = hp.RectTransform;
        if (hpRect != null)
        {
            hpRect.anchorMin = hpRect.anchorMax = hpRect.pivot = new Vector2(0.5f, 0f);
            hpRect.anchoredPosition = new Vector2(0, 50);
        }

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
        RectTransform lootRect = UIBuilder.Anchored("LootList", safe, new Vector2(1, 1), new Vector2(-40, -200), new Vector2(507, 0));
        UIListLayout lootList = UIBuilder.ListLayout(lootRect, UIListLayout.EAxis.Vertical, new Vector2(0, 43), new Vector2(0, 3), TextAnchor.UpperRight,
            isStretch: true, isFitContent: true);

        AddUnitHpBars(safe);
        AddRoarEffect(safe);

        HUDBattle hud = root.gameObject.AddComponent<HUDBattle>();
        UIBuilder.SetRef(hud, "m_Clock", clock);
        UIBuilder.SetRef(hud, "m_Hp", hp);
        UIBuilder.SetRef(hud, "m_AttackActive", attackActive);
        UIBuilder.SetRef(hud, "m_DashActive", dashActive);
        UIBuilder.SetRef(hud, "m_LootList", lootList);
        UIBuilder.SetRef(hud, "m_ExitButton", exit);

        return hud;
    }

    // 몬스터 머리 위 체력바 원본(독립 프리팹, UIGauge). 피벗은 아래 가운데라 머리 위 점에 바닥을 붙인다
    private static UIGauge UnitHpBar()
    {
        UIGauge bar = UIBuilder.BuildGauge(null);
        bar.name = "HUDBattle_UnitHpBar";

        RectTransform rt = bar.RectTransform;
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(120, 14);
            rt.pivot = new Vector2(0.5f, 0f);
        }

        bar.transform.Find("Inner/Fill").GetComponent<Image>().color = UIStyle.HpBoss;
        bar.GetComponentInChildren<TMP_Text>(true).gameObject.SetActive(false);

        return bar;
    }

    // 화면 위 보스 체력바 + 머리 위 바 자리. HUD 맨 뒤에 깔아 다른 HUD를 가리지 않는다
    private static HUDBattle_UnitHpBars AddUnitHpBars(RectTransform safe)
    {
        RectTransform root = UIBuilder.Stretch("UnitHpBars", safe);
        root.SetAsFirstSibling();

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
        RectTransform barRect = bossBar.RectTransform;
        if (barRect != null)
        {
            barRect.anchorMin = barRect.anchorMax = barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(0, 8);
        }

        HUDBattle_UnitHpBars bars = root.gameObject.AddComponent<HUDBattle_UnitHpBars>();
        UIBuilder.SetRef(bars, "m_Root", root);
        UIBuilder.SetRef(bars, "m_BossRoot", boss.gameObject);
        UIBuilder.SetRef(bars, "m_BossBar", bossBar);
        UIBuilder.SetRef(bars, "m_BossName", bossName);
        UIBuilder.SetRef(bars, "m_EnrageIcon", enrage.gameObject);
        boss.gameObject.SetActive(false);

        return bars;
    }

    // 보스 포효 연출: 노치까지 덮는 붉은 가장자리(맨 뒤) + 가운데 위 이름 띠(맨 앞)
    private static HUDBattle_RoarEffect AddRoarEffect(RectTransform safe)
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

        HUDBattle_RoarEffect effect = banner.gameObject.AddComponent<HUDBattle_RoarEffect>();
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
    private static PopupDeath BuildDeathPopup()
    {
        RectTransform root = new GameObject("PopupDeath", typeof(RectTransform)).GetComponent<RectTransform>();
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

        PopupDeath popup = root.gameObject.AddComponent<PopupDeath>();
        UIBuilder.SetRef(popup, "m_Group", root.gameObject.AddComponent<CanvasGroup>()); // 여는 연출(Popup)
        UIBuilder.SetRef(popup, "m_Window", window);
        UIBuilder.SetRef(popup, "m_ReviveButton", revive);
        UIBuilder.SetRef(popup, "m_ReviveLabel", revive.GetComponentInChildren<TMP_Text>());
        UIBuilder.SetRef(popup, "m_HomeButton", home);

        return popup;
    }

    // 주운 버섯 목록 한 줄 원본(독립 프리팹)
    private static HUDBattle_LootRow LootRow()
    {
        RectTransform row = UIBuilder.Rect("HUDBattle_LootRow", null, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-507, -43), Vector2.zero);
        UIBuilder.AddImage(row, new Color(0f, 0f, 0f, 0.6f));

        RectTransform iconRect = UIBuilder.Anchored("Icon", row, new Vector2(0, 0.5f), new Vector2(5, 0), new Vector2(35, 35));
        Image icon = SpriteImage(iconRect, "Icon_001_Songi");
        RectTransform textRect = UIBuilder.Rect("Text", row, Vector2.zero, Vector2.one, new Vector2(47, 0), new Vector2(-5, 0));
        TMP_Text text = UIBuilder.Label(textRect, "송이 신선 x1", LootFontSize, TextAlignmentOptions.Left);

        HUDBattle_LootRow lootRow = row.gameObject.AddComponent<HUDBattle_LootRow>();
        UIBuilder.SetRef(lootRow, "m_Icon", icon);
        UIBuilder.SetRef(lootRow, "m_Text", text);

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

    // 공격 칸 위에 이어 쓸 수 있는 스킬 줄(작은 원 아이콘). 큰 아이콘은 칸의 Icon을 지금 공격으로 바꿔 쓴다
    private static HUDBattle_SkillChain AddSkillChain(RectTransform attackSlot)
    {
        RectTransform chainRoot = UIBuilder.Rect("Chain", attackSlot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-200, 16), new Vector2(200, 16 + SkillIconSize));
        UIListLayout chainLayout = UIBuilder.ListLayout(chainRoot, UIListLayout.EAxis.Horizontal, new Vector2(SkillIconSize, SkillIconSize), new Vector2(10, 0), TextAnchor.LowerCenter);

        // 작은 아이콘은 HUDBattle_SkillIcon 프리팹(CommonPath)을 HUDBattle_SkillChain이 풀로 꺼낸다
        HUDBattle_SkillChain chain = attackSlot.gameObject.AddComponent<HUDBattle_SkillChain>();
        UIBuilder.SetRef(chain, "m_MainIcon", attackSlot.Find("Icon").GetComponent<Image>());
        UIBuilder.SetRef(chain, "m_ChainRoot", chainLayout);

        return chain;
    }

    // 이어 쓸 수 있는 스킬 줄의 작은 아이콘 원본(독립 프리팹): 반투명 원 바탕 + 아이콘
    private static HUDBattle_SkillIcon SkillIcon()
    {
        RectTransform rt = UIBuilder.Anchored("HUDBattle_SkillIcon", null, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(SkillIconSize, SkillIconSize));
        SpriteImage("Back", rt, CircleSprite, 0f).color = new Color(0f, 0f, 0f, 0.6f);
        Image icon = SpriteImage("Icon", rt, "HUD_Attack", SkillIconSize * 0.2f);
        CanvasGroup group = rt.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        HUDBattle_SkillIcon skillIcon = rt.gameObject.AddComponent<HUDBattle_SkillIcon>();
        UIBuilder.SetRef(skillIcon, "m_Group", group);
        UIBuilder.SetRef(skillIcon, "m_Icon", icon);

        return skillIcon;
    }

    private static Image SpriteImage(string name, Transform parent, string sprite, float inset)
    {
        return SpriteImage(UIBuilder.Stretch(name, parent, inset), sprite);
    }

    // 도트 스프라이트는 Art/UI/Sprites 아래에서 이름(확장자 없이)으로 찾는다
    private static Image SpriteImage(RectTransform rt, string sprite)
    {
        Image image = UIBuilder.AddImage(rt, Color.white);
        image.sprite = UIBuilder.LoadSprite(sprite);
        image.preserveAspect = true;

        return image;
    }

    // 팝업 형태(꽉 찬 루트 + 어두운 바탕 + 가운데 창). PopupService가 팝업 캔버스 아래에 띄운다
    private static PopupResult BuildResultPopup()
    {
        RectTransform root = new GameObject("PopupResult", typeof(RectTransform)).GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        UIBuilder.AddImage(root, UIStyle.Dim, true); // 뒤 화면 터치를 막는다

        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();

        // 창
        RectTransform window = UIBuilder.Anchored("Window", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 800));
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
        UIListLayout lootLayout = UIBuilder.ListLayout(lootRoot, UIListLayout.EAxis.Grid, new Vector2(120, 120), new Vector2(48, 64), TextAnchor.UpperCenter);

        // 칸 원본은 UICommonSlot 프리팹(EUIComponentType.UICommonSlot). PopupResult가 UIListView로 꺼낸다
        TMP_Text empty = UIBuilder.Label(lootRoot.parent, "주운 버섯이 없어요", UIStyle.FontNormal, TextAlignmentOptions.Center, "EmptyLabel");
        empty.rectTransform.offsetMin = new Vector2(60, 200);
        empty.rectTransform.offsetMax = new Vector2(-60, -290);
        empty.color = UIStyle.TextDim;

        // 아래 버튼
        Button retry = UIBuilder.Button(window, "RetryButton", "다시 하기", new Vector2(320, 110), UIStyle.PanelLight);
        PlaceBottom(retry, -190);

        Button lobby = UIBuilder.Button(window, "LobbyButton", "로비로", new Vector2(320, 110));
        PlaceBottom(lobby, 190);

        PopupResult popup = root.gameObject.AddComponent<PopupResult>();
        UIBuilder.SetRef(popup, "m_Group", group);
        UIBuilder.SetRef(popup, "m_Window", window);
        UIBuilder.SetRef(popup, "m_Title", title);
        UIBuilder.SetRef(popup, "m_Summary", summary);
        UIBuilder.SetRef(popup, "m_LootRoot", lootLayout);
        UIBuilder.SetRef(popup, "m_EmptyLabel", empty.gameObject);
        UIBuilder.SetRef(popup, "m_RetryButton", retry);
        UIBuilder.SetRef(popup, "m_LobbyButton", lobby);

        return popup;
    }

    // 창 위쪽에서 y만큼 내려온 가로 한 줄 라벨
    private static TMP_Text TopLabel(Transform parent, string name, string text, int size, float top, float height)
    {
        RectTransform rt = UIBuilder.Rect(name, parent, new Vector2(0, 1), Vector2.one, new Vector2(40, top - height), new Vector2(-40, top));
        return UIBuilder.Label(rt, text, size);
    }

    private static void PlaceBottom(Button button, float x)
    {
        RectTransform rt = button.transform as RectTransform;
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(x, 50);
    }
}
