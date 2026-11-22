using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ==================================================

// 타이틀(ScreenTitle)·로딩(OverlayLoading) 화면 프리팹을 만들고, 타이틀 씬을 재생성한다. 로딩은 씬이 아니라 UIManager가 들고 있는 오버레이다.
// 프리팹은 UISetup의 "모든 UI 다시 만들기"가 Prefabs 표로 만들고, 씬은 로비 씬과 같은 구성(카메라·EventSystem·캔버스·PopupRoot)이다

public static class TitleUISetup
{
    private const string TitleSprite = "UI_Title"; // 78x57 도트
    private const float TitleScale = 8f;           // 도트 한 칸 = 8px(기준 해상도 1080 높이)
    private const float LoadingTitleScale = 4f;
    private const string StartBGSprite = "UI_Title_StartBG"; // 64x12. "클릭하여 시작하기" 뒤 좌우로 연해지는 띠
    private const string TitleBackgroundSprite = "UI_Title_Background";
    private static readonly Color TitleBackgroundTint = new Color(0.65f, 0.65f, 0.65f); // 그림을 어둡게 눌러 글자가 뜨게(26.11.22 대장 프리팹 값)
    private const int LoadingSortingOrder = 1000; // 씬 캔버스·팝업보다 위
    private static readonly Vector2 LoadingBarSize = new Vector2(720, 28);

    // ------------------------------------------------

    [MenuItem("MushTaker/초기 설정/타이틀 씬 재생성", false, 10)]
    public static void CreateScenes()
    {
        if (UIBuilder.Font == null)
        {
            CLogger.Warning("TitleUISetup", "MushTaker/초기 설정/UI 기반 준비를 먼저 실행하세요.");
            return;
        }

        if (File.Exists(EditorPath.TitleScene) && EditorUtility.DisplayDialog("타이틀 씬", $"{EditorPath.TitleScene}가 이미 있습니다. 새로 만들까요?", "새로 만들기", "취소") == false)
            return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            return;

        UISetup.BuildAllPrefabs(false); // 없는 프리팹만 만든다

        if (BuildScene<TitleSceneController, ScreenTitle>(EditorPath.ScreenTitlePrefab, EditorPath.TitleScene) == false)
            return;

        AddToBuildSettings();
    }

    public static readonly (string Path, System.Func<GameObject> Build)[] Prefabs =
    {
        (EditorPath.ScreenTitlePrefab, () => BuildTitleScreen().gameObject),
        (EditorPath.OverlayLoadingPrefab, () => BuildOverlayLoading().gameObject),
    };

    // ==================================================

    // 구성은 LobbyUISetup.BuildScene과 같다
    private static bool BuildScene<TController, TScreen>(string prefabPath, string scenePath)
        where TController : SceneControllerBase
        where TScreen : ScreenBase
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TController sceneController = new GameObject(typeof(TController).Name).AddComponent<TController>();

        Camera cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = UIStyle.Background;

        UIBuilder.CreateEventSystemIfNeeded();

        RectTransform canvas = UIBuilder.CreateCanvas("UICanvas");
        TScreen screen = UIBuilder.PlaceScreen<TScreen>(prefabPath, canvas);
        if (screen == null)
        {
            CLogger.Error("TitleUISetup", $"{prefabPath}에 {typeof(TScreen).Name} 프리팹이 없습니다. 모든 UI 다시 만들기를 먼저 실행하세요.");
            return false;
        }

        RectTransform popupRoot = UIBuilder.CreateSafeArea(canvas, "PopupRoot");
        sceneController.EditorSetup(popupRoot, cam, screen);

