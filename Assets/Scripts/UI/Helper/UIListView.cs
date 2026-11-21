using System;
using System.Collections.Generic;

// ==================================================

// 칸 종류(T)를 아는 쪽. 꺼내기·끄기·돌려주기를 맡는다
[Serializable]
public class UIListView<T> : UIListViewBase where T : PoolableMonoUI
{
    private EUIComponentType m_Type;
    private Action<T> m_OnSpawn; // 처음 꺼낼 때 한 번(클릭 연결 등. 대입으로 연결해 쌓이지 않게)
    private readonly List<T> m_List = new();

    public int Count => m_List.Count;
    public IReadOnlyList<T> List => m_List;

    // ------------------------------------------------

    // 클릭된 칸의 순서. 없으면 -1
    public int IndexOf(T item) => m_List.IndexOf(item);
    
    // 화면 Awake에서 한 번 부른다
    public void Init(EUIComponentType type, Action<T> onSpawn = null)
    {
        m_Type = type;
        m_OnSpawn = onSpawn;
    }

    // list 수만큼 칸을 켜서 onSet으로 채우고, 남는 칸은 끈다. 채운 뒤 자리를 잡는다
    public void Refresh<TData>(IReadOnlyList<TData> list, Action<T, TData> onSet)
    {
        int count = list?.Count ?? 0;
        for (int i = 0; i < count; ++i)
        {
            T item = Get(i);
            if (item != null)
                onSet?.Invoke(item, list.E_GetOrDefault(i));
        }

        HideFrom(count);
        Arrange(count);
    }

    // index번 칸을 돌려준다. 모자라면 UIReuseService로 꺼내 채우고, isActive대로 켜거나 끈다.
    // Refresh가 쓰고, 꺼 둔 채 준비할 때는 직접 부른다(그때는 다 채운 뒤 Arrange를 부른다)
    public T Get(int index, bool isActive = true)
    {
        while (index >= m_List.Count)
        {
            if (m_Root == null)
                return null;

            T item = GameManager.Instance.UIReuseS?.Spawn<T>(m_Type, m_Root);
            if (item == null)
                return null;

            m_OnSpawn?.Invoke(item);
            m_List.Add(item);
            m_ListRect.Add(item.RectTransform);
            
            SetDirty(); // 새 칸은 아직 자리가 없다
        }

        T result = m_List[index];
        result.gameObject.E_SetActive(isActive);
        
        return result;
    }

    // index부터 끝까지 끈다
    public void HideFrom(int index)
    {
        foreach (T item in m_List)
        {
            item.E_SetActive(false);
        }
    }

    // 씬 전환 전에 일부러 돌려줄 때만 쓴다(보통은 UIReuseService가 씬 전환 때 회수한다)
    public void Clear()
    {
        UIReuseService reuseService = GameManager.Instance.UIReuseS;
        foreach (T item in m_List)
        {
            if (reuseService != null)
                reuseService.Despawn(item);
            else
                ResourceManager.S_Despawn(item);
        }

        m_List.Clear();
        m_ListRect.Clear();
        SetDirty();
    }
}
