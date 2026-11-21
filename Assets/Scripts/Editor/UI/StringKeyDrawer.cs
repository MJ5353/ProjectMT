using System;
using UnityEditor;
using UnityEngine;

// ==================================================

// [StringKey] string 칸을 EStringKey 드롭다운으로 그린다. 값은 enum 이름 문자열로 저장한다(순서를 바꿔도 안 깨진다).
// enum에 없는 이름이 들어 있으면 맨 앞에 "(없는 키) 이름"으로 보여 주고 빨간 글씨로 표시한다

[CustomPropertyDrawer(typeof(StringKeyAttribute))]
public class StringKeyDrawer : PropertyDrawer
{
    private static readonly string[] s_Names = Enum.GetNames(typeof(EStringKey)); // [0]은 None(빈 값)

    // ------------------------------------------------

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        string current = property.stringValue;
        int index = current.E_IsNullOrEmpty() ? 0 : Array.IndexOf(s_Names, current);
        bool isMissing = index < 0;

        string[] options = s_Names;
        if (isMissing)
        {
            options = new string[s_Names.Length + 1];
            options[0] = $"(없는 키) {current}";
            Array.Copy(s_Names, 0, options, 1, s_Names.Length);
            index = 0;
        }

        Color prevColor = GUI.color;
        if (isMissing)
            GUI.color = Color.red;

        EditorGUI.BeginProperty(position, label, property);
        int selected = EditorGUI.Popup(position, label.text, index, options);
        EditorGUI.EndProperty();

        GUI.color = prevColor;

        if (selected == index)
            return;

        string name = options[selected];
        property.stringValue = name == s_Names[0] ? string.Empty : name;
    }
}
