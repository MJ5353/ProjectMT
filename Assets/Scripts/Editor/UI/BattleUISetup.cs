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

    // ==================================================



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
