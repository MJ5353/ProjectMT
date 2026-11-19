using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 로비 씬 UI를 재생성한다. 지역 데이터는 데이터 편집기에서 관리한다.
public static class LobbyUISetup
{

    private const float TopBarHeight = 110f;
    private const float TabBarHeight = 150f;

    // 하단 탭 순서(26.11.17 대장 확정, 파종 제외). 토벌이 기본 탭
    private static readonly string[] s_TabNames = { "야시장", "토벌", "개량", "파견" };
    private const int HuntTabIndex = 1;

    // ------------------------------------------------

    [MenuItem("MushTaker/초기 설정/로비 씬 재생성", false, 101)]
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

    // ==================================================

    private static void BuildScene()
    {
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

        BuildTopBar(safe);

        RectTransform content = UIBuilder.Rect("Content", safe, Vector2.zero, Vector2.one, new Vector2(0, TabBarHeight), new Vector2(0, -TopBarHeight));

        List<TabView> listView = new List<TabView>();
        for (int i = 0; i < s_TabNames.Length; i++)
        {
            TabView view = i == HuntTabIndex ? BuildHuntTab(content) : BuildComingSoonTab(content, s_TabNames[i]);
            listView.Add(view);
        }

        BuildTabBar(safe, listView);

        RectTransform popupRoot = UIBuilder.Stretch("PopupRoot", safe);
        sceneController.EditorSetup(popupRoot, cam);

        EditorSceneManager.SaveScene(scene, EditorPath.LobbyScene);
        AddToBuildSettings();
    }

    private static void BuildTopBar(RectTransform parent)
    {
        RectTransform bar = UIBuilder.Rect("TopBar", parent, new Vector2(0, 1), Vector2.one, new Vector2(0, -TopBarHeight), Vector2.zero);
        UIBuilder.AddImage(bar, UIStyle.Panel);

        TMP_Text title = UIBuilder.Label(bar, "MushTaker", UIStyle.FontLarge, TextAlignmentOptions.Left, "Title");
        title.rectTransform.offsetMin = new Vector2(40, 0);

        // 재화: 오른쪽 위. 늘어나면 칸을 복사해 ID만 바꾼다
        RectTransform gold = UIBuilder.Anchored("Gold", bar, new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(320, 72));
        UIBuilder.AddImage(gold, UIStyle.Dim);

        RectTransform goldIcon = UIBuilder.Anchored("Icon", gold, new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(44, 44));
        UIBuilder.AddImage(goldIcon, UIStyle.Gold);

        TMP_Text goldText = UIBuilder.Label(gold, "0", UIStyle.FontNormal, TextAlignmentOptions.Right, "Count");
        goldText.rectTransform.offsetMax = new Vector2(-20, 0);
        goldText.gameObject.AddComponent<UICountLabel>().EditorSetup("Currency_Gold", "{0}");
    }

    private static void BuildTabBar(RectTransform parent, List<TabView> listView)
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

