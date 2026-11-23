using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 로비 화면(ScreenLobby) 프리팹을 만들고, 로비 씬을 재생성한다. 지역 데이터는 데이터 편집기에서 관리한다.
// 프리팹은 UISetup의 "모든 UI 다시 만들기"가 Prefabs 표로 만들고, 씬은 그 프리팹을 SafeArea 아래에 놓는다
public static class LobbyUISetup
{

    private const float TopBarHeight = 110f;
    private static readonly Vector2 QuitButtonSize = new Vector2(160, 72); // 윗줄 왼쪽 끝. 게임 이름은 그 오른쪽
    private const float TabBarHeight = 150f;

    // 하단 탭 순서(26.11.17 대장 확정: 정비·토벌·개량·파견, 야시장 탭 폐지). 토벌이 기본 탭.
    // Name은 오브젝트 이름, Icon은 스프라이트 이름, Key는 탭 글자(StringDef)
    private static readonly (string Name, string Icon, EStringKey Key)[] s_Tabs =
    {
        ("Gear", "Tab_Gear", EStringKey.Lobby_Tab_Gear),
        ("Hunt", "Tab_Hunt", EStringKey.Lobby_Tab_Hunt),
        ("Breed", "Tab_Breed", EStringKey.Lobby_Tab_Breed),
        ("Dispatch", "Tab_Dispatch", EStringKey.Lobby_Tab_Dispatch),
    };
    private const string GoldIconSprite = "Icon_Gold"; // 실행 중에는 UICountLabel이 재화 Def 아이콘으로 다시 채운다
    private const string TitleSprite = "UI_Title_Engrave_Line"; // 153x25 음각 한 줄 "버섯을 줍자!"
    private const float TitleScale = 3f;
    private const int GearTabIndex = 0;
    private const int HuntTabIndex = 1;
    private const float GearNodeSize = 84f;

    // ------------------------------------------------

    [MenuItem("MushTaker/초기 설정/로비 씬 재생성", false, 10)]
    public static void CreateLobbyScene()
    {
        if (UIBuilder.Font == null)
        {
            CLogger.Warning("LobbyUISetup", "MushTaker/초기 설정/UI 기반 준비를 먼저 실행하세요.");
            return;
        }

        if (File.Exists(EditorPath.LobbyScene) && EditorUtility.DisplayDialog("로비 씬", $"{EditorPath.LobbyScene}가 이미 있습니다. 새로 만들까요?", "새로 만들기", "취소") == false)
            return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            return;

        BuildScene();
    }

    // isOverwrite면 있어도 다시 만든다(같은 경로에 덮어써 GUID 유지)
    public static readonly (string Path, System.Func<GameObject> Build)[] Prefabs =
    {
        (EditorPath.ScreenLobbyPrefab, () => BuildScreen().gameObject),
    };

    // ==================================================

    // 카메라·EventSystem·캔버스 → ScreenLobby 프리팹 → 그 위에 그려질 PopupRoot(나중 형제). 구성은 UIBuilder.CreateCanvas 주석
    private static void BuildScene()
    {
        UISetup.BuildAllPrefabs(false); // 없는 프리팹만 만든다

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        LobbySceneController sceneController = new GameObject(nameof(LobbySceneController)).AddComponent<LobbySceneController>();

        // UI만 있는 씬이라 카메라는 바탕색만 칠한다
        Camera cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = UIStyle.Background;

        UIBuilder.CreateEventSystemIfNeeded();

        RectTransform canvas = UIBuilder.CreateCanvas("UICanvas");
        ScreenLobby screen = UIBuilder.PlaceScreen<ScreenLobby>(EditorPath.ScreenLobbyPrefab, canvas);
        if (screen == null)
        {
            // 화면 없는 씬을 저장하지 않는다(새 씬은 저장 전이라 버려진다)
            CLogger.Error("LobbyUISetup", $"{EditorPath.ScreenLobbyPrefab}에 ScreenLobby 프리팹이 없습니다. 모든 UI 다시 만들기를 먼저 실행하세요.");
            return;
        }

        RectTransform popupRoot = UIBuilder.CreateSafeArea(canvas, "PopupRoot");
        sceneController.EditorSetup(popupRoot, cam, screen);

        EditorSceneManager.SaveScene(scene, EditorPath.LobbyScene);
        AddToBuildSettings();
    }

