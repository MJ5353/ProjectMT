using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 에디터에서 UI 초안을 만드는 도구 모음. 화면 생성 메뉴(UISetup 등)가 이걸로 조립한다.
// 공용 부품은 프리팹이 있으면 프리팹 인스턴스로 넣어서, 대장이 프리팹을 고치면 모든 화면에 반영된다
public static class UIBuilder
{
    public const string FontDir = "Assets/Art/Fonts/Galmuri";
    public const string FontPath = FontDir + "/Galmuri11.ttf";
    public const string FontAssetPath = FontDir + "/Galmuri11 SDF.asset";

    public const string PrefabDir = "Assets/Prefabs/UI/Common";
    public const string GaugePrefabPath = PrefabDir + "/UIGauge.prefab";
    public const string ItemSlotPrefabPath = PrefabDir + "/UIItemSlot.prefab";
    public const string ButtonPrefabPath = PrefabDir + "/UIButton.prefab";

    private static TMP_FontAsset s_Font;

    // ==================================================

    public static TMP_FontAsset Font
    {
        get
        {
            if (s_Font != null)
                return s_Font;

            s_Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (s_Font != null)
                return s_Font;

            // 폴더를 옮겨도 찾을 수 있게 이름으로 한 번 더 찾는다
            string[] guids = AssetDatabase.FindAssets("Galmuri11 SDF t:TMP_FontAsset");
            if (guids.E_IsNullOrEmpty() == false)
                s_Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));

