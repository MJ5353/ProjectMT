using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// ==================================================

// Define 도구. 전역 조절값(Define.asset)을 스펙(DefineSpecBase) 단위로 보고 고친다.
// 왼쪽 스펙 목록은 DefineDef에서 DefineSpecBase 타입 프로퍼티를 찾아 자동으로 채우므로, 스펙을 더해도 도구는 고치지 않는다

public sealed class DefineToolWindow : EditorWindow
{
    private const string BackingFieldFormat = "<{0}>k__BackingField";
    private const float SpecListWidth = 180f;

    // 스펙 한 칸: 목록에 보일 이름과 Define 에셋 안 직렬화 경로
    private readonly struct SpecEntry
    {
        public readonly string DisplayName;
        public readonly string PropertyPath;

        public SpecEntry(string displayName, string propertyPath)
        {
            DisplayName = displayName;
            PropertyPath = propertyPath;
        }
    }

    [SerializeField] private int m_SelectedIndex; // 다시 그려도 고른 스펙을 유지

    private readonly List<SpecEntry> m_ListSpec = new();
    private DefineDef m_Define;
    private SerializedObject m_SerializedDefine;
    private ListView m_SpecList;
    private ScrollView m_Details;

    // ------------------------------------------------

    [MenuItem("MushTaker/Define 도구", false, 1)]
    public static void Open()
    {
        DefineToolWindow window = GetWindow<DefineToolWindow>("Define 도구");
        window.minSize = new Vector2(560f, 360f);
    }

    private void OnEnable()
    {
        EditorApplication.projectChanged += Rebuild;
        Undo.undoRedoPerformed += RefreshDetails;
    }

    private void OnDisable()
    {
        EditorApplication.projectChanged -= Rebuild;
        Undo.undoRedoPerformed -= RefreshDetails;
    }

    public void CreateGUI()
    {
        Rebuild();
    }

    private void Rebuild()
    {
        rootVisualElement.Unbind();
        rootVisualElement.Clear();
        m_Define = FindDefine();
        if (m_Define == null)
        {
            rootVisualElement.Add(new HelpBox("Define 에셋이 없습니다. 만들면 코드 기본값으로 채워집니다.", HelpBoxMessageType.Warning));
            rootVisualElement.Add(new Button(CreateDefine) { text = "Define 에셋 만들기" });
            return;
        }

        m_SerializedDefine = new SerializedObject(m_Define);
        CollectSpecs();

        Button ping = new Button(() => EditorGUIUtility.PingObject(m_Define)) { text = "프로젝트에서 찾기", tooltip = AssetDatabase.GetAssetPath(m_Define) };
        rootVisualElement.Add(ping);

        TwoPaneSplitView split = new TwoPaneSplitView(0, SpecListWidth, TwoPaneSplitViewOrientation.Horizontal);
        split.style.flexGrow = 1f;
        rootVisualElement.Add(split);

        m_SpecList = new ListView(m_ListSpec, 22f, () => new Label(), (element, index) => ((Label)element).text = m_ListSpec[index].DisplayName)
        {
            selectionType = SelectionType.Single,
        };
        m_SpecList.selectionChanged += _ =>
        {
            m_SelectedIndex = m_SpecList.selectedIndex;
            RefreshDetails();
        };
        split.Add(m_SpecList);

        m_Details = new ScrollView();
        m_Details.style.paddingLeft = 8f;
        m_Details.style.paddingRight = 8f;
        split.Add(m_Details);

        if (m_ListSpec.Count > 0)
            m_SpecList.SetSelection(Mathf.Clamp(m_SelectedIndex, 0, m_ListSpec.Count - 1));
    }

    // DefineDef의 DefineSpecBase 프로퍼티를 선언 순서대로 모은다
    private void CollectSpecs()
    {
        m_ListSpec.Clear();
        IEnumerable<PropertyInfo> properties = typeof(DefineDef).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => typeof(DefineSpecBase).IsAssignableFrom(property.PropertyType))
            .OrderBy(property => property.MetadataToken);

        foreach (PropertyInfo property in properties)
        {
            DefineSpecBase spec = property.GetValue(m_Define) as DefineSpecBase;
            string displayName = spec != null ? spec.DisplayName : property.Name;
            m_ListSpec.Add(new SpecEntry(displayName, string.Format(BackingFieldFormat, property.Name)));
        }
    }

    // 고른 스펙의 값만 그린다
    private void RefreshDetails()
    {
        if (m_Details == null || m_SerializedDefine == null || m_SerializedDefine.targetObject == null)
            return;

        m_Details.Unbind();
        m_Details.Clear();
        if (m_SelectedIndex < 0 || m_SelectedIndex >= m_ListSpec.Count)
            return;

        m_SerializedDefine.Update();
        SpecEntry entry = m_ListSpec[m_SelectedIndex];
        Label title = new Label(entry.DisplayName);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.fontSize = 14f;
        title.style.marginTop = 4f;
        title.style.marginBottom = 6f;
        m_Details.Add(title);

        SerializedProperty spec = m_SerializedDefine.FindProperty(entry.PropertyPath);
        if (spec == null)
        {
            m_Details.Add(new HelpBox($"{entry.PropertyPath}를 찾지 못했습니다. 스펙 클래스에 [Serializable]이 있는지 확인하세요.", HelpBoxMessageType.Error));
            return;
        }

        SerializedProperty child = spec.Copy();
        SerializedProperty end = spec.GetEndProperty();
        bool enter = true;
        while (child.NextVisible(enter) && SerializedProperty.EqualContents(child, end) == false)
        {
            enter = false;
            m_Details.Add(new PropertyField(child.Copy()));
        }

        m_Details.Bind(m_SerializedDefine);
    }

    private static DefineDef FindDefine()
    {
        string guid = AssetDatabase.FindAssets("t:" + nameof(DefineDef)).FirstOrDefault();
        return guid == null ? null : AssetDatabase.LoadAssetAtPath<DefineDef>(AssetDatabase.GUIDToAssetPath(guid));
    }

    private void CreateDefine()
    {
        DefineDef define = CreateInstance<DefineDef>();
        define.EditorSetIdentity(DefineDef.DefineID, "전역 조절값");
        AssetDatabase.CreateAsset(define, EditorPath.DataDir + DefineDef.DefineID + ".asset");
        AssetDatabase.SaveAssets();
        Rebuild();
    }
}
