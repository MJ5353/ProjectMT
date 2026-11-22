using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 전투 씬 UI(ScreenBattle과 그 팝업) 프리팹을 만든다. 메뉴는 UISetup의 "모든 UI 다시 만들기" 하나다
public static class BattleUISetup
{

    private const int LootFontSize = 27; // 주운 버섯 목록 글자 크기
    private const float SkillIconSize = 64f; // 이어 쓸 수 있는 스킬 줄의 작은 아이콘
    private const string CircleSprite = "UI_Circle"; // 반투명 원 바탕(스킬 아이콘). 없으면 사각형으로 보인다
    private const float ClockSize = 140f;                                   // 밤 시계 지름. 마스크·테두리가 어긋나지 않게 정사각형으로 둔다
    private static readonly Vector2 ClockPosition = new Vector2(44, -30);    // 좌상단 기준
    private static readonly Vector2 ExitButtonSize = new Vector2(135, 148);  // 토벌 종료: 바탕 없이 음각 아이콘 + 아래 라벨
    private static readonly Vector2 ExitButtonPosition = new Vector2(49, -190); // 시계 아래(좌상단 기준)
    private const float ExitIconInset = 25f;                                // 아이콘 좌우 여백(판 안쪽)
    private const float ExitLabelCenterY = -20f;                            // 라벨 띠 중심(버튼 아래 끝 기준, 판 아래로 살짝 걸침)
    private const string ExitIconSprite = "HUD_EndHunt_Engrave";           // 반투명 음각 아이콘
    private const float LabelBarHeight = 45f;                               // 토벌 종료 아래 반투명 검은 띠
    private static readonly Vector2 ClockTimePosition = new Vector2(14, 18);  // 시계 오른쪽 가운데 기준 남은 시간(시장, 아래 작은 글자 자리만큼 위로)
    private static readonly Vector2 ClockWindowPosition = new Vector2(14, -6);  // 전투: 시계 오른쪽 파인 창(계기판) 안에 남은 시간
    private static readonly Vector2 ClockWindowSize = new Vector2(190, 68);
    private static readonly Vector2 ClockCaptionPosition = new Vector2(0, 40);  // 창 위 가운데 작은 "남은 밤"(창 위 끝 기준)
    private const float ClockCaptionHeight = 40f;
    private const string ClockWindowSprite = "HUD_Time_Window";             // 반투명 음각 창(9-slice)
    private const string ExitPlateSprite = "HUD_EndHunt_Button";
    private static readonly Color ExitPlateColor = new Color(0.6f, 0.67f, 0.55f, 0.55f); // 흰 판에 얹는 반투명 Tint
    private static readonly Vector2 ClockTimeSize = new Vector2(232, 105);
    private static readonly Vector2 ClockSubPosition = new Vector2(14, -52);  // 그 아래 "행동당 30분"(시장만)
    private static readonly Vector2 ClockSubSize = new Vector2(232, 36);

    // ==================================================

    // 사망·확인·시작·결과 팝업 → 주운 버섯 줄 → 전투 화면(ScreenBattle) 순서로 만든다. isOverwrite면 있어도 다시 만든다(GUID 유지).
    // 팝업은 ScreenBattle이 참조하지 않고 PopupService.Open<T>(EPopupType)가 PopupDef에서 꺼낸다
    public static readonly (string Path, System.Func<GameObject> Build)[] Prefabs =
    {
        (EditorPath.PopupDeathPrefab, () => BuildDeathPopup().gameObject),
        (EditorPath.PopupConfirmPrefab, () => BuildConfirmPopup().gameObject),
        (EditorPath.PopupHuntStartPrefab, () => BuildHuntStartPopup().gameObject),
        (EditorPath.PopupResultPrefab, () => BuildResultPopup().gameObject),
        (EditorPath.BattleLootRowPrefab, () => LootRow().gameObject),
        (EditorPath.BattleSkillIconPrefab, () => SkillIcon().gameObject),
        (EditorPath.BattleUnitHpBarPrefab, () => UnitHpBar().gameObject),
        (EditorPath.ScreenBattlePrefab, () => BuildBattleScreen().gameObject),
    };

