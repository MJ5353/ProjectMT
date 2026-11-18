using System;
using UnityEditor;
using UnityEngine;

// ==================================================

public static class SerializableDictionaryChecks
{
    [Serializable]
    private sealed class CheckData
    {
        public SerializableDictionary<string, int> DicValue = new SerializableDictionary<string, int>();
    }

    // ------------------------------------------------

    public static void Run()
    {
        CheckData source = new CheckData();
        source.DicValue.Add("First", 1);
        source.DicValue["Second"] = 2;
        string json = JsonUtility.ToJson(source);
        CheckData restored = JsonUtility.FromJson<CheckData>(json);
        Require(restored.DicValue.Count == 2 && restored.DicValue["First"] == 1 && restored.DicValue["Second"] == 2, "직렬화 왕복 실패");

        restored.DicValue.Remove("First");
        restored = JsonUtility.FromJson<CheckData>(JsonUtility.ToJson(restored));
        Require(restored.DicValue.Count == 1 && restored.DicValue.TryGetValue("Second", out int value) && value == 2, "삭제 후 직렬화 실패");

        restored.DicValue.Clear();
        restored = JsonUtility.FromJson<CheckData>(JsonUtility.ToJson(restored));
        Require(restored.DicValue.Count == 0, "빈 딕셔너리 복원 실패");

        CheckData duplicate = JsonUtility.FromJson<CheckData>("{\"DicValue\":{\"m_ListEntry\":[{\"Key\":\"Same\",\"Value\":1},{\"Key\":\"Same\",\"Value\":2}]}}");
        Require(duplicate.DicValue.Count == 1 && duplicate.DicValue["Same"] == 2, "중복 키 복원 실패");
        Require(JsonUtility.ToJson(duplicate).Contains("\"Value\":1"), "Inspector의 중복 행이 직렬화 중 사라짐");
        duplicate.DicValue["Same"] = 3;
        duplicate = JsonUtility.FromJson<CheckData>(JsonUtility.ToJson(duplicate));
        Require(duplicate.DicValue.Count == 1 && duplicate.DicValue["Same"] == 3, "런타임 수정 반영 실패");

        SerializableDictionary<GameObject, int> nullKey = JsonUtility.FromJson<SerializableDictionary<GameObject, int>>("{\"m_ListEntry\":[{\"Value\":1}]}");
        Require(nullKey.Count == 0, "null 키 제외 실패");

        SerializableDictionary<KeyCode, Vector3> typed = new SerializableDictionary<KeyCode, Vector3>();
        typed.Add(KeyCode.Space, Vector3.one);
        typed = JsonUtility.FromJson<SerializableDictionary<KeyCode, Vector3>>(JsonUtility.ToJson(typed));
        Require(typed.Count == 1 && typed[KeyCode.Space] == Vector3.one, "열거형 키·구조체 값 복원 실패");

        Debug.Log("SerializableDictionaryChecks 통과: 직렬화 왕복·삭제·빈 목록·중복 키·null 키·열거형 키·구조체 값");
    }

    // ------------------------------------------------

