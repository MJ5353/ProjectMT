using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// ==================================================

// [SerializeReference, SubclassSelector] 칸 오른쪽에 하위 클래스 드롭다운을 그린다. 고르면 새 인스턴스로 바꾼다(목록 +로 늘린 원소가 앞 원소를 같이 가리키는 것도 이걸로 끊긴다)
[CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
public class SubclassSelectorDrawer : PropertyDrawer
{
    private const string NoneLabel = "(없음)";

    private static readonly Dictionary<Type, Type[]> s_DicTypes = new(); // 기반 타입 → 고를 수 있는 타입

    // ------------------------------------------------

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        EditorGUI.BeginProperty(position, label, property);

        float labelWidth = EditorGUIUtility.labelWidth;
        Rect buttonRect = new(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
        object value = property.managedReferenceValue;
        if (EditorGUI.DropdownButton(buttonRect, new GUIContent(GetLabel(value?.GetType())), FocusType.Keyboard))
            ShowMenu(property);

        EditorGUI.PropertyField(position, property, label, true);
        EditorGUI.EndProperty();
    }

    // ==================================================

    private static void ShowMenu(SerializedProperty property)
    {
        SerializedObject serializedObject = property.serializedObject;
        string path = property.propertyPath;
        Type baseType = GetBaseType(property.managedReferenceFieldTypename);

        GenericMenu menu = new();
        menu.AddItem(new GUIContent(NoneLabel), false, () => SetValue(serializedObject, path, null));
        foreach (Type type in GetTypes(baseType))
        {
            menu.AddItem(new GUIContent(GetLabel(type)), false, () => SetValue(serializedObject, path, Activator.CreateInstance(type, true)));
        }

        menu.ShowAsContext();
    }

    private static void SetValue(SerializedObject serializedObject, string path, object value)
    {
        serializedObject.Update();
        SerializedProperty property = serializedObject.FindProperty(path);
        if (property == null)
            return;

        property.managedReferenceValue = value;
        property.isExpanded = value != null;
        serializedObject.ApplyModifiedProperties();
    }

    // "어셈블리 타입이름" → Type
    private static Type GetBaseType(string fieldTypename)
    {
        if (string.IsNullOrEmpty(fieldTypename))
            return null;

        string[] parts = fieldTypename.Split(' ');
        return parts.Length == 2 ? Type.GetType($"{parts[1]}, {parts[0]}") : null;
    }

    private static Type[] GetTypes(Type baseType)
    {
        if (baseType == null)
            return Array.Empty<Type>();

        if (s_DicTypes.TryGetValue(baseType, out Type[] types) == false)
        {
            types = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(type => type.IsAbstract == false && type.IsGenericType == false && type.IsSerializable)
                .OrderBy(GetLabel)
                .ToArray();
            s_DicTypes[baseType] = types;
        }

        return types;
    }

    private static string GetLabel(Type type)
    {
        if (type == null)
            return NoneLabel;

        SubclassLabelAttribute attribute = type.GetCustomAttribute<SubclassLabelAttribute>();
        return attribute != null ? attribute.Label : type.Name;
    }
}
