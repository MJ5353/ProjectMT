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
    private const float TabBarHeight = 150f;

    // 하단 탭 순서(26.11.17 대장 확정: 정비·토벌·개량·파견, 야시장 탭 폐지). 토벌이 기본 탭
    private static readonly string[] s_TabNames = { "정비", "토벌", "개량", "파견" };
    private static readonly string[] s_TabIcons = { "Tab_Gear", "Tab_Hunt", "Tab_Breed", "Tab_Dispatch" }; // s_TabNames 순서
    private static readonly EStringKey[] s_TabKeys = { EStringKey.Lobby_Tab_Gear, EStringKey.Lobby_Tab_Hunt, EStringKey.Lobby_Tab_Breed, EStringKey.Lobby_Tab_Dispatch }; // 탭 글자(StringDef). s_TabNames는 오브젝트 이름
    private const string GoldIconSprite = "Icon_Gold"; // 실행 중에는 UICountLabel이 재화 Def 아이콘으로 다시 채운다
    private const int HuntTabIndex = 1;

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

    // 카메라·EventSystem·캔버스(SafeArea·바탕) → SafeArea 아래 ScreenLobby 프리팹 → 그 위에 그려질 PopupRoot(나중 형제)
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

        RectTransform safe = UIBuilder.CreateCanvas("LobbyCanvas");
        UIBuilder.Panel("Background", safe.parent, UIStyle.Background).transform.SetAsFirstSibling();

        GameObject screenGo = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EditorPath.ScreenLobbyPrefab), safe) as GameObject;
        ScreenLobby screen = screenGo != null ? screenGo.GetComponent<ScreenLobby>() : null;
        if (screen == null)
        {
            // 화면 없는 씬을 저장하지 않는다(새 씬은 저장 전이라 버려진다)
            CLogger.Error("LobbyUISetup", $"{EditorPath.ScreenLobbyPrefab}에 ScreenLobby 프리팹이 없습니다. 모든 UI 다시 만들기를 먼저 실행하세요.");
            return;
        }

        RectTransform screenRt = screen.RectTransform;
        screenRt.anchorMin = Vector2.zero;
        screenRt.anchorMax = Vector2.one;
        screenRt.offsetMin = Vector2.zero;
        screenRt.offsetMax = Vector2.zero;

        RectTransform popupRoot = UIBuilder.Stretch("PopupRoot", safe);
        sceneController.EditorSetup(popupRoot, cam, screen);

        EditorSceneManager.SaveScene(scene, EditorPath.LobbyScene);
        AddToBuildSettings();
    }

    // 루트(Stretch, ScreenLobby) 아래 TopBar·Content(탭 뷰들)·TabBar. 팝업 루트는 넣지 않는다(씬에서 형제로 둔다)
    private static ScreenLobby BuildScreen()
    {
        RectTransform root = UIBuilder.Stretch(nameof(ScreenLobby), null);

        UICountLabel goldLabel = BuildTopBar(root);

        RectTransform content = UIBuilder.Rect("Content", root, Vector2.zero, Vector2.one, new Vector2(0, TabBarHeight), new Vector2(0, -TopBarHeight));

        List<UITabView> listView = new List<UITabView>();
        ScreenLobby_HuntView huntView = null;
        for (int i = 0; i < s_TabNames.Length; i++)
        {
            if (i == HuntTabIndex)
            {
                huntView = BuildHuntTab(content);
                listView.Add(huntView);
            }
            else
            {
                listView.Add(BuildComingSoonTab(content, s_TabNames[i], s_TabKeys[i]));
            }
        }

        UITabBar tabBar = BuildTabBar(root, listView);

        ScreenLobby screen = root.gameObject.AddComponent<ScreenLobby>();
        screen.EditorSetup(tabBar, huntView, goldLabel);
        return screen;
    }

    private static UICountLabel BuildTopBar(RectTransform parent)
    {
        RectTransform bar = UIBuilder.Rect("TopBar", parent, new Vector2(0, 1), Vector2.one, new Vector2(0, -TopBarHeight), Vector2.zero);
        UIBuilder.AddImage(bar, UIStyle.Panel);

        TMP_Text title = UIBuilder.Label(bar, EStringKey.Common_GameTitle, UIStyle.FontLarge, TextAlignmentOptions.Left, "Title");
        title.rectTransform.offsetMin = new Vector2(40, 0);

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
        for (int i = 0; i < s_TabNames.Length; i++)
        {
            RectTransform btnRt = UIBuilder.Rect($"Tab_{s_TabNames[i]}", bar, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image image = UIBuilder.AddImage(btnRt, UIStyle.PanelLight, true);

            Button button = btnRt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            // [mj] todo) 탭 아이콘 자리. 지금은 색 칸
            RectTransform iconRt = UIBuilder.Anchored("Icon", btnRt, new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(64, 64));
            Image icon = UIBuilder.AddImage(iconRt, Color.white);
            icon.sprite = UIBuilder.LoadSprite(s_TabIcons[i]);
            icon.preserveAspect = true;

            TMP_Text label = UIBuilder.Label(btnRt, s_TabKeys[i], UIStyle.FontNormal, TextAlignmentOptions.Bottom);
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

    // 지도 그림(256x144). 노드 자리(MapDef.MapPosition)에 맞춰 그려져 있어 노드 영역에 꽉 채운다. 이미 만든 로비 씬에도 쓴다
    public static void SetMapBackground(RectTransform nodeRoot)
    {
        if (nodeRoot.TryGetComponent(out Image image) == false)
            image = nodeRoot.gameObject.AddComponent<Image>();
        image.sprite = UIBuilder.LoadSprite("UI_Map_Background");
        image.color = Color.white;
        image.preserveAspect = false;
        image.raycastTarget = false;
    }

    // 노드 버튼에 지역 아이콘 칸과 ScreenLobby_HuntView_MapNode를 붙인다. 이미 만든 로비 씬에도 쓴다
    public static ScreenLobby_HuntView_MapNode AddMapNode(Button node, TMP_Text nodeName)
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
        RectTransform view = UIBuilder.Stretch("View_토벌", parent);

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

    // 정보 패널 위쪽에서 top만큼 내려온 왼쪽 정렬 라벨
    private static TMP_Text InfoLabel(Transform parent, string name, string text, int size, float top, float height)
    {
        RectTransform rt = UIBuilder.Rect(name, parent, new Vector2(0, 1), Vector2.one, new Vector2(30, top - height), new Vector2(-30, top));
        return UIBuilder.Label(rt, text, size, TextAlignmentOptions.Left);
    }

    private static void AddToBuildSettings()
    {
        List<EditorBuildSettingsScene> listScene = EditorBuildSettings.scenes
            .Where(s => s.path != EditorPath.LobbyScene)
            .ToList();

        // 빌드 첫 씬은 로비
        listScene.Insert(0, new EditorBuildSettingsScene(EditorPath.LobbyScene, true));
        EditorBuildSettings.scenes = listScene.ToArray();
    }
}