    // 루트(꽉 참, 바탕색) 아래 SafeArea → TopBar·Content(탭 뷰들)·TabBar. 팝업 루트는 넣지 않는다(씬에서 형제로 둔다)
    private static ScreenLobby BuildScreen()
    {
        RectTransform root = UIBuilder.CreateScreenRoot(nameof(ScreenLobby), out RectTransform safe);
        UIBuilder.AddImage(root, UIStyle.Background); // 노치까지 덮는 바탕

        UICountLabel goldLabel = BuildTopBar(safe, out Button quitButton);

        RectTransform content = UIBuilder.Rect("Content", safe, Vector2.zero, Vector2.one, new Vector2(0, TabBarHeight), new Vector2(0, -TopBarHeight));

        List<UITabView> listView = new List<UITabView>();
        ScreenLobby_HuntView huntView = null;
        for (int i = 0; i < s_Tabs.Length; i++)
        {
            if (i == HuntTabIndex)
            {
                huntView = BuildHuntTab(content);
                listView.Add(huntView);
            }
            else if (i == GearTabIndex)
            {
                listView.Add(BuildGearTab(content));
            }
            else
            {
                listView.Add(BuildComingSoonTab(content, s_Tabs[i].Name, s_Tabs[i].Key));
            }
        }

        UITabBar tabBar = BuildTabBar(safe, listView);

        ScreenLobby screen = root.gameObject.AddComponent<ScreenLobby>();
        screen.EditorSetup(tabBar, huntView, goldLabel, quitButton);
        return screen;
    }

    private static UICountLabel BuildTopBar(RectTransform parent, out Button quitButton)
    {
        RectTransform bar = UIBuilder.Rect("TopBar", parent, new Vector2(0, 1), Vector2.one, new Vector2(0, -TopBarHeight), Vector2.zero);
        UIBuilder.AddImage(bar, UIStyle.Panel);

        // 종료: 왼쪽 끝, 게임 이름은 그 오른쪽
        quitButton = UIBuilder.Button(bar, "QuitButton", EStringKey.Common_Quit, QuitButtonSize);
        RectTransform quitRt = quitButton.transform as RectTransform;
        quitRt.anchorMin = quitRt.anchorMax = quitRt.pivot = new Vector2(0, 0.5f);
        quitRt.anchoredPosition = new Vector2(40, 0);

        // 게임 이름: 음각 한 줄 그림. 그림이 없으면 빈 칸(LoadSprite가 경고)
        Sprite titleSprite = UIBuilder.LoadSprite(TitleSprite);
        Vector2 titleSize = titleSprite != null ? titleSprite.rect.size * TitleScale : Vector2.zero;
        RectTransform titleRt = UIBuilder.Anchored("Title", bar, new Vector2(0, 0.5f), new Vector2(40 + QuitButtonSize.x + 30, 0), titleSize);
        UIBuilder.AddImage(titleRt, Color.white).sprite = titleSprite;

        // 재화: 오른쪽 위. 늘어나면 칸을 복사해 ID만 바꾼다
        RectTransform gold = UIBuilder.Anchored("Gold", bar, new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(320, 72));
        UIBuilder.AddImage(gold, UIStyle.Dim);

        RectTransform goldIconRt = UIBuilder.Anchored("Icon", gold, new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(44, 44));
        Image goldIcon = UIBuilder.AddImage(goldIconRt, Color.white);
        goldIcon.sprite = UIBuilder.LoadSprite(GoldIconSprite);
        goldIcon.preserveAspect = true;

        TMP_Text goldText = UIBuilder.Label(gold, "0", UIStyle.FontNormal, TextAlignmentOptions.Right, "Count");
        goldText.rectTransform.offsetMax = new Vector2(-20, 0);
        UICountLabel goldLabel = goldText.gameObject.AddComponent<UICountLabel>();
        goldLabel.EditorSetup(goldText, goldIcon, CurrencyDef.GoldID, "{0}");
        return goldLabel;
    }

