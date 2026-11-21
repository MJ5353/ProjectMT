using System;
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

    private static void Require(bool condition, string message)
    {
        if (condition == false)
            throw new InvalidOperationException("SerializableDictionaryChecks: " + message);
    }
}