    public static void RunInspector()
    {
        string path = "Assets/SerializableDictionaryCheck_" + Guid.NewGuid().ToString("N");
        string prefabPath = path + ".prefab";
        string defPath = path + ".asset";
        GameObject root = new GameObject("DictionaryCheck");
        GameObject head = new GameObject("Head");
        head.transform.SetParent(root.transform);
        MonsterDef def = null;
        try
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Transform bone = prefab.transform.Find("Head");
            def = ScriptableObject.CreateInstance<MonsterDef>();
            AssetDatabase.CreateAsset(def, defPath);
            using (SerializedObject serialized = new SerializedObject(def))
            {
                SerializedProperty dictionary = serialized.FindProperty("<DicBodyBone>k__BackingField");
                Require(dictionary != null, "MonsterDef의 딕셔너리 필드 없음");
                dictionary.isExpanded = true;
                SerializableDictionaryDrawer drawer = new SerializableDictionaryDrawer();
                Require(drawer.GetPropertyHeight(dictionary, GUIContent.none) > EditorGUIUtility.singleLineHeight && drawer.CreatePropertyGUI(dictionary) != null, "Inspector 생성 실패");

                SerializedProperty entries = dictionary.FindPropertyRelative("m_ListEntry");
                SerializableDictionaryDrawer.AddEntry(entries);
                entries.GetArrayElementAtIndex(0).FindPropertyRelative("Key").intValue = (int)EBodyPart.Head;
                entries.GetArrayElementAtIndex(0).FindPropertyRelative("Value").objectReferenceValue = bone;
                serialized.ApplyModifiedProperties();
                Require(def.DicBodyBone.Count == 1 && def.DicBodyBone[EBodyPart.Head] == bone, "Inspector 편집 반영 실패");

                Undo.IncrementCurrentGroup();
                SerializableDictionaryDrawer.AddEntry(entries);
                Require(entries.GetArrayElementAtIndex(1).FindPropertyRelative("Key").intValue != (int)EBodyPart.Head, "사용 중인 enum 키 자동 선택");
                Require(entries.GetArrayElementAtIndex(1).FindPropertyRelative("Value").objectReferenceValue == null, "추가한 Transform 값이 이전 행을 복제함");
                entries.GetArrayElementAtIndex(1).FindPropertyRelative("Key").intValue = (int)EBodyPart.Tail;
                serialized.ApplyModifiedProperties();
                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                serialized.Update();
                Require(entries.arraySize == 1 && def.DicBodyBone.Count == 1, "행 추가 Undo 실패");
                Undo.PerformRedo();
                serialized.Update();
                Require(entries.arraySize == 2 && def.DicBodyBone.ContainsKey(EBodyPart.Tail), "행 추가 Redo 실패");

                SerializableDictionaryDrawer.AddEntry(entries);
                entries.GetArrayElementAtIndex(2).FindPropertyRelative("Key").intValue = (int)EBodyPart.Head;
                serialized.ApplyModifiedProperties();
                Require(string.IsNullOrEmpty(SerializableDictionaryDrawer.GetWarning(entries)) == false, "중복 키 경고 없음");
                EditorJsonUtility.ToJson(def);
                serialized.Update();
                Require(entries.arraySize == 3 && def.DicBodyBone.Count == 2, "중복 행 보존 실패");
                entries.GetArrayElementAtIndex(2).FindPropertyRelative("Key").intValue = (int)EBodyPart.Neck;
                serialized.ApplyModifiedProperties();
                Require(def.DicBodyBone.Count == 3 && string.IsNullOrEmpty(SerializableDictionaryDrawer.GetWarning(entries)), "중복 키 수정 실패");

                entries.MoveArrayElement(2, 0);
                serialized.ApplyModifiedProperties();
                entries.DeleteArrayElementAtIndex(0);
                serialized.ApplyModifiedProperties();
                Require(def.DicBodyBone.Count == 2 && def.DicBodyBone.ContainsKey(EBodyPart.Neck) == false, "순서 변경·삭제 반영 실패");
            }

            Undo.ClearUndo(def);
            AssetDatabase.SaveAssetIfDirty(def);
            Resources.UnloadAsset(def);
            def = AssetDatabase.LoadAssetAtPath<MonsterDef>(defPath);
            Require(def.DicBodyBone.Count == 2 && def.DicBodyBone[EBodyPart.Head] == bone && def.DicBodyBone[EBodyPart.Tail] == null, "Def 저장·재로드 후 Transform 참조 유지 실패");
            Debug.Log("SerializableDictionary Inspector 검사 통과: enum·Transform 편집·추가·중복 행 보존·Undo/Redo·정렬·삭제·에셋 재로드");
        }
        finally
        {
            if (def != null)
                Undo.ClearUndo(def);

            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.DeleteAsset(defPath);
            AssetDatabase.DeleteAsset(prefabPath);
        }
    }

    // ------------------------------------------------

    private static void Require(bool condition, string message)
    {
        if (condition == false)
            throw new InvalidOperationException("SerializableDictionaryChecks: " + message);
    }
}