    // 전투 씬을 만들 때(BattleSetup) 부른다. 없는 UI 프리팹을 만들고 전투 화면을 캔버스 아래에 넣는다. 프리팹이 없으면 null
    public static ScreenBattle AddBattleScreen(Transform canvas)
    {
        UISetup.BuildAllPrefabs(false);
        ScreenBattle screen = UIBuilder.PlaceScreen<ScreenBattle>(EditorPath.ScreenBattlePrefab, canvas);
        UIBuilder.CreateEventSystemIfNeeded();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        return screen;
    }

    // ==================================================

    private static ScreenBattle BuildBattleScreen()
    {
        RectTransform root = UIBuilder.CreateScreenRoot(nameof(ScreenBattle), out RectTransform safe);

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

        // 시계 아래 토벌 종료: 반투명 Tint 흰 판 위 음각 아이콘 + 아래 반투명 검은 띠 위 "토벌 종료". 누르면 확인 팝업을 거친다
        RectTransform exitRect = UIBuilder.Anchored("ExitButton", safe, new Vector2(0, 1), ExitButtonPosition, ExitButtonSize);
        UIBuilder.AddImage(exitRect, Color.clear, true); // 보이지 않는 누름 영역(버튼 전체)
        Image exitPlate = SpriteImage("Plate", exitRect, ExitPlateSprite, 0f);
        exitPlate.color = ExitPlateColor;
        Image exitIcon = SpriteImage("Icon", exitPlate.rectTransform, ExitIconSprite, 0f); // 판 안쪽, 좌우만 여백
        exitIcon.rectTransform.offsetMin = new Vector2(ExitIconInset, 0);
        exitIcon.rectTransform.offsetMax = new Vector2(-ExitIconInset, 0);

        Button exit = exitRect.gameObject.AddComponent<Button>();
        exit.targetGraphic = exitPlate; // 누르면 판이 어두워진다
        ColorBlock exitColors = exit.colors;
        exitColors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        exitColors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        exit.colors = exitColors;

        LabelBar(exitRect, "LabelBar", ExitLabelCenterY, 30f, EStringKey.Battle_Exit);

        // 그 아래 주운 버섯 목록(버섯·신선도별)
        RectTransform lootRect = UIBuilder.Anchored("LootList", safe, new Vector2(1, 1), new Vector2(-40, -49), new Vector2(507, 0));
        UIBuilder.ListOption lootList = new(lootRect, EUIListAxis.Vertical, new Vector2(0, 43), new Vector2(0, 3), TextAnchor.UpperRight,
            isStretch: true, isFitContent: true);

        AddUnitHpBars(safe);
        AddRoarEffect(safe);

        ScreenBattle screen = root.gameObject.AddComponent<ScreenBattle>();
        UIBuilder.SetRef(screen, "m_Clock", clock);
        UIBuilder.SetRef(screen, "m_Hp", hp);
        UIBuilder.SetRef(screen, "m_AttackActive", attackActive);
        UIBuilder.SetRef(screen, "m_DashActive", dashActive);
        UIBuilder.SetListView(screen, "m_LootPool", lootList);
        UIBuilder.SetRef(screen, "m_ExitButton", exit);

        return screen;
    }

    // 몬스터 머리 위 체력바 원본(독립 프리팹, UIGauge). 피벗은 아래 가운데라 머리 위 점에 바닥을 붙인다
    private static UIGauge UnitHpBar()
    {
        UIGauge bar = UIBuilder.BuildGauge(null);
        bar.name = "ScreenBattle_UnitHpBar";

        RectTransform rt = bar.RectTransform;
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(120, 14);
            rt.pivot = new Vector2(0.5f, 0f);
        }

        bar.EditorSetStyle(UIStyle.HpBoss, false);

