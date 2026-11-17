using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 기반 씬(Lobby, Battle)과 샘플 데이터를 만들어 주는 도구. 이미 있는 파일은 덮어쓰지 않는다
public static class ProjectSetup
{
    private const string ScenesDir = "Assets/Scenes";
    private const string LobbyPath = ScenesDir + "/Lobby.unity";
    private const string BattlePath = ScenesDir + "/Battle.unity";
    private const int TabCount = 4;

    [MenuItem("MushTaker/Setup/샘플 데이터 만들기")]
    public static void CreateSampleData()
    {
        CreateDef<CurrencyDef>("Assets/Data/Currencies", "Currency_Gold", "골드");
        CreateDef<MushroomDef>("Assets/Data/Mushrooms", "Mushroom_Songi", "송이");
        CreateDef<MushroomDef>("Assets/Data/Mushrooms", "Mushroom_FlyAgaric", "광대버섯");

        AssetDatabase.SaveAssets();
        DataTools.RefreshDatabase(true);
    }

    [MenuItem("MushTaker/Setup/기본 씬 만들기")]
    public static void CreateScenes()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            return;

        Directory.CreateDirectory(ScenesDir);

        if (File.Exists(LobbyPath))
            Debug.LogWarning($"[ProjectSetup] {LobbyPath}가 이미 있어 건너뜁니다.");
        else
            CreateLobby();

        if (File.Exists(BattlePath))
            Debug.LogWarning($"[ProjectSetup] {BattlePath}가 이미 있어 건너뜁니다.");
        else
            CreateBattle();

        AddToBuildSettings();
        EditorSceneManager.OpenScene(LobbyPath);
    }

    private static void CreateDef<T>(string dir, string id, string displayName) where T : DataDef
    {
        string path = $"{dir}/{id}.asset";
        if (File.Exists(path))
            return;

        Directory.CreateDirectory(dir);

        T def = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(def, path);

        SerializedObject so = new SerializedObject(def);
        so.FindProperty("m_ID").stringValue = id;
        so.FindProperty("m_DisplayName").stringValue = displayName;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateLobby()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        CreateEventSystem();

        RectTransform canvas = CreateCanvas();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 위: 탭 화면 영역, 아래: 탭바(높이 200)
        RectTransform content = Rect("Content", canvas, Vector2.zero, Vector2.one, new Vector2(0, 200), Vector2.zero);
        RectTransform bar = Rect("TabBar", canvas, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 200));
        bar.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f);

        HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        List<TabBar.Entry> listEntry = new List<TabBar.Entry>();
        for (int i = 0; i < TabCount; i++)
        {
            RectTransform view = Rect($"TabView_{i + 1}", content, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            view.gameObject.AddComponent<Image>().color = Color.HSVToRGB(i / (float)TabCount, 0.25f, 0.35f);
            Label(view, $"TAB {i + 1}", font, 64);

            RectTransform btnRt = new GameObject($"TabButton_{i + 1}", typeof(RectTransform)).GetComponent<RectTransform>();
            btnRt.SetParent(bar, false);

            Image btnImage = btnRt.gameObject.AddComponent<Image>();
            btnImage.color = new Color(0.18f, 0.18f, 0.2f);

            Button button = btnRt.gameObject.AddComponent<Button>();
            button.targetGraphic = btnImage;
            Label(btnRt, $"Tab {i + 1}", font, 40);

            RectTransform highlight = Rect("Highlight", btnRt, new Vector2(0, 1), Vector2.one, new Vector2(0, -12),
                Vector2.zero);
            highlight.gameObject.AddComponent<Image>().color = new Color(1f, 0.75f, 0.3f);

            listEntry.Add(new TabBar.Entry
            {
                button = button, view = view.gameObject.AddComponent<TabView>(), highlight = highlight.gameObject
            });
        }

        bar.gameObject.AddComponent<TabBar>().EditorSetTabs(listEntry);

        // 첫 탭: 재화 표시 + 전투 시작 버튼
        Transform firstView = listEntry[0].view.transform;

        Text gold = Label(firstView, "Gold 0", font, 48);
        RectTransform goldRt = gold.rectTransform;
        goldRt.anchorMin = new Vector2(0, 1);
        goldRt.anchorMax = Vector2.one;
        goldRt.offsetMin = new Vector2(40, -140);
        goldRt.offsetMax = new Vector2(-40, -40);
        gold.alignment = TextAnchor.MiddleLeft;
        gold.gameObject.AddComponent<CurrencyLabel>().EditorSetup("Currency_Gold", "Gold {0}");

        RectTransform start = Rect("StartBattle", firstView, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(-250, 80), new Vector2(250, 240));

        Image startImage = start.gameObject.AddComponent<Image>();
        startImage.color = new Color(0.85f, 0.45f, 0.2f);
        start.gameObject.AddComponent<Button>().targetGraphic = startImage;
        start.gameObject.AddComponent<StartBattleButton>();
        Label(start, "BATTLE", font, 56);

        RectTransform popupRoot = Rect("PopupRoot", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        canvas.gameObject.AddComponent<PopupManager>().EditorSetRoot(popupRoot);

        EditorSceneManager.SaveScene(scene, LobbyPath);
    }

    private static void CreateBattle()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        CreateEventSystem();

        new GameObject("BattlePlaceholder").AddComponent<BattlePlaceholder>();
        EditorSceneManager.SaveScene(scene, BattlePath);
    }

    private static RectTransform CreateCanvas()
    {
        GameObject go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        return go.GetComponent<RectTransform>();
    }

    private static void CreateEventSystem()
    {
        GameObject go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        return rt;
    }

    private static Text Label(Transform parent, string text, Font font, int size)
    {
        RectTransform rt = Rect("Label", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Text t = rt.gameObject.AddComponent<Text>();
        t.text = text;
        t.font = font;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;

        return t;
    }

    private static void AddToBuildSettings()
    {
        List<EditorBuildSettingsScene> listScene = EditorBuildSettings.scenes
            .Where(s => s.path != LobbyPath && s.path != BattlePath)
            .ToList();

        listScene.Insert(0, new EditorBuildSettingsScene(BattlePath, true));
        listScene.Insert(0, new EditorBuildSettingsScene(LobbyPath, true));
        EditorBuildSettings.scenes = listScene.ToArray();
    }
}