        List<TabBar.Entry> listEntry = new List<TabBar.Entry>();
        for (int i = 0; i < s_TabNames.Length; i++)
        {
            RectTransform btnRt = UIBuilder.Rect($"Tab_{s_TabNames[i]}", bar, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image image = UIBuilder.AddImage(btnRt, UIStyle.PanelLight, true);

            Button button = btnRt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            // [mj] todo) 탭 아이콘 자리. 지금은 색 칸
            RectTransform icon = UIBuilder.Anchored("Icon", btnRt, new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(64, 64));
            UIBuilder.AddImage(icon, UIStyle.TextDim);

            TMP_Text label = UIBuilder.Label(btnRt, s_TabNames[i], UIStyle.FontNormal, TextAlignmentOptions.Bottom);
            label.rectTransform.offsetMin = new Vector2(0, 14);

            RectTransform highlight = UIBuilder.Rect("Highlight", btnRt, new Vector2(0, 1), Vector2.one, new Vector2(0, -8), Vector2.zero);
            UIBuilder.AddImage(highlight, UIStyle.Gold);

            listEntry.Add(new TabBar.Entry(button, listView[i], highlight.gameObject));
        }

        TabBar tabBar = bar.gameObject.AddComponent<TabBar>();
        tabBar.EditorSetTabs(listEntry);

        SerializedObject so = new SerializedObject(tabBar);
        so.FindProperty("m_DefaultIndex").intValue = HuntTabIndex;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TabView BuildComingSoonTab(RectTransform parent, string tabName)
    {
        RectTransform view = UIBuilder.Stretch($"View_{tabName}", parent);
        TMP_Text label = UIBuilder.Label(view, $"{tabName}\n<size={UIStyle.FontNormal}>준비 중</size>", UIStyle.FontTitle);
        label.color = UIStyle.TextDim;

        return view.gameObject.AddComponent<TabView>();
    }

    // 지도 그림(256x144). 노드 자리(MapDef.MapPosition)에 맞춰 그려져 있어 노드 영역에 꽉 채운다. 이미 만든 로비 씬에도 쓴다
    public static void SetMapBackground(RectTransform nodeRoot)
    {
        if (nodeRoot.TryGetComponent(out Image image) == false)
            image = nodeRoot.gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(EditorPath.UISpriteDir + "UI_Map_Background.png");
        image.color = Color.white;
        image.preserveAspect = false;
        image.raycastTarget = false;
    }

    // 노드 버튼에 지역 아이콘 칸과 UIMapNode를 붙인다. 이미 만든 로비 씬에도 쓴다
    public static UIMapNode AddMapNode(Button node, TMP_Text nodeName)
    {
        RectTransform iconRt = UIBuilder.Stretch("Icon", node.transform);
        iconRt.SetSiblingIndex(0);
        Image icon = UIBuilder.AddImage(iconRt, Color.white);
        icon.preserveAspect = true;

        UIMapNode mapNode = node.gameObject.AddComponent<UIMapNode>();
        UIBuilder.SetRef(mapNode, "m_Button", node);
        UIBuilder.SetRef(mapNode, "m_Icon", icon);
        UIBuilder.SetRef(mapNode, "m_NameLabel", nodeName); // !!) m_Name은 Object 이름과 겹쳐 직렬화가 깨진다
        return mapNode;
    }

    private static TabView BuildHuntTab(RectTransform parent)
    {
        RectTransform view = UIBuilder.Stretch("View_토벌", parent);

        // 왼쪽: 지도
        RectTransform map = UIBuilder.Rect("Map", view, Vector2.zero, new Vector2(0.62f, 1f), new Vector2(30, 30), new Vector2(-15, -30));
        UIBuilder.AddImage(map, UIStyle.Panel);

        TMP_Text mapTitle = UIBuilder.Label(map, "토벌 지도", UIStyle.FontNormal, TextAlignmentOptions.TopLeft, "Title");
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
        UIMapNode mapNode = AddMapNode(node, nodeName);

        // 오른쪽: 지역 정보
        RectTransform info = UIBuilder.Rect("Info", view, new Vector2(0.62f, 0f), Vector2.one, new Vector2(15, 30), new Vector2(-30, -30));
        UIBuilder.AddImage(info, UIStyle.Panel);

        TMP_Text infoName = InfoLabel(info, "Name", "지역 이름", UIStyle.FontLarge, -30, 60);
        TMP_Text infoState = InfoLabel(info, "State", "토벌 가능", UIStyle.FontSmall, -95, 34);
        TMP_Text infoDesc = InfoLabel(info, "Desc", "설명", UIStyle.FontSmall, -145, 140);
        infoDesc.textWrappingMode = TextWrappingModes.Normal;
        infoDesc.alignment = TextAlignmentOptions.TopLeft;
        infoDesc.color = UIStyle.TextDim;

        TMP_Text mushHeader = InfoLabel(info, "MushroomHeader", "나오는 버섯", UIStyle.FontSmall, -300, 34);
        mushHeader.color = UIStyle.TextDim;

        RectTransform mushRoot = UIBuilder.Rect("Mushrooms", info, new Vector2(0, 1), Vector2.one, new Vector2(30, -480), new Vector2(-30, -345));
        HorizontalLayoutGroup mushLayout = mushRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        mushLayout.childControlWidth = false;
        mushLayout.childControlHeight = false;
        mushLayout.childForceExpandWidth = false;
        mushLayout.childForceExpandHeight = false;
        mushLayout.spacing = 30;
        mushLayout.childAlignment = TextAnchor.UpperLeft;

        UIItemSlot mushTemplate = UIBuilder.ItemSlot(mushRoot, "MushroomTemplate", 96f);

        Button enter = UIBuilder.Button(info, "EnterButton", "입장", new Vector2(360, 120));
        RectTransform enterRt = (RectTransform)enter.transform;
        enterRt.anchorMin = new Vector2(0.5f, 0f);
        enterRt.anchorMax = new Vector2(0.5f, 0f);
        enterRt.pivot = new Vector2(0.5f, 0f);
        enterRt.anchoredPosition = new Vector2(0, 40);

        HuntTabView hunt = view.gameObject.AddComponent<HuntTabView>();
        UIBuilder.SetRef(hunt, "m_MapRoot", nodeRoot);
        UIBuilder.SetRef(hunt, "m_NodeTemplate", mapNode);
        UIBuilder.SetRef(hunt, "m_InfoName", infoName);
        UIBuilder.SetRef(hunt, "m_InfoState", infoState);
        UIBuilder.SetRef(hunt, "m_InfoDesc", infoDesc);
        UIBuilder.SetRef(hunt, "m_MushroomRoot", mushRoot);
        UIBuilder.SetRef(hunt, "m_MushroomTemplate", mushTemplate);
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
