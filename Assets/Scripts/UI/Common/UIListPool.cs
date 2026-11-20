using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// ==================================================

// 목록 칸 재사용. 원본(template)을 필요한 만큼 복제해 두고, 쓰는 칸만 켠다

public class UIListPool<T> where T : Component
{
    private readonly T m_Template;
    private readonly Transform m_Parent;
    private readonly Action<T> m_OnCreate; // 새로 만든 칸에 한 번만 (클릭 연결 등)
    private readonly List<T> m_List = new();

    public int Count => m_List.Count;
    public IReadOnlyList<T> List => m_List;

    // ------------------------------------------------

    public UIListPool(T template, Transform parent, Action<T> onCreate = null)
    {
        m_Template = template;
        m_Parent = parent;
        m_OnCreate = onCreate;
    }

    public T Get(int index, bool isActive = true)
    {
        while (index >= m_List.Count)
        {
            T item = Object.Instantiate(m_Template, m_Parent);
            m_OnCreate?.Invoke(item);
            m_List.Add(item);
        }

        T result = m_List[index];
        result.gameObject.SetActive(isActive);
        return result;
    }

    // index부터 끝까지 끈다
    public void HideFrom(int index)
    {
        for (int i = Mathf.Max(0, index); i < m_List.Count; ++i)
        {
            m_List[i].gameObject.SetActive(false);
        }
    }
}
