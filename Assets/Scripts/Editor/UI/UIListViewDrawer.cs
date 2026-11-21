using UnityEditor;
using UnityEngine;

// ==================================================

// UIListView 인스펙터. 배치 옵션 아래에 "미리보기" 버튼을 붙여, 저장되지 않는 가짜 칸으로 배치를 바로 확인한다
// (LayoutGroup이 없어서 에디터에서는 칸 자리가 보이지 않으므로)

[CustomPropertyDrawer(typeof(UIListViewBase), true)]
public class UIListViewDrawer : PropertyDrawer
{
    private const float ButtonGap = 2f;
    private const int PreviewMax = 50;

    private static int s_PreviewCount = 6; // 미리보기 칸 수(모든 목록 공용)

    // ------------------------------------------------

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUI.GetPropertyHeight(property, label, true);
        if (property.isExpanded)
            height += EditorGUIUtility.singleLineHeight + ButtonGap * 2f;

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        float fieldHeight = EditorGUI.GetPropertyHeight(property, label, true);
        Rect fieldRect = new Rect(position.x, position.y, position.width, fieldHeight);
        EditorGUI.PropertyField(fieldRect, property, label, true);

        if (property.isExpanded == false)
            return;

        Rect line = EditorGUI.IndentedRect(new Rect(position.x, fieldRect.yMax + ButtonGap, position.width, EditorGUIUtility.singleLineHeight));
        float third = line.width / 3f;

        Rect countRect = new Rect(line.x, line.y, third - ButtonGap, line.height);
        Rect previewRect = new Rect(line.x + third, line.y, third - ButtonGap, line.height);
        Rect clearRect = new Rect(line.x + third * 2f, line.y, third, line.height);

        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        s_PreviewCount = Mathf.Clamp(EditorGUI.IntField(countRect, s_PreviewCount), 0, PreviewMax);
        EditorGUI.indentLevel = indent;

        // boxedValue는 사본이지만 m_Root(오브젝트 참조)와 배치 옵션은 그대로라 미리보기에 충분하다
        if (GUI.Button(previewRect, "배치 미리보기"))
        {
            property.serializedObject.ApplyModifiedProperties();
            (property.boxedValue as UIListViewBase)?.EditorPreview(s_PreviewCount);
        }

        if (GUI.Button(clearRect, "미리보기 지우기"))
            (property.boxedValue as UIListViewBase)?.EditorClearPreview();
    }
}