    private static UITabBar BuildTabBar(RectTransform parent, List<UITabView> listView)
    {
        RectTransform bar = UIBuilder.Rect("TabBar", parent, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, TabBarHeight));
        UIBuilder.AddImage(bar, UIStyle.Panel);

        HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.spacing = 6;
        layout.padding = new RectOffset(6, 6, 6, 0);

        List<UITabBar.Entry> listEntry = new List<UITabBar.Entry>();
        for (int i = 0; i < s_Tabs.Length; i++)
        {
            RectTransform btnRt = UIBuilder.Rect($"Tab_{s_Tabs[i].Name}", bar, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image image = UIBuilder.AddImage(btnRt, UIStyle.PanelLight, true);

            Button button = btnRt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            // [mj] todo) 탭 아이콘 자리. 지금은 색 칸
            RectTransform iconRt = UIBuilder.Anchored("Icon", btnRt, new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(64, 64));
            Image icon = UIBuilder.AddImage(iconRt, Color.white);
            icon.sprite = UIBuilder.LoadSprite(s_Tabs[i].Icon);
            icon.preserveAspect = true;

            TMP_Text label = UIBuilder.Label(btnRt, s_Tabs[i].Key, UIStyle.FontNormal, TextAlignmentOptions.Bottom);
            label.rectTransform.offsetMin = new Vector2(0, 14);

            RectTransform highlight = UIBuilder.Rect("Highlight", btnRt, new Vector2(0, 1), Vector2.one, new Vector2(0, -8), Vector2.zero);
            UIBuilder.AddImage(highlight, UIStyle.Gold);

            listEntry.Add(new UITabBar.Entry(button, listView[i], highlight.gameObject));
        }

        UITabBar uiTabBar = bar.gameObject.AddComponent<UITabBar>();
        uiTabBar.EditorSetTabs(listEntry);

        SerializedObject so = new SerializedObject(uiTabBar);
        so.FindProperty("m_DefaultIndex").intValue = HuntTabIndex;
        so.ApplyModifiedPropertiesWithoutUndo();
        return uiTabBar;
    }

    // 탭 이름(큰 글자) 아래 "준비 중"(작은 글자). 두 줄 모두 StringDef 문구
    private static UITabView BuildComingSoonTab(RectTransform parent, string tabName, EStringKey tabKey)
    {
        RectTransform view = UIBuilder.Stretch($"View_{tabName}", parent);

        TMP_Text title = UIBuilder.Label(view, tabKey, UIStyle.FontTitle, TextAlignmentOptions.Center, "Title");
        title.rectTransform.offsetMin = new Vector2(0, 50);
        title.color = UIStyle.TextDim;

        TMP_Text sub = UIBuilder.Label(view, EStringKey.Lobby_ComingSoon, UIStyle.FontNormal, TextAlignmentOptions.Center, "Sub");
        sub.rectTransform.offsetMax = new Vector2(0, -70);
        sub.color = UIStyle.TextDim;

        return view.gameObject.AddComponent<UITabView>();
    }

    // 지도 그림(256x144). 노드 자리(MapDef.MapPosition)에 맞춰 그려져 있어 노드 영역에 꽉 채운다
    private static void SetMapBackground(RectTransform nodeRoot)
    {
        if (nodeRoot.TryGetComponent(out Image image) == false)
            image = nodeRoot.gameObject.AddComponent<Image>();
        image.sprite = UIBuilder.LoadSprite("UI_Map_Background");
        image.color = Color.white;
        image.preserveAspect = false;
        image.raycastTarget = false;
    }