            return s_Font;
        }
    }

    // root ------------------------------------------------

    // 화면 하나 = 캔버스 하나. 자식은 SafeArea 아래에 붙인다
    public static RectTransform CreateCanvas(string name, int sortingOrder = 0)
    {
        GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        // 가로 고정이라 높이에 맞추고, 폭은 기기마다 늘어난다
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIStyle.RefResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        RectTransform safe = Stretch("SafeArea", go.transform);
        safe.gameObject.AddComponent<UISafeArea>();

        return safe;
    }

    public static void CreateEventSystemIfNeeded()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    // rect ------------------------------------------------

    public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        return rt;
    }

    public static RectTransform Stretch(string name, Transform parent, float inset = 0f)
    {
        return Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));
    }

    // 한 점(앵커) 기준으로 크기를 정해 놓는다. pivot도 앵커와 같게 둔다
    public static RectTransform Anchored(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rt = Rect(name, parent, anchor, anchor, Vector2.zero, Vector2.zero);
        rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        return rt;
    }

    // graphic ------------------------------------------------

    public static Image Panel(string name, Transform parent, Color color, bool isRaycast = false)
    {
        RectTransform rt = Stretch(name, parent);
        return AddImage(rt, color, isRaycast);
    }

    public static Image AddImage(RectTransform rt, Color color, bool isRaycast = false)
    {
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = isRaycast;

        return image;
    }

    public static TMP_Text Label(Transform parent, string text, int size, TextAlignmentOptions align = TextAlignmentOptions.Center, string name = "Label")
    {
        RectTransform rt = Stretch(name, parent);

        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (Font != null)
            t.font = Font;

        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = UIStyle.Text;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;

        return t;
    }

    // parts ------------------------------------------------

    public static Button Button(Transform parent, string name, string text, Vector2 size, Color? color = null)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        Button button = prefab != null ? Spawn<Button>(prefab, parent) : BuildButton(parent);

        button.name = name;
        ((RectTransform)button.transform).sizeDelta = size;
        button.GetComponentInChildren<TMP_Text>().text = text;

        if (color.HasValue)
            button.targetGraphic.color = color.Value;

        return button;
    }

    public static UIGauge Gauge(Transform parent, string name, Vector2 size, Color fillColor, bool isShowText)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GaugePrefabPath);
        UIGauge gauge = prefab != null ? Spawn<UIGauge>(prefab, parent) : BuildGauge(parent);

        gauge.name = name;
        ((RectTransform)gauge.transform).sizeDelta = size;
        gauge.transform.Find("Inner/Fill").GetComponent<Image>().color = fillColor;
        gauge.GetComponentInChildren<TMP_Text>(true).gameObject.SetActive(isShowText);

        return gauge;
    }

    public static UIItemSlot ItemSlot(Transform parent, string name, float size = 120f)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ItemSlotPrefabPath);
        UIItemSlot slot = prefab != null ? Spawn<UIItemSlot>(prefab, parent) : BuildItemSlot(parent);

        slot.name = name;
        ((RectTransform)slot.transform).sizeDelta = new Vector2(size, size);

        return slot;
    }

    // build (프리팹 원본) ------------------------------------------------

    public static Button BuildButton(Transform parent)
    {
        RectTransform rt = Anchored("UIButton", parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, 110));
        Image image = AddImage(rt, UIStyle.Accent, true);

        // 아래쪽 그림자 띠로 도트 버튼 느낌을 낸다
        RectTransform shade = Rect("Shade", rt, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 10));
        AddImage(shade, UIStyle.AccentDark);

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        button.colors = colors;

        TMP_Text label = Label(rt, "버튼", UIStyle.FontNormal);
        label.rectTransform.offsetMin = new Vector2(0, 10);

        return button;
    }

    public static UIGauge BuildGauge(Transform parent)
    {
        RectTransform rt = Anchored("UIGauge", parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 36));
        AddImage(rt, UIStyle.Dim);

        RectTransform inner = Stretch("Inner", rt, 4f);

        RectTransform trail = Stretch("Trail", inner);
        AddImage(trail, UIStyle.GaugeTrail);

        RectTransform fill = Stretch("Fill", inner);
        AddImage(fill, UIStyle.HpPlayer);

        TMP_Text text = Label(rt, "0 / 0", UIStyle.FontSmall, TextAlignmentOptions.Center, "Text");

        UIGauge gauge = rt.gameObject.AddComponent<UIGauge>();
        gauge.EditorSetup(fill, trail, text);

        return gauge;
    }

    public static UIItemSlot BuildItemSlot(Transform parent)
    {
        RectTransform rt = Anchored("UIItemSlot", parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 120));
        Image bg = AddImage(rt, UIStyle.PanelLight, true);
        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;

        RectTransform iconRt = Stretch("Icon", rt, 14f);
        Image icon = AddImage(iconRt, Color.white);
        icon.preserveAspect = true;

        TMP_Text count = Label(rt, "x0", UIStyle.FontSmall, TextAlignmentOptions.BottomRight, "Count");
        count.rectTransform.offsetMin = new Vector2(6, 4);
        count.rectTransform.offsetMax = new Vector2(-8, -4);

        // 이름은 칸 아래에 붙는다. 목록마다 필요 없으면 꺼 둔다
        RectTransform nameRt = Rect("Name", rt, Vector2.zero, new Vector2(1, 0), new Vector2(-20, -40), new Vector2(20, -4));
        TMP_Text displayName = Label(nameRt, string.Empty, UIStyle.FontSmall);
        displayName.color = UIStyle.TextDim;

        RectTransform badge = Anchored("Badge", rt, new Vector2(0, 1), new Vector2(-6, 6), new Vector2(44, 32));
        AddImage(badge, UIStyle.Gold);
        TMP_Text badgeText = Label(badge, "S", UIStyle.FontSmall);
        badgeText.color = UIStyle.Background;

        UIItemSlot slot = rt.gameObject.AddComponent<UIItemSlot>();
        slot.EditorSetup(icon, count, displayName, badge.gameObject, badgeText, button);

        return slot;
    }

    // serialize ------------------------------------------------

    // 화면 스크립트의 private 직렬화 필드(m_Xxx)를 채운다. 이름이 틀리면 에러 로그를 남긴다
    public static void SetRef(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            CLogger.Error("UIBuilder", $"{target.GetType().Name}.{field} 필드가 없습니다.", target);
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // prefab ------------------------------------------------

    public static T Spawn<T>(GameObject prefab, Transform parent) where T : Component
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        return go.GetComponent<T>();
    }

    // 이미 있으면 덮어쓰지 않는다(대장이 손본 프리팹 보호). 만들었으면 true
    public static bool SaveAsPrefab(GameObject go, string path)
    {
        if (File.Exists(path))
        {
            CLogger.Warning("UIBuilder", $"{path}가 이미 있어 건너뜁니다.");
            Object.DestroyImmediate(go);
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        return true;
    }
}
