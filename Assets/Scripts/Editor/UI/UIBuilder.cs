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

    private const string SlotMoldSprite = "UI_Slot_Mold";         // 덜 신선한 칸의 냄새(아이콘 위)
    private const string SlotMoldBackSprite = "UI_Slot_MoldBack"; // 덜 신선한 칸의 상한 색 바탕(아이콘 아래)
    private const string SlotBadgeSprite = "UI_Badge_Prefer";     // 상인 선호 버섯 하트

    private static TMP_FontAsset s_Font;

    // ==================================================

    // 배경 없이 화면에 바로 뜨는 라벨용(외곽선). 없으면 기본 폰트
    public static TMP_FontAsset OutlineFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EditorPath.FontOutlineAsset) ?? Font;

    // 바탕 없이 뜨는 30 이상 글자: 외곽선 폰트(흰 외곽선) + 검은 글자
    public static void SetOutline(TMP_Text text)
    {
        text.font = OutlineFont;
        text.color = Color.black;
    }

    public static TMP_FontAsset Font
    {
        get
        {
            if (s_Font != null)
                return s_Font;

            s_Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EditorPath.FontAsset);
            if (s_Font != null)
                return s_Font;

            // 폴더를 옮겨도 찾을 수 있게 이름으로 한 번 더 찾는다
            string[] guids = AssetDatabase.FindAssets("Galmuri11 SDF t:TMP_FontAsset");
            if (guids.E_IsNullOrEmpty() == false)
                s_Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));

            return s_Font;
        }
    }

    // root ---

    // 씬 UI 구성: 캔버스 하나 → 화면 프리팹(꽉 차게, 안에 SafeArea) → PopupRoot(SafeArea, 나중 형제라 화면 위에 그린다).
    // 화면 프리팹이 노치까지 덮는 것(바탕·가장자리 연출)과 SafeArea 안 UI를 함께 든다
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

        return go.transform as RectTransform;
    }

    // 노치를 피하는 영역. 전체 화면 크기의 부모(캔버스·화면 루트) 바로 아래에 둔다
    public static RectTransform CreateSafeArea(Transform parent, string name = "SafeArea")
    {
        RectTransform safe = Stretch(name, parent);
        safe.gameObject.AddComponent<UISafeArea>();
        return safe;
    }

    // 화면 프리팹 루트(꽉 찬 Stretch). 자식 UI는 함께 만든 SafeArea 아래에 붙인다
    public static RectTransform CreateScreenRoot(string name, out RectTransform safe)
    {
        RectTransform root = Stretch(name, null);
        safe = CreateSafeArea(root);
        return root;
    }

    // 화면 프리팹을 캔버스 아래 꽉 차게 놓는다. 프리팹이 없으면 null(호출부가 에러)
    public static T PlaceScreen<T>(string prefabPath, Transform canvas) where T : ScreenBase
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        T screen = prefab != null ? Spawn<T>(prefab, canvas) : null;
        if (screen == null)
            return null;

        RectTransform rt = screen.RectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return screen;
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

    // sprite ---

    // Art/UI/Sprites 아래(하위 폴더 포함)에서 파일 이름(확장자 없이)이 같은 스프라이트를 찾는다. 없으면 경고 후 null
    public static Sprite LoadSprite(string spriteName)
    {
        string[] listGUID = AssetDatabase.FindAssets($"{spriteName} t:Sprite", new[] { EditorPath.UISpriteDir.TrimEnd('/') });
        foreach (string guid in listGUID)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == spriteName)
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        CLogger.Warning("UIBuilder", $"스프라이트가 없습니다: {spriteName}");
        return null;
    }

    // rect ---

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

    // graphic ---

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

    // 고정 문구 라벨. UILabel을 붙여 켜질 때 StringDef 문구로 채운다(에디터에서는 지금 표 값을 미리 넣어 둔다)
    public static TMP_Text Label(Transform parent, EStringKey key, int size, TextAlignmentOptions align = TextAlignmentOptions.Center, string name = "Label")
    {
        TMP_Text t = Label(parent, string.Empty, size, align, name);
        AddStringKey(t, key);
        return t;
    }

    public static UILabel AddStringKey(TMP_Text text, EStringKey key)
    {
        UILabel label = text.gameObject.AddComponent<UILabel>();
        label.EditorSetup(text, key);
        text.text = StringDef.EditorGet(StringDef.S_GetName(key));
        return label;
    }

    // parts ---

    // 고정 문구 버튼. 버튼 글자에 UILabel을 붙인다
    public static Button Button(Transform parent, string name, EStringKey key, Vector2 size, Color? color = null)
    {
        Button button = Button(parent, name, string.Empty, size, color);
        AddStringKey(button.GetComponentInChildren<TMP_Text>(), key);
        return button;
    }

    public static Button Button(Transform parent, string name, string text, Vector2 size, Color? color = null)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EditorPath.ButtonPrefab);
        Button button = prefab != null ? Spawn<Button>(prefab, parent) : BuildButton(parent);

        button.name = name;
        RectTransform buttonRt = button.transform as RectTransform;
        if (buttonRt != null)
            buttonRt.sizeDelta = size;

        button.GetComponentInChildren<TMP_Text>().text = text;

        if (color.HasValue)
            button.targetGraphic.color = color.Value;

        return button;
    }

    public static UIGauge Gauge(Transform parent, string name, Vector2 size, Color fillColor, bool isShowText)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EditorPath.GaugePrefab);
        UIGauge gauge = prefab != null ? Spawn<UIGauge>(prefab, parent) : BuildGauge(parent);

        gauge.name = name;
        RectTransform gaugeRt = gauge.RectTransform;
        if (gaugeRt != null)
            gaugeRt.sizeDelta = size;

        gauge.EditorSetStyle(fillColor, isShowText);

        return gauge;
    }

    public static UICommonSlot CommonSlot(Transform parent, string name, float size = 120f)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EditorPath.CommonSlotPrefab);
        UICommonSlot slot = prefab != null ? Spawn<UICommonSlot>(prefab, parent) : BuildCommonSlot(parent);

        slot.name = name;
        RectTransform slotRt = slot.RectTransform;
        if (slotRt != null)
            slotRt.sizeDelta = new Vector2(size, size);

        return slot;
    }

    // 칸 위에 뜨는 이름 말풍선. 글자 길이에 맞춰 늘어난다. 목록보다 뒤 형제로 만들어야 칸 위에 그려진다
    public static UITooltip Tooltip(Transform parent, string name = "Tooltip")
    {
        RectTransform rt = Anchored(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 48));
        rt.pivot = new Vector2(0.5f, 0f);
        AddImage(rt, UIStyle.Dim);
        CanvasGroup group = rt.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        HorizontalLayoutGroup layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 8, 8);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        ContentSizeFitter fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text label = Label(rt, "이름", UIStyle.FontSmall);

        UITooltip tooltip = rt.gameObject.AddComponent<UITooltip>();
        tooltip.EditorSetup(group, label);
        rt.gameObject.SetActive(false);

        return tooltip;
    }

    // build (프리팹 원본) ---

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

    public static UICommonSlot BuildCommonSlot(Transform parent)
    {
        RectTransform rt = Anchored("UICommonSlot", parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 120));
        Image bg = AddImage(rt, UIStyle.RarityCommon, true);
        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;

        // 상한 색 바탕(덜 신선). 바탕 위·아이콘 아래에 칸 전체를 덮는다
        GameObject moldBack = BuildSlotMoldBack(rt).gameObject;

        RectTransform iconRt = Stretch("Icon", rt, 14f);
        Image icon = AddImage(iconRt, Color.white);
        icon.preserveAspect = true;

        // 냄새(덜 신선). 아이콘 위·개수 글자 아래에 칸 전체를 덮는다
        GameObject mold = BuildSlotMold(rt).gameObject;

        TMP_Text count = Label(rt, "x0", UIStyle.FontSmall, TextAlignmentOptions.BottomRight, "Count");
        count.rectTransform.offsetMin = new Vector2(6, 4);
        count.rectTransform.offsetMax = new Vector2(-8, -4);
        count.enableAutoSizing = true; // 큰 골드도 칸 안에 들어가게 줄인다
        count.fontSizeMin = UIStyle.FontSmall / 2;
        count.fontSizeMax = UIStyle.FontSmall;

        // 이름은 칸 아래에 붙는다. 목록마다 필요 없으면 꺼 둔다
        RectTransform nameRt = Rect("Name", rt, Vector2.zero, new Vector2(1, 0), new Vector2(-20, -40), new Vector2(20, -4));
        TMP_Text displayName = Label(nameRt, string.Empty, UIStyle.FontSmall);
        displayName.color = UIStyle.TextDim;

        GameObject dim = BuildSlotDim(rt).gameObject;
        TMP_Text center = BuildSlotCenter(rt);
        GameObject badge = BuildSlotBadge(rt).gameObject;
        TMP_Text caption = BuildSlotCaption(rt);

        UICommonSlot slot = rt.gameObject.AddComponent<UICommonSlot>();
        slot.EditorSetup(bg, icon, mold, moldBack, dim, caption, center, badge, count, displayName, button); // 희귀도 바탕 그림은 RarityDef

        return slot;
    }

    // 그림 한 장짜리 공용 요소(클릭 버섯 조각 등). 누름은 받지 않는다
    public static UICommonImage BuildCommonImage(Transform parent)
    {
        RectTransform rt = Anchored(nameof(EUIComponentType.UICommonImage), parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
        Image image = AddImage(rt, Color.white);
        image.preserveAspect = true;

        UICommonImage commonImage = rt.gameObject.AddComponent<UICommonImage>();
        SetRef(commonImage, "m_Image", image);

        return commonImage;
    }

    // UIListView 배치 옵션. 생성기에서 영역을 만들 때 값을 모아 두었다가 SetListView로 화면 필드에 넣는다
    public class ListOption
    {
        public RectTransform Root;
        public EUIListAxis Axis;
        public Vector2 CellSize;
        public Vector2 Spacing;
        public TextAnchor Alignment;
        public RectOffset Padding;
        public bool IsStretch;
        public bool IsFitContent;

        public ListOption(RectTransform root, EUIListAxis axis, Vector2 cellSize, Vector2 spacing, TextAnchor alignment,
            RectOffset padding = null, bool isStretch = false, bool isFitContent = false)
        {
            Root = root;
            Axis = axis;
            CellSize = cellSize;
            Spacing = spacing;
            Alignment = alignment;
            Padding = padding ?? new RectOffset();
            IsStretch = isStretch;
            IsFitContent = isFitContent;
        }
    }

    // 화면의 UIListView 직렬화 필드(m_XxxPool)에 배치 옵션을 넣는다. 좌표는 런타임에 UIListView가 계산한다(LayoutGroup 쓰지 않음)
    public static void SetListView(Object target, string field, ListOption option)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            CLogger.Error("UIBuilder", $"{target.GetType().Name}.{field} 필드가 없습니다.", target);
            return;
        }

        prop.FindPropertyRelative("m_Root").objectReferenceValue = option.Root;
        prop.FindPropertyRelative("m_Axis").enumValueIndex = (int)option.Axis;
        prop.FindPropertyRelative("m_CellSize").vector2Value = option.CellSize;
        prop.FindPropertyRelative("m_Spacing").vector2Value = option.Spacing;
        prop.FindPropertyRelative("m_Alignment").enumValueIndex = (int)option.Alignment;
        prop.FindPropertyRelative("m_IsStretch").boolValue = option.IsStretch;
        prop.FindPropertyRelative("m_IsFitContent").boolValue = option.IsFitContent;

        SerializedProperty padding = prop.FindPropertyRelative("m_Padding");
        padding.FindPropertyRelative("m_Left").intValue = option.Padding.left;
        padding.FindPropertyRelative("m_Right").intValue = option.Padding.right;
        padding.FindPropertyRelative("m_Top").intValue = option.Padding.top;
        padding.FindPropertyRelative("m_Bottom").intValue = option.Padding.bottom;

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // serialize ---

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

    // prefab ---

    public static T Spawn<T>(GameObject prefab, Transform parent) where T : Component
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        return go.GetComponent<T>();
    }

    // 슬롯의 곰팡이 얼룩 칸. 꺼 둔 채 만든다
    private static Image BuildSlotMold(RectTransform slot)
    {
        RectTransform moldRt = Stretch("Mold", slot);
        Image mold = AddImage(moldRt, Color.white);
        mold.sprite = LoadSprite(SlotMoldSprite);
        mold.preserveAspect = true;
        moldRt.gameObject.SetActive(false);

        return mold;
    }

    // 슬롯의 상한 색 바탕. 꺼 둔 채 만든다
    private static Image BuildSlotMoldBack(RectTransform slot)
    {
        RectTransform moldBackRt = Stretch("MoldBack", slot, 4f);
        Image moldBack = AddImage(moldBackRt, Color.white);
        moldBack.sprite = LoadSprite(SlotMoldBackSprite);
        moldBackRt.gameObject.SetActive(false);

        return moldBack;
    }

    // 슬롯 오른쪽 위 작은 하트(상인 선호). 칸 모서리에서 바깥으로 살짝 넘어간다. 꺼 둔 채 만든다
    private static Image BuildSlotBadge(RectTransform slot)
    {
        RectTransform badgeRt = Anchored("Badge", slot, Vector2.one, new Vector2(8, 8), new Vector2(24, 24)); // 24 중 8이 칸 밖
        Image badge = AddImage(badgeRt, Color.white);
        badge.sprite = LoadSprite(SlotBadgeSprite);
        badge.preserveAspect = true;
        badgeRt.gameObject.SetActive(false);

        return badge;
    }

    // 슬롯 가운데 큰 글자("전체", "7 → 3"). 딤 위에 뜬다. 꺼 둔 채 만든다
    private static TMP_Text BuildSlotCenter(RectTransform slot)
    {
        TMP_Text center = Label(slot, string.Empty, UIStyle.FontNormal, TextAlignmentOptions.Center, "Center");
        center.rectTransform.offsetMin = new Vector2(6, 6);
        center.rectTransform.offsetMax = new Vector2(-6, -6);
        center.enableAutoSizing = true;
        center.fontSizeMin = UIStyle.FontSmall / 2;
        center.fontSizeMax = UIStyle.FontNormal;
        center.gameObject.SetActive(false); // SetCenter가 글자 오브젝트를 켜고 끈다

        return center;
    }

    // 슬롯을 덮는 반투명 검정. 꺼 둔 채 만든다
    private static Image BuildSlotDim(RectTransform slot)
    {
        Image dim = Panel("Dim", slot, UIStyle.Dim);
        dim.gameObject.SetActive(false);

        return dim;
    }

    // 슬롯 바로 위 작은 글자. 칸보다 조금 넓고, 길면 줄어든다. 꺼 둔 채 만든다
    private static TMP_Text BuildSlotCaption(RectTransform slot)
    {
        RectTransform rt = Rect("Caption", slot, new Vector2(0, 1), Vector2.one, new Vector2(-20, 4), new Vector2(20, 34));
        TMP_Text caption = Label(rt, string.Empty, UIStyle.FontSmall, TextAlignmentOptions.Bottom);
        caption.color = UIStyle.Gold;
        caption.enableAutoSizing = true;
        caption.fontSizeMin = UIStyle.FontSmall / 2;
        caption.fontSizeMax = UIStyle.FontSmall;
        caption.gameObject.SetActive(false); // SetCaption이 글자 오브젝트를 켜고 끈다(rt를 끄면 켜도 안 보인다)

        return caption;
    }

    // 이미 있으면 덮어쓰지 않는다(isOverwrite면 같은 경로에 덮어써 GUID·참조를 유지). 만들었으면 true. go는 지운다
    public static bool SaveAsPrefab(GameObject go, string path, bool isOverwrite = false)
    {
        if (isOverwrite == false && File.Exists(path))
        {
            CLogger.Log("UIBuilder", $"{path}가 이미 있어 건너뜁니다.");
            Object.DestroyImmediate(go);
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        return true;
    }
}