    // 노드 버튼에 지역 아이콘 칸과 ScreenLobby_HuntView_MapNode를 붙인다
    private static ScreenLobby_HuntView_MapNode AddMapNode(Button node, TMP_Text nodeName)
    {
        RectTransform iconRt = UIBuilder.Stretch("Icon", node.transform);
        iconRt.SetSiblingIndex(0);
        Image icon = UIBuilder.AddImage(iconRt, Color.white);
        icon.preserveAspect = true;

        ScreenLobby_HuntView_MapNode mapNode = node.gameObject.AddComponent<ScreenLobby_HuntView_MapNode>();
        UIBuilder.SetRef(mapNode, "m_Button", node);
        UIBuilder.SetRef(mapNode, "m_Icon", icon);
        UIBuilder.SetRef(mapNode, "m_NameLabel", nodeName); // !!) m_Name은 Object 이름과 겹쳐 직렬화가 깨진다
        return mapNode;
    }

    private static ScreenLobby_HuntView BuildHuntTab(RectTransform parent)
    {
        RectTransform view = UIBuilder.Stretch($"View_{s_Tabs[HuntTabIndex].Name}", parent);

        // 왼쪽: 지도
        RectTransform map = UIBuilder.Rect("Map", view, Vector2.zero, new Vector2(0.62f, 1f), new Vector2(30, 30), new Vector2(-15, -30));
        UIBuilder.AddImage(map, UIStyle.Panel);

        TMP_Text mapTitle = UIBuilder.Label(map, EStringKey.Lobby_Hunt_MapTitle, UIStyle.FontNormal, TextAlignmentOptions.TopLeft, "Title");
        mapTitle.rectTransform.offsetMin = new Vector2(24, 0);
        mapTitle.rectTransform.offsetMax = new Vector2(0, -18);
        mapTitle.color = UIStyle.TextDim;

        RectTransform nodeRoot = UIBuilder.Stretch("Nodes", map, 60f);
        SetMapBackground(nodeRoot);

        RectTransform nodeRt = UIBuilder.Anchored("NodeTemplate", nodeRoot, new Vector2(0.5f, 0.5f), new Vector2(0, 15), new Vector2(200, 200)); // 위치(0, 15)는 지도 자리에서 띄우는 거리
        nodeRt.pivot = new Vector2(0.5f, 0.5f);
        Image nodeImage = UIBuilder.AddImage(nodeRt, Color.clear, true); // 바탕 없이 누르는 영역만
        Button node = nodeRt.gameObject.AddComponent<Button>();
        node.targetGraphic = nodeImage;

        RectTransform nodeLabelRt = UIBuilder.Rect("Name", nodeRt, Vector2.zero, new Vector2(1, 0), new Vector2(-20, -56), new Vector2(20, -8));
        UIBuilder.AddImage(nodeLabelRt, new Color(0f, 0f, 0f, 0.6f)); // 지도 그림 위에서 이름이 보이게
        TMP_Text nodeName = UIBuilder.Label(nodeLabelRt, "지역", 28);
        ScreenLobby_HuntView_MapNode mapNode = AddMapNode(node, nodeName);

        // 오른쪽: 지역 정보
        RectTransform info = UIBuilder.Rect("Info", view, new Vector2(0.62f, 0f), Vector2.one, new Vector2(15, 30), new Vector2(-30, -30));
        UIBuilder.AddImage(info, UIStyle.Panel);

        TMP_Text infoName = InfoLabel(info, "Name", "지역 이름", UIStyle.FontLarge, -30, 60);
        TMP_Text infoState = InfoLabel(info, "State", "토벌 가능", UIStyle.FontSmall, -95, 34);
        TMP_Text infoDesc = InfoLabel(info, "Desc", "설명", UIStyle.FontSmall, -145, 140);
        infoDesc.textWrappingMode = TextWrappingModes.Normal;
        infoDesc.alignment = TextAlignmentOptions.TopLeft;
        infoDesc.color = UIStyle.TextDim;

        TMP_Text mushHeader = InfoLabel(info, "MushroomHeader", string.Empty, UIStyle.FontSmall, -300, 34);
        UIBuilder.AddStringKey(mushHeader, EStringKey.Lobby_Hunt_MushroomHeader);
        mushHeader.color = UIStyle.TextDim;

        RectTransform mushRoot = UIBuilder.Rect("Mushrooms", info, new Vector2(0, 1), Vector2.one, new Vector2(30, -480), new Vector2(-30, -345));
        UIBuilder.ListOption mushLayout = new(mushRoot, EUIListAxis.Horizontal, new Vector2(96, 96), new Vector2(30, 0), TextAnchor.UpperLeft);

        // 칸은 UICommonSlot 프리팹(EUIComponentType.UICommonSlot)을 ScreenLobby_HuntView가 UIListView로 꺼낸다
        UITooltip mushTooltip = UIBuilder.Tooltip(info, "MushroomTooltip"); // 목록 뒤에 만들어 칸 위에 그린다

        Button enter = UIBuilder.Button(info, "EnterButton", EStringKey.Lobby_Hunt_Enter, new Vector2(360, 120));
        RectTransform enterRt = enter.transform as RectTransform;
        if (enterRt != null)
        {
            enterRt.anchorMin = new Vector2(0.5f, 0f);
            enterRt.anchorMax = new Vector2(0.5f, 0f);
            enterRt.pivot = new Vector2(0.5f, 0f);
            enterRt.anchoredPosition = new Vector2(0, 40);
        }

        ScreenLobby_HuntView hunt = view.gameObject.AddComponent<ScreenLobby_HuntView>();
        UIBuilder.SetRef(hunt, "m_MapRoot", nodeRoot);
        UIBuilder.SetRef(hunt, "m_NodeTemplate", mapNode);
        UIBuilder.SetRef(hunt, "m_InfoName", infoName);
        UIBuilder.SetRef(hunt, "m_InfoState", infoState);
        UIBuilder.SetRef(hunt, "m_InfoDesc", infoDesc);
        UIBuilder.SetListView(hunt, "m_MushroomPool", mushLayout);
        UIBuilder.SetRef(hunt, "m_MushroomTooltip", mushTooltip);
        UIBuilder.SetRef(hunt, "m_EnterButton", enter);

        return hunt;
    }

