using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

// ==================================================

[CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
public sealed class SerializableDictionaryDrawer : PropertyDrawer
{
    private ReorderableList m_ListEntry = null;
    private SerializedObject m_SerializedObject = null;
    private string m_PropertyPath = null;

    // ------------------------------------------------

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        SerializedProperty dictionary = property.Copy();
        return new IMGUIContainer(() =>
        {
            dictionary.serializedObject.UpdateIfRequiredOrScript();
            Rect rect = EditorGUILayout.GetControlRect(false, GetPropertyHeight(dictionary, GUIContent.none));
            OnGUI(rect, dictionary, new GUIContent(dictionary.displayName));
            dictionary.serializedObject.ApplyModifiedProperties();
        });
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        SerializedProperty entries = property.FindPropertyRelative("m_ListEntry");
        Rect header = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(header, property.isExpanded, new GUIContent($"{label.text} ({entries.arraySize})"), true);
        if (property.isExpanded)
        {
            position.y += header.height + EditorGUIUtility.standardVerticalSpacing;
            ReorderableList list = GetList(property);
            position.height = list.GetHeight();
            list.DoList(position);

            string warning = GetWarning(entries);
            if (string.IsNullOrEmpty(warning) == false)
            {
                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                position.height = 40f;
                EditorGUI.HelpBox(position, warning, MessageType.Warning);
            }
        }
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (property.isExpanded == false)
            return height;

        height += EditorGUIUtility.standardVerticalSpacing + GetList(property).GetHeight();
        if (string.IsNullOrEmpty(GetWarning(property.FindPropertyRelative("m_ListEntry"))) == false)
            height += EditorGUIUtility.standardVerticalSpacing + 40f;

        return height;
    }

    // ------------------------------------------------

    private ReorderableList GetList(SerializedProperty property)
    {
        if (m_ListEntry != null && m_SerializedObject == property.serializedObject && m_PropertyPath == property.propertyPath)
            return m_ListEntry;

        m_SerializedObject = property.serializedObject;
        m_PropertyPath = property.propertyPath;
        SerializedProperty entries = property.FindPropertyRelative("m_ListEntry");
        m_ListEntry = new ReorderableList(m_SerializedObject, entries, true, true, true, true);
        m_ListEntry.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width * 0.4f, rect.height), "키");
            EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.4f + 4f, rect.y, rect.width * 0.6f - 4f, rect.height), "값");
        };
        m_ListEntry.elementHeightCallback = index =>
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            return Mathf.Max(EditorGUI.GetPropertyHeight(entry.FindPropertyRelative("Key"), true), EditorGUI.GetPropertyHeight(entry.FindPropertyRelative("Value"), true)) + 4f;
        };
        m_ListEntry.drawElementCallback = (rect, index, active, focused) =>
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            rect.y += 2f;
            rect.height -= 4f;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width * 0.4f, rect.height), entry.FindPropertyRelative("Key"), GUIContent.none, true);
            EditorGUI.PropertyField(new Rect(rect.x + rect.width * 0.4f + 4f, rect.y, rect.width * 0.6f - 4f, rect.height), entry.FindPropertyRelative("Value"), GUIContent.none, true);
        };
        m_ListEntry.onAddCallback = list => AddEntry(list.serializedProperty);
        return m_ListEntry;
    }

    internal static void AddEntry(SerializedProperty entries)
    {
        entries.arraySize++;
        SerializedProperty entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
        SerializedProperty key = entry.FindPropertyRelative("Key");
        SerializedProperty value = entry.FindPropertyRelative("Value");
        if (value.propertyType == SerializedPropertyType.ObjectReference)
            value.objectReferenceValue = null;

        if (key.propertyType != SerializedPropertyType.Enum)
            return;

        HashSet<int> setKey = new HashSet<int>();
        for (int i = 0; i < entries.arraySize - 1; i++)
        {
            setKey.Add(entries.GetArrayElementAtIndex(i).FindPropertyRelative("Key").intValue);
        }

        for (int i = 0; i < key.enumNames.Length; i++)
        {
            key.enumValueIndex = i;
            if (setKey.Contains(key.intValue) == false)
                return;
        }
    }

    internal static string GetWarning(SerializedProperty entries)
    {
        HashSet<object> setKey = new HashSet<object>();
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty key = entries.GetArrayElementAtIndex(i).FindPropertyRelative("Key");
            if (key.boxedValue == null || key.propertyType == SerializedPropertyType.ObjectReference && key.objectReferenceValue == null)
                return "null 키는 런타임 딕셔너리에서 제외됩니다.";

            if (setKey.Add(key.boxedValue) == false)
                return "중복 키가 있습니다. 런타임에서는 마지막 행의 값을 사용합니다.";
        }

        return null;
    }
}
