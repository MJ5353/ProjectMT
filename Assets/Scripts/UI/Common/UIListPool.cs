using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 목록 칸 재사용. 칸은 ResourceManager 풀에서 꺼내 parent 아래에 둔다.
// 화면이 떠 있는 동안은 남는 칸을 끄기만 하고, 풀 반환은 Clear(팝업 OnClose·씬 정리)에서만 한다.
// 흐름: UI가 가공 데이터 리스트를 갖고 Refresh(list, onSet) → 칸은 받은 값만 표시. 클릭은 IndexOf(slot)로 UI 리스트에서 찾는다

public class UIListPool<T> where T : PoolableMonoUI
{
    private readonly T m_Prefab;
    private readonly Transform m_Parent;
    private readonly Action<T> m_OnSpawn; // 풀에서 꺼낼 때마다 (클릭 연결 등. 대입으로 연결해 쌓이지 않게)
    private readonly List<T> m_List = new();

    public int Count => m_List.Count;
    public IReadOnlyList<T> List => m_List;

    // ------------------------------------------------

    public UIListPool(T prefab, Transform parent, Action<T> onSpawn = null)
    {
        m_Prefab = prefab;
        m_Parent = parent;
        m_OnSpawn = onSpawn;
    }

    // list 수만큼 칸을 켜서 onSet으로 채우고, 남는 칸은 끈다
    public void Refresh<TData>(IReadOnlyList<TData> list, Action<T, TData> onSet)
    {
        int count = list != null ? list.Count : 0;
        for (int i = 0; i < count; ++i)
        {
            T item = Get(i);
            if (item != null)
                onSet?.Invoke(item, list[i]);
        }

        HideFrom(count);
    }

    // 결과 화면 연출처럼 칸을 하나씩 다룰 때. 모자라면 풀에서 꺼낸다
    public T Get(int index, bool isActive = true)
    {
        while (index >= m_List.Count)
        {
            T item = ResourceManager.Instance?.Spawn(m_Prefab, m_Parent);
            if (item == null)
                return null;

            m_OnSpawn?.Invoke(item);
            m_List.Add(item);
        }

        T result = m_List[index];
        result.gameObject.SetActive(isActive);
        return result;
    }

    // 클릭된 칸의 순서. 없으면 -1
    public int IndexOf(T item) => m_List.IndexOf(item);

    // index부터 끝까지 끈다
    public void HideFrom(int index)
    {
        for (int i = Mathf.Max(0, index); i < m_List.Count; ++i)
        {
            m_List[i].gameObject.SetActive(false);
        }
    }

    // 모든 칸을 풀로 돌려보낸다
    public void Clear()
    {
        foreach (T item in m_List)
        {
            ResourceManager.S_Despawn(item);
        }

        m_List.Clear();
    }
}
