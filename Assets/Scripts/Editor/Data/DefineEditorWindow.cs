using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// ==================================================

// Define 편집기. 전역 조절값(Define.asset)을 스펙(DefineSpecBase) 단위로 보고 고친다.
// 왼쪽 스펙 목록은 DefineDef에서 DefineSpecBase 타입 프로퍼티를 찾아 자동으로 채우므로, 스펙을 더해도 편집기는 고치지 않는다.
// 오른쪽 값 칸은 이름 폭을 고정해 줄을 맞추고(벡터 값도 이름 옆 한 줄), 필드에 마우스를 올리면 Tooltip 설명이 뜬다

public sealed class DefineEditorWindow : EditorWindow
{
    private const string BackingFieldFormat = "<{0}>k__BackingField";
    private const float SpecListWidth = 180f;
    private const float LabelWidth = 200f;      // 값 이름 칸 폭. 모든 줄이 같은 위치에서 값 칸이 시작한다
    private const float DetailPadding = 10f;

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
    private Vector2 m_Scroll;
    private GUIStyle m_TitleStyle;

    // ------------------------------------------------

    [MenuItem("MushTaker/Define 편집기", false, 1)]
    public static void Open()
    {
        DefineEditorWindow window = GetWindow<DefineEditorWindow>("Define 편집기");
        window.minSize = new Vector2(620f, 360f);
    }

    private void OnEnable()
    {
        EditorApplication.projectChanged += Rebuild;
    }

    private void OnDisable()
    {
        EditorApplication.projectChanged -= Rebuild;
    }

    public void CreateGUI()
    {
        Rebuild();
    }

    private void Rebuild()
    {
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
        m_SelectedIndex = Mathf.Clamp(m_SelectedIndex, 0, Mathf.Max(0, m_ListSpec.Count - 1));

        TwoPaneSplitView split = new TwoPaneSplitView(0, SpecListWidth, TwoPaneSplitViewOrientation.Horizontal);
        split.style.flexGrow = 1f;
        rootVisualElement.Add(split);

        m_SpecList = new ListView(m_ListSpec, 24f, MakeSpecRow, BindSpecRow)
        {
            selectionType = SelectionType.Single,
        };
        // 값 칸은 IMGUI로 그린다(이름 폭 고정·Tooltip·Undo가 인스펙터와 같게 동작)
        IMGUIContainer details = new IMGUIContainer(DrawDetails);
        details.style.flexGrow = 1f;

        m_SpecList.selectionChanged += _ =>
        {
            m_SelectedIndex = m_SpecList.selectedIndex;
            m_Scroll = Vector2.zero;
            details.MarkDirtyRepaint();
        };
        split.Add(m_SpecList);
        split.Add(details);

        if (m_ListSpec.Count > 0)
            m_SpecList.SetSelection(m_SelectedIndex);
    }

    private static VisualElement MakeSpecRow()
    {
        Label label = new Label();
        label.style.unityTextAlign = TextAnchor.MiddleLeft;
        label.style.paddingLeft = 8f;
        return label;
    }

    private void BindSpecRow(VisualElement element, int index)
    {
        ((Label)element).text = m_ListSpec[index].DisplayName;
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
    private void DrawDetails()
    {
        if (m_SerializedDefine == null || m_SerializedDefine.targetObject == null || m_SelectedIndex < 0 || m_SelectedIndex >= m_ListSpec.Count)
            return;

        m_TitleStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
        SpecEntry entry = m_ListSpec[m_SelectedIndex];

        m_SerializedDefine.Update();
        float labelWidth = EditorGUIUtility.labelWidth;
        bool wideMode = EditorGUIUtility.wideMode;
        EditorGUIUtility.labelWidth = LabelWidth;
        EditorGUIUtility.wideMode = true; // Vector2·Vector2Int 값을 이름 옆 한 줄에 둔다(좁으면 다음 줄로 내려가는 것 방지)

        GUILayout.BeginHorizontal();
        GUILayout.Space(DetailPadding);
        GUILayout.BeginVertical();
        GUILayout.Space(DetailPadding);
        GUILayout.Label(entry.DisplayName, m_TitleStyle);
        EditorGUILayout.Space(4f);

        m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
        SerializedProperty spec = m_SerializedDefine.FindProperty(entry.PropertyPath);
        if (spec == null)
        {
            EditorGUILayout.HelpBox($"{entry.PropertyPath}를 찾지 못했습니다. 스펙 클래스에 [Serializable]이 있는지 확인하세요.", MessageType.Error);
        }
        else
        {
            SerializedProperty child = spec.Copy();
            SerializedProperty end = spec.GetEndProperty();
            bool enter = true;
            while (child.NextVisible(enter) && SerializedProperty.EqualContents(child, end) == false)
            {
                enter = false;
                EditorGUILayout.PropertyField(child, true);
            }
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();
        GUILayout.Space(DetailPadding);
        GUILayout.EndHorizontal();

        EditorGUIUtility.labelWidth = labelWidth;
        EditorGUIUtility.wideMode = wideMode;
        m_SerializedDefine.ApplyModifiedProperties();
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