        EditorSceneManager.SaveScene(scene, scenePath);
        return true;
    }

    // 루트(꽉 참, 배경 그림, 누르면 시작하는 버튼) 아래 SafeArea → 왼쪽 위 타이틀 그림·"클릭하여 시작하기"
    // 배치는 26.11.22 대장이 프리팹에서 고친 값
    private static ScreenTitle BuildTitleScreen()
    {
        RectTransform root = UIBuilder.CreateScreenRoot(nameof(ScreenTitle), out RectTransform safe);
        Button startButton = AddBackgroundButton(root, TitleBackgroundTint, UIBuilder.LoadSprite(TitleBackgroundSprite));

        RectTransform title = AddTitleImage(safe, new Vector2(0f, 1f), TitleScale);
        title.pivot = new Vector2(0f, 1f);
        title.anchoredPosition = new Vector2(100, -75);

        RectTransform press = UIBuilder.Anchored("Press", safe, new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(800, 60));

        // 글자 뒤 띠. 글자 칸보다 좌우 80씩 좁고 위아래 15씩 크다. 글자와 함께 튄다
        RectTransform bg = UIBuilder.Stretch("BG", press);
        bg.sizeDelta = new Vector2(-160, 30);
        UIBuilder.AddImage(bg, Color.white).sprite = UIBuilder.LoadSprite(StartBGSprite);

        UIBuilder.SetOutline(UIBuilder.Label(press, EStringKey.Title_PressToStart, UIStyle.FontLarge));

        ScreenTitle screen = root.gameObject.AddComponent<ScreenTitle>();
        screen.EditorSetup(startButton, press);
        return screen;
    }

    // 루트가 맨 위 정렬 캔버스(씬 밖 [Managers] 아래에 놓인다) → 바탕 버튼(클릭을 막고 버섯을 튀긴다) → SafeArea → 작은 타이틀 그림·"불러오는 중..."·진행바
    private static OverlayLoading BuildOverlayLoading()
    {
        RectTransform root = UIBuilder.CreateCanvas(nameof(OverlayLoading), LoadingSortingOrder);
        Button burstButton = AddBackgroundButton(UIBuilder.Stretch("Background", root), UIStyle.Background);
        RectTransform safe = UIBuilder.CreateSafeArea(root);

        AddTitleImage(safe, new Vector2(0.5f, 0.6f), LoadingTitleScale);

        RectTransform text = UIBuilder.Anchored("Text", safe, new Vector2(0.5f, 0.3f), Vector2.zero, new Vector2(800, 80));
        UIBuilder.SetOutline(UIBuilder.Label(text, EStringKey.Loading_Text, UIStyle.FontNormal));

        UIGauge progress = UIBuilder.Gauge(safe, "Progress", LoadingBarSize, UIStyle.Gold, false);
        RectTransform progressRt = progress.RectTransform;
        progressRt.anchorMin = progressRt.anchorMax = progressRt.pivot = new Vector2(0.5f, 0.2f);
        progressRt.anchoredPosition = Vector2.zero;
        progress.SetRatio(0f, true);

        OverlayLoading overlay = root.gameObject.AddComponent<OverlayLoading>();
        overlay.EditorSetup(burstButton, progress);
        return overlay;
    }

    // 노치까지 덮는 바탕이 곧 버튼(색 변화 없음)
    private static Button AddBackgroundButton(RectTransform root, Color color, Sprite sprite = null)
    {
        Image bg = UIBuilder.AddImage(root, color, true);
        bg.sprite = sprite;
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;
        button.transition = Selectable.Transition.None;
        return button;
    }

    // 타이틀 도트 그림을 scale배로 anchor 가운데에 놓는다. 그림이 없으면 빈 칸(LoadSprite가 경고)
    private static RectTransform AddTitleImage(Transform parent, Vector2 anchor, float scale)
    {
        Sprite sprite = UIBuilder.LoadSprite(TitleSprite);
        Vector2 size = sprite != null ? sprite.rect.size * scale : Vector2.zero;

        RectTransform rt = UIBuilder.Anchored("Title", parent, anchor, Vector2.zero, size);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        UIBuilder.AddImage(rt, Color.white).sprite = sprite;
        return rt;
    }

    // 빌드 첫 씬은 타이틀. 나머지 순서는 그대로 둔다
    private static void AddToBuildSettings()
    {
        List<EditorBuildSettingsScene> listScene = EditorBuildSettings.scenes
            .Where(s => s.path != EditorPath.TitleScene)
            .ToList();

        listScene.Insert(0, new EditorBuildSettingsScene(EditorPath.TitleScene, true));
        EditorBuildSettings.scenes = listScene.ToArray();
    }
}
