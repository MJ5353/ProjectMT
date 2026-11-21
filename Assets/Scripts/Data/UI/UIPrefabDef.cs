using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// UI 프리팹 표의 기반. ID(enum) → 프리팹 참조를 들고 있다.
// !!) 목록은 "MushTaker/UI/모든 UI 다시 만들기"가 enum 이름과 같은 프리팹을 찾아 채우므로 직접 고치지 않는다

public abstract class UIPrefabDef<TType, TPrefab> : DataDef where TType : struct, Enum where TPrefab : Component
{
    private const string MissingHint = "MushTaker/UI/모든 UI 다시 만들기를 실행하세요.";

    [Serializable]
    public class Entry
    {
        [field: SerializeField] public TType Type { get; private set; }
        [field: SerializeField] public TPrefab Prefab { get; private set; }

        public Entry(TType type, TPrefab prefab)
        {
            Type = type;
            Prefab = prefab;
        }
    }

    [SerializeField] private List<Entry> m_ListEntry = new(); // 에디터 메뉴가 채워 에셋에 저장한다(readonly면 저장되지 않음)
    private readonly Dictionary<TType, TPrefab> m_DicPrefab = new(); // 런타임 조회용. 처음 찾을 때 채운다

    // ------------------------------------------------

    public bool Has(TType type) => Find(type) != null;

    // 원본 프리팹. 없거나 T가 맞지 않으면 null(에러 로그)
    public T Get<T>(TType type) where T : TPrefab
    {
        TPrefab prefab = Find(type);
        if (prefab == null)
        {
            CLogger.Error(GetType().Name, $"{type} 프리팹이 등록되지 않았습니다. {MissingHint}", this);
            return null;
        }

        T result = prefab as T;
        if (result == null)
            CLogger.Error(GetType().Name, $"{type} 프리팹이 {typeof(T).Name}이 아닙니다.", this);

        return result;
    }

    private TPrefab Find(TType type)
    {
        if (m_DicPrefab.Count == 0)
        {
            foreach (Entry entry in m_ListEntry)
            {
                if (entry != null && entry.Prefab != null)
                    m_DicPrefab[entry.Type] = entry.Prefab;
            }
        }

        return m_DicPrefab.TryGetValue(type, out TPrefab prefab) ? prefab : null;
    }

#if UNITY_EDITOR
    // UISetup 메뉴 전용. 모은 목록으로 통째로 바꾼다
    public void EditorSetList(List<Entry> listEntry)
    {
        m_ListEntry = listEntry;
        m_DicPrefab.Clear();
    }
#endif
}
