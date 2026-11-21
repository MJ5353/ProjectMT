using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 목록 칸 묶음. 칸은 UIFactory로 꺼내 m_Root 아래에 두고, 좌표는 LayoutGroup 없이 Arrange가 직접 계산한다.
// 화면이 [SerializeField]로 들고 배치 옵션(영역·방향·칸 크기·간격…)은 여기 직렬화 필드에 둔다(값은 UI 생성기가 넣는다). 칸 종류·생성 콜백은 Init으로.
// 화면이 떠 있는 동안은 남는 칸을 끄기만 하고, 씬이 바뀌면 UIFactory가 한꺼번에 회수한다(주인 화면도 씬과 함께 사라진다).
// 흐름: UI가 가공 데이터 리스트를 갖고 Refresh(list, onSet) → 칸은 받은 값만 표시. 클릭은 IndexOf(item)로 UI 리스트에서 찾는다

public enum EUIListAxis
{
    Vertical,   // 위에서 아래로 한 줄씩
    Horizontal, // 왼쪽에서 오른쪽으로
    Grid,       // 왼쪽에서 오른쪽, 넘치면 다음 줄
}

[Serializable]
public class UIListView<T> where T : PoolableMonoUI
{
    [SerializeField] private RectTransform m_Root = null;                 // 칸을 놓을 영역
    [SerializeField] private EUIListAxis m_Axis = EUIListAxis.Vertical;
    [SerializeField] private Vector2 m_CellSize = Vector2.zero;           // 칸 크기. 0인 축은 칸 원래 크기(격자는 둘 다 필요)
    [SerializeField] private Vector2 m_Spacing = Vector2.zero;            // 칸 사이(가로, 세로)
    [SerializeField] private RectOffset m_Padding = new();                // 영역 안쪽 여백
    [SerializeField] private TextAnchor m_Alignment = TextAnchor.UpperLeft; // 칸 묶음을 영역 어디에 붙일지
    [SerializeField] private bool m_IsStretch = false;                    // 세로 목록은 폭, 가로 목록은 높이를 영역에 맞춘다
    [SerializeField] private bool m_IsFitContent = false;                 // 영역 크기를 칸 묶음에 맞춘다(세로·격자는 높이, 가로는 폭)

    private EUIComponentType m_Type;
    private Action<T> m_OnSpawn; // 처음 꺼낼 때 한 번(클릭 연결 등. 대입으로 연결해 쌓이지 않게)

    private readonly List<T> m_List = new();
    private readonly List<Vector2> m_ListCell = new(); // Arrange 계산용 칸 크기

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

    // list 수만큼 칸을 켜서 onSet으로 채우고, 남는 칸은 끈다. 채운 뒤 자리를 잡는다(칸 크기가 onSet에서 바뀌어도 맞게)
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

    // index번 칸을 돌려준다. 모자라면 UIFactory로 꺼내 채우고, isActive대로 켜거나 끈다.
    // Refresh가 쓰고, 꺼 둔 채 준비할 때는 직접 부른다(그때는 다 채운 뒤 Arrange를 부른다)
    public T Get(int index, bool isActive = true)
    {
        while (index >= m_List.Count)
        {
            if (m_Root == null)
                return null;

            T item = GameManager.Instance?.UIFactory?.Spawn<T>(m_Type, m_Root);
            if (item == null)
                return null;

            m_OnSpawn?.Invoke(item);
            m_List.Add(item);
        }

        T result = m_List[index];
        result.gameObject.E_SetActive(isActive);
        
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

    // 앞에서 count개 칸의 크기·좌표를 정한다. 칸 앵커는 영역 왼쪽 위로 두고, 피벗은 칸 원래 값을 지킨다(튕김 연출 기준점 유지)
    public void Arrange(int count)
    {
        RectTransform area = m_Root;
        if (area == null)
            return;

        count = Mathf.Min(count, m_List.Count);
        m_ListCell.Clear();

        RectOffset padding = m_Padding;
        Vector2 spacing = m_Spacing;
        EUIListAxis axis = m_Axis;
        float innerWidth = area.rect.width - padding.horizontal;
        float innerHeight = area.rect.height - padding.vertical;

        // 1. 칸 크기
        for (int i = 0; i < count; ++i)
        {
            m_ListCell.Add(GetCellSize(m_List[i].RectTransform, axis, innerWidth, innerHeight));
        }

        // 2. 칸 묶음 크기(격자는 한 줄에 들어가는 칸 수부터)
        int columns = 1;
        if (axis == EUIListAxis.Grid)
        {
            Vector2 cell = m_CellSize;
            columns = Mathf.Max(1, Mathf.FloorToInt((innerWidth + spacing.x) / Mathf.Max(1f, cell.x + spacing.x)));
        }

        Vector2 content = GetContentSize(count, axis, columns, spacing);

        // 3. 영역을 묶음에 맞추거나, 남는 공간에서 정렬 위치를 고른다
        float alignX = (int)m_Alignment % 3 * 0.5f; // 왼쪽 0, 가운데 0.5, 오른쪽 1
        float alignY = (int)m_Alignment / 3 * 0.5f; // 위 0, 가운데 0.5, 아래 1
        if (m_IsFitContent)
        {
            if (axis == EUIListAxis.Horizontal)
            {
                area.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, content.x + padding.horizontal);
                innerWidth = content.x;
            }
            else
            {
                area.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, content.y + padding.vertical);
                innerHeight = content.y;
            }
        }