    // 정비 탭: 왼쪽 엠블렘 · 가운데 강화 노드 트리(노드·선은 템플릿, 실행 중 GearNodeDef마다 복제) · 오른쪽 노드 정보
    private static ScreenLobby_GearView BuildGearTab(RectTransform parent)
    {
        RectTransform view = UIBuilder.Stretch($"View_{s_Tabs[GearTabIndex].Name}", parent);

        // 왼쪽: 엠블렘
        RectTransform emblem = UIBuilder.Rect("Emblem", view, Vector2.zero, new Vector2(0.24f, 1f), new Vector2(30, 30), new Vector2(-15, -30));
        UIBuilder.AddImage(emblem, UIStyle.Panel);

        RectTransform emblemImageRt = UIBuilder.Anchored("EmblemImage", emblem, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(240, 240));
        Image emblemImage = UIBuilder.AddImage(emblemImageRt, Color.white); // 슬롯 없이 뱃지 그림만. 실행 중 EmblemDef 아이콘으로 바뀌고, 그림이 없으면 숨긴다
        emblemImage.preserveAspect = true;

        TMP_Text emblemName = InfoLabel(emblem, "EmblemName", "엠블렘", UIStyle.FontLarge, -300, 60);
        emblemName.alignment = TextAlignmentOptions.Center;

        TMP_Text emblemProgress = InfoLabel(emblem, "EmblemProgress", "강화 0 / 0", UIStyle.FontSmall, -370, 70);
        emblemProgress.alignment = TextAlignmentOptions.Center;
        emblemProgress.color = UIStyle.TextDim;

        TMP_Text bonusHeader = InfoLabel(emblem, "BonusHeader", string.Empty, UIStyle.FontSmall, -470, 34);
        UIBuilder.AddStringKey(bonusHeader, EStringKey.Lobby_Gear_EmblemBonusHeader);
        bonusHeader.alignment = TextAlignmentOptions.Center;
        bonusHeader.color = UIStyle.Gold;

        TMP_Text emblemBonus = InfoLabel(emblem, "EmblemBonus", "없음", UIStyle.FontSmall, -515, 200);
        emblemBonus.alignment = TextAlignmentOptions.Top;
        emblemBonus.textWrappingMode = TextWrappingModes.Normal;

        // 가운데: 트리
        RectTransform tree = UIBuilder.Rect("Tree", view, new Vector2(0.24f, 0f), new Vector2(0.74f, 1f), new Vector2(15, 30), new Vector2(-15, -30));
        UIBuilder.AddImage(tree, UIStyle.Panel);

        TMP_Text treeTitle = UIBuilder.Label(tree, EStringKey.Lobby_Gear_TreeTitle, UIStyle.FontNormal, TextAlignmentOptions.TopLeft, "Title");
        treeTitle.rectTransform.offsetMin = new Vector2(24, 0);
        treeTitle.rectTransform.offsetMax = new Vector2(0, -18);
        treeTitle.color = UIStyle.TextDim;

        RectTransform noticeRt = UIBuilder.Rect("EquipNotice", tree, Vector2.zero, new Vector2(1, 0), new Vector2(20, 12), new Vector2(-20, 52));
        TMP_Text notice = UIBuilder.Label(noticeRt, EStringKey.Lobby_Gear_EquipComingSoon, UIStyle.FontSmall);
        notice.color = UIStyle.TextDim;

        // 노드 칸 영역. 노드 중심이 영역 끝에 놓이므로 노드 반 크기만큼 안쪽으로 둔다
        float inset = GearNodeSize * 0.5f + 20f;
        RectTransform nodeRoot = UIBuilder.Rect("Nodes", tree, Vector2.zero, Vector2.one, new Vector2(inset, 60 + inset), new Vector2(-inset, -70 - inset));

        RectTransform lineRt = UIBuilder.Anchored("LineTemplate", nodeRoot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 6));
        Image line = UIBuilder.AddImage(lineRt, UIStyle.PanelLight);

