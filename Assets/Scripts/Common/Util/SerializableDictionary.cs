using System;
using UnityEngine;
using System.Collections.Generic;

// ==================================================

[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [Serializable]
    private struct Entry
    {
        public TKey Key;
        public TValue Value;
    }

    [SerializeField] private List<Entry> m_ListEntry = new List<Entry>();
    [NonSerialized] private Dictionary<TKey, TValue> m_DicSerialized = null;

    // ------------------------------------------------

    public void OnBeforeSerialize()
    {
        // Inspector의 중복 키·미완성 행은 런타임 값이 바뀔 때까지 보존한다.
        if (HasChanges() == false)
            return;

        m_ListEntry.Clear();
        foreach (var pair in this)
        {
            m_ListEntry.Add(new Entry { Key = pair.Key, Value = pair.Value });
        }

        m_DicSerialized = new Dictionary<TKey, TValue>(this);
    }

    public void OnAfterDeserialize()
    {
        Clear();
        foreach (Entry entry in m_ListEntry)
        {
            if (entry.Key == null)
                continue;

            // 중복 키는 마지막 값을 사용한다.
            this[entry.Key] = entry.Value;
        }

        m_DicSerialized = new Dictionary<TKey, TValue>(this);
    }

    // ------------------------------------------------

    private bool HasChanges()
    {
        if (m_DicSerialized == null || Count != m_DicSerialized.Count)
            return true;

        foreach (var pair in this)
        {
            if (m_DicSerialized.TryGetValue(pair.Key, out TValue value) == false || EqualityComparer<TValue>.Default.Equals(pair.Value, value) == false)
                return true;
        }

        return false;
    }
}