        float startX = padding.left + (innerWidth - content.x) * alignX;
        float startY = padding.top + (innerHeight - content.y) * alignY;

        // 4. 칸마다 왼쪽 위 모서리를 정하고 놓는다
        float x = startX;
        float y = startY;
        
        for (int i = 0; i < count; ++i)
        {
            Vector2 cell = m_ListCell[i];
            float cellX = x;
            float cellY = y;

            switch (axis)
            {
                case EUIListAxis.Vertical:
                {
                    cellX = startX + (content.x - cell.x) * alignX;
                    y += cell.y + spacing.y;
                    break;
                }
                case EUIListAxis.Horizontal:
                {   
                    cellY = startY + (content.y - cell.y) * alignY;
                    x += cell.x + spacing.x;
                    break;
                }
                case EUIListAxis.Grid:
                {
                    cellX = startX + i % columns * (cell.x + spacing.x);
                    cellY = startY + i / columns * (cell.y + spacing.y);
                    break;
                }
            }

            Place(m_List[i].RectTransform, cellX, cellY, cell);
        }
    }

    // 씬 전환 전에 일부러 돌려줄 때만 쓴다(보통은 UIFactory가 씬 전환 때 회수한다)
    public void Clear()
    {
        UIFactory factory = GameManager.Instance?.UIFactory;
        foreach (T item in m_List)
        {
            if (factory != null)
                factory.Despawn(item);
            else
                ResourceManager.S_Despawn(item);
        }

        m_List.Clear();
    }

    // arrange ---

    private Vector2 GetCellSize(RectTransform rt, EUIListAxis axis, float innerWidth, float innerHeight)
    {
        Vector2 cell = m_CellSize;
        if (axis == EUIListAxis.Grid)
            return cell;

        Vector2 own = Vector2.zero;
        if (rt != null)
            own = rt.rect.size;

        Vector2 size = new Vector2(cell.x > 0f ? cell.x : own.x, cell.y > 0f ? cell.y : own.y);
        
        if (m_IsStretch && axis == EUIListAxis.Vertical)
            size.x = innerWidth;
        else if (m_IsStretch && axis == EUIListAxis.Horizontal)
            size.y = innerHeight;

        return size;
    }

    private Vector2 GetContentSize(int count, EUIListAxis axis, int columns, Vector2 spacing)
    {
        if (count <= 0)
            return Vector2.zero;

        if (axis == EUIListAxis.Grid)
        {
            Vector2 cell = m_CellSize;
            int usedColumns = Mathf.Min(count, columns);
            int rows = (count + columns - 1) / columns;
            
            return new Vector2(usedColumns * cell.x + (usedColumns - 1) * spacing.x, rows * cell.y + (rows - 1) * spacing.y);
        }

        Vector2 content = Vector2.zero;
        foreach (Vector2 cell in m_ListCell)
        {
            if (axis == EUIListAxis.Vertical)
            {
                content.x = Mathf.Max(content.x, cell.x);
                content.y += cell.y;
            }
            else
            {
                content.x += cell.x;
                content.y = Mathf.Max(content.y, cell.y);
            }
        }

        if (axis == EUIListAxis.Vertical)
            content.y += (count - 1) * spacing.y;
        else
            content.x += (count - 1) * spacing.x;

        return content;
    }

    // 왼쪽 위 모서리(x, 아래로 y)에 칸을 놓는다. 피벗만큼 옮겨 앵커 기준 좌표로 바꾼다
    private static void Place(RectTransform rt, float x, float y, Vector2 cell)
    {
        if (rt == null)
            return;

        Vector2 topLeft = new Vector2(0f, 1f);
        rt.anchorMin = topLeft;
        rt.anchorMax = topLeft;
        rt.sizeDelta = cell;
        rt.anchoredPosition = new Vector2(x + cell.x * rt.pivot.x, -y - cell.y * (1f - rt.pivot.y));
    }
}
