using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 로비 씬(상단 재화 바, 하단 탭 4개, 토벌 탭)과 샘플 지역 데이터를 만드는 메뉴
public static class LobbyUISetup
{
    private const string ScenePath = "Assets/Scenes/" + SceneNames.Lobby + ".unity";
    private const string RegionDir = "Assets/Data/Regions";
    private const string MushroomDir = "Assets/Data/Mushrooms";

    private const float TopBarHeight = 110f;
    private const float TabBarHeight = 150f;

    // 하단 탭 순서(26.11.17 대장 확정, 파종 제외). 토벌이 기본 탭
    private static readonly string[] s_TabNames = { "야시장", "토벌", "개량", "파견" };
    private const int HuntTabIndex = 1;

    // ==================================================

    [MenuItem("MushTaker/UI/4. 로비 씬 만들기")]
    public static void CreateLobbyScene()
    {
        if (UIBuilder.Font == null)
        {
            CLogger.Warning("LobbyUISetup", "폰트 에셋이 없습니다. 1번 메뉴를 먼저 실행하세요.");
            return;
        }

        if (File.Exists(ScenePath) && EditorUtility.DisplayDialog("로비 씬", $"{ScenePath}가 이미 있습니다. 새로 만들까요?", "새로 만들기", "취소") == false)
            return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            return;

        CreateSampleRegions();
        BuildScene();
    }

    // ==================================================

    private static void CreateSampleRegions()
    {
        MushroomDef songi = AssetDatabase.LoadAssetAtPath<MushroomDef>($"{MushroomDir}/Mushroom_Songi.asset");
        MushroomDef cloud = AssetDatabase.LoadAssetAtPath<MushroomDef>($"{MushroomDir}/Mushroom_Cloud.asset");

        RegionDef r1 = CreateRegion("Region_01", "이끼 숲 어귀", "버끼 무리가 사는 얕은 숲. 송이가 흔하다.",
            1, null, new Vector2(0.2f, 0.3f), songi);
        RegionDef r2 = CreateRegion("Region_02", "구름 고개", "버곰이 길을 막는 고갯길. 단단한 구름버섯 갓을 깨야 한다.",
            2, r1, new Vector2(0.5f, 0.62f), songi, cloud);
        CreateRegion("Region_03", "말불 언덕", "부풀어 오른 버우르스가 포자를 뿜는 언덕.",
            3, r2, new Vector2(0.8f, 0.35f), cloud);

        AssetDatabase.SaveAssets();
        DataTools.RefreshDatabase(false);
    }

    // 이미 있으면 그대로 둔다(대장이 고친 값 보호)
    private static RegionDef CreateRegion(string id, string displayName, string description, int order, RegionDef require, Vector2 mapPosition, params MushroomDef[] mushrooms)
    {
        string path = $"{RegionDir}/{id}.asset";
        RegionDef exist = AssetDatabase.LoadAssetAtPath<RegionDef>(path);
        if (exist != null)
            return exist;

        Directory.CreateDirectory(RegionDir);

        RegionDef def = ScriptableObject.CreateInstance<RegionDef>();
        def.EditorSetup(order, require, mapPosition, mushrooms.Where(m => m != null).ToList());
        AssetDatabase.CreateAsset(def, path);

        SerializedObject so = new SerializedObject(def);
        so.FindProperty("m_ID").stringValue = id;
        so.FindProperty("m_DisplayName").stringValue = displayName;
        so.FindProperty("m_Description").stringValue = description;
        so.ApplyModifiedPropertiesWithoutUndo();

        return def;
    }

    private static void BuildScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

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
        safe.parent.gameObject.AddComponent<PopupManager>().EditorSetRoot(popupRoot);

        EditorSceneManager.SaveScene(scene, ScenePath);
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

        RectTransform nodeRt = UIBuilder.Anchored("NodeTemplate", nodeRoot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 110));
        nodeRt.pivot = new Vector2(0.5f, 0.5f);
        Image nodeImage = UIBuilder.AddImage(nodeRt, UIStyle.Accent, true);
        Button node = nodeRt.gameObject.AddComponent<Button>();
        node.targetGraphic = nodeImage;

        RectTransform nodeLabelRt = UIBuilder.Rect("Name", nodeRt, Vector2.zero, new Vector2(1, 0), new Vector2(-120, -50), new Vector2(120, -8));
        UIBuilder.Label(nodeLabelRt, "지역", UIStyle.FontSmall);

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
        UIBuilder.SetRef(hunt, "m_NodeTemplate", node);
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
            .Where(s => s.path != ScenePath)
            .ToList();

        // 빌드 첫 씬은 로비
        listScene.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = listScene.ToArray();
    }
}