        return bar;
    }

    // 화면 위 보스 체력바 + 머리 위 바 자리. HUD 맨 뒤에 깔아 다른 HUD를 가리지 않는다
    private static ScreenBattle_UnitHpBars AddUnitHpBars(RectTransform safe)
    {
        RectTransform root = UIBuilder.Stretch("UnitHpBars", safe);
        root.SetAsFirstSibling();

        RectTransform boss = UIBuilder.Anchored("Boss", root, new Vector2(0.5f, 1f), new Vector2(0, -36), new Vector2(900, 90));
        boss.pivot = new Vector2(0.5f, 1f);
        RectTransform nameRect = UIBuilder.Rect("Name", boss, new Vector2(0, 1), Vector2.one, new Vector2(0, -44), Vector2.zero);
        TMP_Text bossName = UIBuilder.Label(nameRect, "보스", UIStyle.FontNormal);
        UIBuilder.SetOutline(bossName);

        RectTransform enrageRect = UIBuilder.Rect("Enrage", boss, new Vector2(1, 1), Vector2.one, new Vector2(-140, -44), Vector2.zero);
        TMP_Text enrage = UIBuilder.Label(enrageRect, EStringKey.Battle_Enrage, UIStyle.FontNormal, TextAlignmentOptions.Right);
        UIBuilder.SetOutline(enrage);
        enrage.gameObject.SetActive(false);

        UIGauge bossBar = UIBuilder.Gauge(boss, "Bar", new Vector2(900, 32), UIStyle.HpBoss, false);
        RectTransform barRect = bossBar.RectTransform;
        if (barRect != null)
        {
            barRect.anchorMin = barRect.anchorMax = barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(0, 8);
        }

        ScreenBattle_UnitHpBars bars = root.gameObject.AddComponent<ScreenBattle_UnitHpBars>();
        UIBuilder.SetRef(bars, "m_Root", root);
        UIBuilder.SetRef(bars, "m_BossRoot", boss.gameObject);
        UIBuilder.SetRef(bars, "m_BossBar", bossBar);
        UIBuilder.SetRef(bars, "m_BossName", bossName);
        UIBuilder.SetRef(bars, "m_EnrageIcon", enrage.gameObject);
        boss.gameObject.SetActive(false);

        return bars;
    }

    // 보스 포효 연출: 노치까지 덮는 붉은 가장자리(맨 뒤) + 가운데 위 이름 띠(맨 앞)
    private static ScreenBattle_RoarEffect AddRoarEffect(RectTransform safe)
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

        ScreenBattle_RoarEffect effect = banner.gameObject.AddComponent<ScreenBattle_RoarEffect>();
        UIBuilder.SetRef(effect, "m_Vignette", vignetteGroup);
        UIBuilder.SetRef(effect, "m_Banner", bannerGroup);
        UIBuilder.SetRef(effect, "m_BannerText", text);

        return effect;
    }

    // 밤 시계: 둥근 마스크 안에서 하늘 띠가 내려가고, 위에 음각 테두리(마스크와 같은 칸).
    // 남은 시간은 시계 오른쪽. 시장(isMarket)은 아래에 작은 글자("행동당 30분")도 쓴다
    public static UINightClock BuildNightClock(Transform parent, bool isMarket = false)
    {
        RectTransform clockRoot = UIBuilder.Anchored("NightClock", parent, new Vector2(0, 1), ClockPosition, Vector2.one * ClockSize);
        Image mask = SpriteImage("Mask", clockRoot, "HUD_NightClock_Fill", 0f);
        mask.preserveAspect = false;
        mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        RectTransform sky = UIBuilder.Rect("Sky", mask.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
        sky.pivot = new Vector2(0.5f, 0f);
        sky.sizeDelta = new Vector2(ClockSize, ClockSize * 5f); // 48x240 도트 비율
        SpriteImage(sky, "HUD_NightClock_Sky").preserveAspect = false;
        SpriteImage("Frame", clockRoot, "HUD_NightClock_Frame", 0f); // 마스크와 같은 칸(음각 테두리)

        // 남은 시간은 시계 오른쪽(시장 프리팹에서 맞춘 자리 기준)
        RectTransform subRect = UIBuilder.Anchored("Sub", clockRoot, new Vector2(1, 0.5f), ClockSubPosition, ClockSubSize);
        subRect.pivot = new Vector2(0, 0.5f);

        TMP_Text time;
        if (isMarket)
        {
            // 시장: 어두운 바탕 위라 그냥 밝은 글자 두 줄("남은 시간\n5시간 30분")
            RectTransform timeRect = UIBuilder.Anchored("Time", clockRoot, new Vector2(1, 0.5f), ClockTimePosition, ClockTimeSize);
            timeRect.pivot = new Vector2(0, 0.5f);
            time = UIBuilder.Label(timeRect, "남은 시간\n5시간 30분", UIStyle.FontNormal, TextAlignmentOptions.Left);
        }
        else
        {
            // 전투: 계기판처럼 파인 창(음각) 안에 숫자, 위에 작은 "남은 밤". 정보는 창, 버튼은 둥근 판으로 모양을 나눈다
            RectTransform windowRect = UIBuilder.Anchored("Window", clockRoot, new Vector2(1, 0.5f), ClockWindowPosition, ClockWindowSize);
            windowRect.pivot = new Vector2(0, 0.5f);
            Image window = SpriteImage(windowRect, ClockWindowSprite);
            window.preserveAspect = false;
            window.type = Image.Type.Sliced;
            time = UIBuilder.Label(UIBuilder.Stretch("Time", windowRect), "05:00", UIStyle.FontLarge);

            RectTransform captionRect = UIBuilder.Anchored("Caption", windowRect, new Vector2(0.5f, 1f), ClockCaptionPosition, new Vector2(ClockWindowSize.x, ClockCaptionHeight));
            UIBuilder.SetOutline(UIBuilder.Fit(UIBuilder.Label(captionRect, EStringKey.Clock_NightLeft, UIStyle.FontSmall)));
        }

        UIBuilder.Fit(time);
        TMP_Text sub = UIBuilder.Fit(UIBuilder.Label(subRect, "행동당 30분", UIStyle.FontSmall, TextAlignmentOptions.Left));
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

        TMP_Text title = TopLabel(window, "Title", string.Empty, UIStyle.FontTitle, -40, 100);
        UIBuilder.AddStringKey(title, EStringKey.Death_Title);
        title.color = UIStyle.Accent;
        title.fontStyle = FontStyles.Bold;

        TMP_Text desc = TopLabel(window, "Desc", string.Empty, UIStyle.FontNormal, -150, 50);
        UIBuilder.AddStringKey(desc, EStringKey.Death_Desc);
        desc.color = UIStyle.TextDim;

        Button revive = UIBuilder.Button(window, "ReviveButton", "부활 (100 골드)", new Vector2(320, 110), UIStyle.Accent);
        PlaceBottom(revive, -170);
        Button home = UIBuilder.Button(window, "HomeButton", EStringKey.Death_Home, new Vector2(260, 110), UIStyle.PanelLight);
        PlaceBottom(home, 190);

        PopupDeath popup = root.gameObject.AddComponent<PopupDeath>();
        UIBuilder.SetRef(popup, "m_Group", root.gameObject.AddComponent<CanvasGroup>()); // 여는 연출(PopupBase)
        UIBuilder.SetRef(popup, "m_Window", window);
        UIBuilder.SetRef(popup, "m_ReviveButton", revive);
        UIBuilder.SetRef(popup, "m_ReviveLabel", revive.GetComponentInChildren<TMP_Text>());
        UIBuilder.SetRef(popup, "m_HomeButton", home);

        return popup;
    }

    // 예/아니오 확인 팝업(공용, 지금은 토벌 종료 확인에 쓴다): 어두운 배경 + 가운데 창(문구, 예/아니오)
    private static PopupConfirm BuildConfirmPopup()
    {
        RectTransform root = UIBuilder.Stretch(nameof(EPopupType.PopupConfirm), null);
        UIBuilder.AddImage(root, new Color(0f, 0f, 0f, 0.45f), true); // 뒤 화면 터치를 막는다

        RectTransform window = UIBuilder.Anchored("Window", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 360));
        UIBuilder.AddImage(window, UIStyle.Panel, true);

        // 두 줄 문구(시장 닫기 확인)도 들어가게 높이를 두고, 넘치면 글자를 줄인다
        TMP_Text message = UIBuilder.Fit(TopLabel(window, "Message", "확인", UIStyle.FontLarge, -40, 160));

        Button yes = UIBuilder.Button(window, "YesButton", EStringKey.Common_Yes, new Vector2(260, 110), UIStyle.Accent);
        PlaceBottom(yes, -150);
        Button no = UIBuilder.Button(window, "NoButton", EStringKey.Common_No, new Vector2(260, 110), UIStyle.PanelLight);
        PlaceBottom(no, 150);

        PopupConfirm popup = root.gameObject.AddComponent<PopupConfirm>();
        UIBuilder.SetRef(popup, "m_Group", root.gameObject.AddComponent<CanvasGroup>()); // 여는 연출(PopupBase)
        UIBuilder.SetRef(popup, "m_Window", window);
        UIBuilder.SetRef(popup, "m_Message", message);
        UIBuilder.SetRef(popup, "m_YesButton", yes);
        UIBuilder.SetRef(popup, "m_NoButton", no);

        return popup;
    }

    // 맵 진입 시작 팝업: 화면 전체 딤(어디를 눌러도 시작) + 가운데 맵 이름 + 아래 깜빡이는 "클릭하여 시작"
    private static PopupHuntStart BuildHuntStartPopup()
    {
        RectTransform root = UIBuilder.Stretch(nameof(EPopupType.PopupHuntStart), null);
        UIBuilder.AddImage(root, UIStyle.Dim, true); // 뒤 화면 터치를 막고 누르면 시작
        Button button = root.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;

        RectTransform mapRect = UIBuilder.Anchored("MapName", root, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1000, 110));
        mapRect.pivot = new Vector2(0.5f, 0.5f);
        TMP_Text mapText = UIBuilder.Label(mapRect, "평원", UIStyle.FontTitle);
        mapText.fontStyle = FontStyles.Bold;

        RectTransform tapRect = UIBuilder.Anchored("Tap", root, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(800, 70));
        tapRect.pivot = new Vector2(0.5f, 0.5f);
        TMP_Text tapText = UIBuilder.Label(tapRect, string.Empty, UIStyle.FontLarge);
        tapText.color = UIStyle.Gold;
        UIBuilder.AddStringKey(tapText, EStringKey.Battle_TapToStart);

        PopupHuntStart popup = root.gameObject.AddComponent<PopupHuntStart>();
        UIBuilder.SetRef(popup, "m_Group", root.gameObject.AddComponent<CanvasGroup>()); // 여는·닫는 페이드(PopupBase)
        UIBuilder.SetRef(popup, "m_Button", button);
        UIBuilder.SetRef(popup, "m_MapText", mapText);
        UIBuilder.SetRef(popup, "m_TapText", tapText);

        return popup;
    }

    // 주운 버섯 목록 한 줄 원본(독립 프리팹)
    private static ScreenBattle_LootRow LootRow()
    {
        RectTransform row = UIBuilder.Rect("ScreenBattle_LootRow", null, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-507, -43), Vector2.zero);
        UIBuilder.AddImage(row, new Color(0f, 0f, 0f, 0.6f));

        RectTransform iconRect = UIBuilder.Anchored("Icon", row, new Vector2(0, 0.5f), new Vector2(5, 0), new Vector2(35, 35));
        Image icon = SpriteImage(iconRect, "Icon_001_Songi");
        RectTransform textRect = UIBuilder.Rect("Text", row, Vector2.zero, Vector2.one, new Vector2(47, 0), new Vector2(-5, 0));
        TMP_Text text = UIBuilder.Label(textRect, "송이 신선 x1", LootFontSize, TextAlignmentOptions.Left);

        ScreenBattle_LootRow lootRow = row.gameObject.AddComponent<ScreenBattle_LootRow>();
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
    private static ScreenBattle_SkillChain AddSkillChain(RectTransform attackSlot)
    {
        RectTransform chainRoot = UIBuilder.Rect("Chain", attackSlot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-200, 16), new Vector2(200, 16 + SkillIconSize));
        UIBuilder.ListOption chainLayout = new(chainRoot, EUIListAxis.Horizontal, new Vector2(SkillIconSize, SkillIconSize), new Vector2(10, 0), TextAnchor.LowerCenter);

        // 작은 아이콘은 ScreenBattle_SkillIcon 프리팹(EUIComponentType.ScreenBattle_SkillIcon)을 ScreenBattle_SkillChain이 UIListView로 꺼낸다
        ScreenBattle_SkillChain chain = attackSlot.gameObject.AddComponent<ScreenBattle_SkillChain>();
        UIBuilder.SetRef(chain, "m_MainIcon", attackSlot.Find("Icon").GetComponent<Image>());
        UIBuilder.SetListView(chain, "m_ChainPool", chainLayout);

        return chain;
    }

    // 이어 쓸 수 있는 스킬 줄의 작은 아이콘 원본(독립 프리팹): 반투명 원 바탕 + 아이콘
    private static ScreenBattle_SkillIcon SkillIcon()
    {
        RectTransform rt = UIBuilder.Anchored("ScreenBattle_SkillIcon", null, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(SkillIconSize, SkillIconSize));
        SpriteImage("Back", rt, CircleSprite, 0f).color = new Color(0f, 0f, 0f, 0.6f);
        Image icon = SpriteImage("Icon", rt, "HUD_Attack", SkillIconSize * 0.2f);
        CanvasGroup group = rt.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        ScreenBattle_SkillIcon skillIcon = rt.gameObject.AddComponent<ScreenBattle_SkillIcon>();
        UIBuilder.SetRef(skillIcon, "m_Group", group);
        UIBuilder.SetRef(skillIcon, "m_Icon", icon);

        return skillIcon;
    }

    // 부모 아래쪽에 붙는 반투명 검은 띠 + 가운데 글자(시계 남은 시간·토벌 종료). centerY는 부모 아래 변 기준 띠 가운데 높이, extraWidth는 부모보다 넓힐 폭
    private static TMP_Text LabelBar(RectTransform parent, string name, float centerY, float extraWidth, EStringKey? key)
    {
        float half = LabelBarHeight * 0.5f;
        RectTransform bar = UIBuilder.Rect(name, parent, Vector2.zero, new Vector2(1, 0), new Vector2(-extraWidth * 0.5f, centerY - half), new Vector2(extraWidth * 0.5f, centerY + half));
        UIBuilder.AddImage(bar, UIStyle.Dim);

        return key.HasValue ? UIBuilder.Label(bar, key.Value, UIStyle.FontNormal) : UIBuilder.Label(bar, "05:00", UIStyle.FontNormal);
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

        TMP_Text lootHeader = TopLabel(window, "LootHeader", string.Empty, UIStyle.FontNormal, -225, 50);
        UIBuilder.AddStringKey(lootHeader, EStringKey.Result_LootHeader);

        // 주운 버섯 칸 영역
        RectTransform lootRoot = UIBuilder.Rect("LootRoot", window, Vector2.zero, Vector2.one, new Vector2(60, 200), new Vector2(-60, -290));
        UIBuilder.ListOption lootLayout = new(lootRoot, EUIListAxis.Grid, new Vector2(120, 120), new Vector2(48, 64), TextAnchor.UpperCenter);

        // 칸 원본은 UICommonSlot 프리팹(EUIComponentType.UICommonSlot). PopupResult가 UIListView로 꺼낸다
        TMP_Text empty = UIBuilder.Label(lootRoot.parent, EStringKey.Result_LootEmpty, UIStyle.FontNormal, TextAlignmentOptions.Center, "EmptyLabel");
        empty.rectTransform.offsetMin = new Vector2(60, 200);
        empty.rectTransform.offsetMax = new Vector2(-60, -290);
        empty.color = UIStyle.TextDim;

        // 아래 버튼
        Button retry = UIBuilder.Button(window, "RetryButton", EStringKey.Result_Retry, new Vector2(320, 110), UIStyle.PanelLight);
        PlaceBottom(retry, -190);

        Button lobby = UIBuilder.Button(window, "LobbyButton", EStringKey.Result_Lobby, new Vector2(320, 110));
        PlaceBottom(lobby, 190);

        PopupResult popup = root.gameObject.AddComponent<PopupResult>();
        UIBuilder.SetRef(popup, "m_Group", group);
        UIBuilder.SetRef(popup, "m_Window", window);
        UIBuilder.SetRef(popup, "m_Title", title);
        UIBuilder.SetRef(popup, "m_Summary", summary);
        UIBuilder.SetListView(popup, "m_SlotPool", lootLayout);
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