        ScreenLobby_GearView_Node node = BuildGearNode(nodeRoot);

        // 오른쪽: 노드 정보
        RectTransform detail = UIBuilder.Rect("Detail", view, new Vector2(0.74f, 0f), Vector2.one, new Vector2(15, 30), new Vector2(-30, -30));
        UIBuilder.AddImage(detail, UIStyle.Panel);

        TMP_Text detailName = InfoLabel(detail, "Name", "노드 이름", UIStyle.FontLarge, -30, 60);
        TMP_Text detailEffect = InfoLabel(detail, "Effect", "효과", UIStyle.FontSmall, -110, 220);
        detailEffect.textWrappingMode = TextWrappingModes.Normal;
        detailEffect.alignment = TextAlignmentOptions.TopLeft;

        TMP_Text detailRequire = InfoLabel(detail, "Require", "조건", UIStyle.FontSmall, -345, 100);
        detailRequire.textWrappingMode = TextWrappingModes.Normal;
        detailRequire.alignment = TextAlignmentOptions.TopLeft;
        detailRequire.color = UIStyle.TextDim;

        TMP_Text detailCost = InfoLabel(detail, "Cost", "0G", UIStyle.FontNormal, -460, 50);
        detailCost.color = UIStyle.Gold;

        Button open = UIBuilder.Button(detail, "OpenButton", "강화", new Vector2(320, 110));
        RectTransform openRt = open.transform as RectTransform;
        if (openRt != null)
        {
            openRt.anchorMin = new Vector2(0.5f, 0f);
            openRt.anchorMax = new Vector2(0.5f, 0f);
            openRt.pivot = new Vector2(0.5f, 0f);
            openRt.anchoredPosition = new Vector2(0, 40);
        }

        ScreenLobby_GearView gear = view.gameObject.AddComponent<ScreenLobby_GearView>();
        UIBuilder.SetRef(gear, "m_EmblemImage", emblemImage);
        UIBuilder.SetRef(gear, "m_EmblemName", emblemName);
        UIBuilder.SetRef(gear, "m_EmblemProgress", emblemProgress);
        UIBuilder.SetRef(gear, "m_EmblemBonus", emblemBonus);
        UIBuilder.SetRef(gear, "m_NodeRoot", nodeRoot);
        UIBuilder.SetRef(gear, "m_NodeTemplate", node);
        UIBuilder.SetRef(gear, "m_LineTemplate", line);
        UIBuilder.SetRef(gear, "m_DetailName", detailName);
        UIBuilder.SetRef(gear, "m_DetailEffect", detailEffect);
        UIBuilder.SetRef(gear, "m_DetailRequire", detailRequire);
        UIBuilder.SetRef(gear, "m_DetailCost", detailCost);
        UIBuilder.SetRef(gear, "m_OpenButton", open);
        UIBuilder.SetRef(gear, "m_OpenLabel", open.GetComponentInChildren<TMP_Text>());

        return gear;
    }

    // 노드 템플릿: 고름 테두리(흰) → 스페셜 테두리(금) → 바탕(버튼) → 아이콘 → 이름
    private static ScreenLobby_GearView_Node BuildGearNode(RectTransform parent)
    {
        RectTransform rt = UIBuilder.Anchored("NodeTemplate", parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(GearNodeSize, GearNodeSize));

        RectTransform selectRt = UIBuilder.Stretch("Select", rt, -10f);
        UIBuilder.AddImage(selectRt, UIStyle.Text);

        RectTransform frameRt = UIBuilder.Stretch("Frame", rt, -5f);
        Image frame = UIBuilder.AddImage(frameRt, UIStyle.Gold);

        RectTransform bgRt = UIBuilder.Stretch("Bg", rt);
        Image bg = UIBuilder.AddImage(bgRt, UIStyle.PanelLight, true);
        Button button = bgRt.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;

        RectTransform iconRt = UIBuilder.Stretch("Icon", rt, 14f);
        Image icon = UIBuilder.AddImage(iconRt, new Color(1f, 1f, 1f, 0.35f)); // 아이콘이 있으면 이름 뒤에 흐리게
        icon.preserveAspect = true;

        TMP_Text nameLabel = UIBuilder.Label(rt, "노드", UIStyle.FontSmall);
        nameLabel.textWrappingMode = TextWrappingModes.Normal;
        nameLabel.rectTransform.offsetMin = new Vector2(4, 4);
        nameLabel.rectTransform.offsetMax = new Vector2(-4, -4);

        ScreenLobby_GearView_Node node = rt.gameObject.AddComponent<ScreenLobby_GearView_Node>();
        UIBuilder.SetRef(node, "m_Button", button);
        UIBuilder.SetRef(node, "m_Bg", bg);
        UIBuilder.SetRef(node, "m_Icon", icon);
        UIBuilder.SetRef(node, "m_Frame", frame);
        UIBuilder.SetRef(node, "m_Select", selectRt.gameObject);
        UIBuilder.SetRef(node, "m_NameLabel", nameLabel); // !!) m_Name은 Object 이름과 겹쳐 직렬화가 깨진다
        return node;
    }

    // 정보 패널 위쪽에서 top만큼 내려온 왼쪽 정렬 라벨
    private static TMP_Text InfoLabel(Transform parent, string name, string text, int size, float top, float height)
    {
        RectTransform rt = UIBuilder.Rect(name, parent, new Vector2(0, 1), Vector2.one, new Vector2(30, top - height), new Vector2(-30, top));
        return UIBuilder.Label(rt, text, size, TextAlignmentOptions.Left);
    }

    private static void AddToBuildSettings()
    {
        // 빌드 첫 씬은 타이틀(TitleUISetup). 로비는 없을 때만 뒤에 붙인다
        List<EditorBuildSettingsScene> listScene = EditorBuildSettings.scenes.ToList();
        if (listScene.Any(s => s.path == EditorPath.LobbyScene))
            return;

        listScene.Add(new EditorBuildSettingsScene(EditorPath.LobbyScene, true));
        EditorBuildSettings.scenes = listScene.ToArray();
    }
}
