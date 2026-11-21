using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

public enum EUIListAxis
{
    Vertical,   // 위에서 아래로 한 줄씩
    Horizontal, // 왼쪽에서 오른쪽으로
    Grid,       // 왼쪽에서 오른쪽, 넘치면 다음 줄
}

// ==================================================

// 목록 칸 묶음. 칸은 UIReuseService로 꺼내 m_Root 아래에 두고, 좌표는 LayoutGroup 없이 Arrange가 직접 계산한다.
// 화면이 [SerializeField]로 들고 배치 옵션(영역·방향·칸 크기·간격…)은 여기 직렬화 필드에 둔다(값은 UI 생성기가 넣는다). 칸 종류·생성 콜백은 Init으로.
// 화면이 떠 있는 동안은 남는 칸을 끄기만 하고, 씬이 바뀌면 UIReuseService가 한꺼번에 회수한다(주인 화면도 씬과 함께 사라진다).
// 흐름: UI가 가공 데이터 리스트를 갖고 Refresh(list, onSet) → 칸은 받은 값만 표시. 클릭은 IndexOf(item)로 UI 리스트에서 찾는다

[Serializable]
public abstract class UIListViewBase
{
    [SerializeField] protected RectTransform m_Root = null;                 // 칸을 놓을 영역
    [SerializeField] private EUIListAxis m_Axis = EUIListAxis.Vertical;
    [SerializeField] private Vector2 m_CellSize = Vector2.zero;           // 칸 크기. 0인 축은 칸 원래 크기(격자는 둘 다 필요)
    [SerializeField] private Vector2 m_Spacing = Vector2.zero;            // 칸 사이(가로, 세로)
    
    [SerializeField] private RectOffset m_Padding = new();                // 영역 안쪽 여백
    [SerializeField] private TextAnchor m_Alignment = TextAnchor.UpperLeft; // 칸 묶음을 영역 어디에 붙일지
    [SerializeField] private bool m_IsStretch = false;                    // 세로 목록은 폭, 가로 목록은 높이를 영역에 맞춘다
    [SerializeField] private bool m_IsFitContent = false;                 // 영역 크기를 칸 묶음에 맞춘다(세로·격자는 높이, 가로는 폭)

    private readonly List<Vector2> m_ListCell = new();         // Arrange 계산용 칸 크기
    protected readonly List<RectTransform> m_ListRect = new(); // 칸 RectTransform. 자식 클래스가 칸 순서대로 채운다

    // 마지막 배치 상태. 같으면 다시 계산하지 않는다
    private bool m_IsDirty = true;
    private int m_ArrangedCount = -1;
    private Vector2 m_ArrangedArea;

    // ------------------------------------------------

    // 칸 크기가 onSet 등에서 바뀌었으면 불러서 다음 Arrange가 다시 계산하게 한다
    public void SetDirty() => m_IsDirty = true;

    // 앞에서 count개 칸의 크기·좌표를 정한다. 칸 앵커는 영역 왼쪽 위로 두고, 피벗은 칸 원래 값을 지킨다(튕김 연출 기준점 유지).
    // 칸 수·영역 크기가 그대로이고 새 칸도 없으면 건너뛴다(isForce면 항상)
    public void Arrange(int rectCnt, bool isForce = false)
    {
        RectTransform area = m_Root;
        if (area == null)
            return;

        rectCnt = Mathf.Min(rectCnt, m_ListRect.Count);
        
        // 사이즈 계산, 전체 재배치가 필요하지 않은 경우는 arrange 하지 않는다. 
        if (isForce == false && m_IsDirty == false && rectCnt == m_ArrangedCount && area.rect.size == m_ArrangedArea)
            return;

        m_ListCell.Clear();

        RectOffset padding = m_Padding;
        Vector2 spacing = m_Spacing;
        EUIListAxis axis = m_Axis;
        
        float innerWidth = area.rect.width - padding.horizontal;
        float innerHeight = area.rect.height - padding.vertical;

        // 1. 칸 크기
        for (int i = 0; i < rectCnt; ++i)
        {
            RectTransform rect = m_ListRect[i];
            m_ListCell.Add(GetCellSize(rect, axis, innerWidth, innerHeight));
        }

        // 2. 칸 묶음 크기(격자는 한 줄에 들어가는 칸 수부터)
        int columnCnt = 1;
        if (axis == EUIListAxis.Grid)
        {
            Vector2 cell = m_CellSize;

            float totalWidth = innerWidth + spacing.x;
            float columnSize = Mathf.Max(1f, cell.x + spacing.x);

            columnCnt = Mathf.Max(1, Mathf.FloorToInt(totalWidth / columnSize)); // !!) rectCnt(칸 수)는 그대로 둔다
        }

        Vector2 content = GetContentSize(rectCnt, axis, columnCnt, spacing);

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
        
        for (int i = 0; i < rectCnt; ++i)
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
                    cellX = startX + i % columnCnt * (cell.x + spacing.x);
                    cellY = startY + i / columnCnt * (cell.y + spacing.y);
                    break;
                }
            }

            Place(m_ListRect[i], cellX, cellY, cell);
        }

        m_IsDirty = false;
        m_ArrangedCount = rectCnt;
        m_ArrangedArea = area.rect.size;
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

        float x = cell.x > 0f ? cell.x : own.x;
        float y = cell.y > 0f ? cell.y : own.y;
        Vector2 size = new Vector2(x, y);
        
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

    #region Editor

#if UNITY_EDITOR
    private const string PreviewName = "__ListPreview";
    private const float PreviewDefaultSize = 100f; // 칸 크기가 0인 축의 미리보기 크기

    // 에디터 전용 배치 미리보기. 영역 아래에 저장되지 않는 가짜 칸 count개를 놓고 같은 계산으로 배치한다(인스펙터 버튼이 부른다)
    public void EditorPreview(int count)
    {
        EditorClearPreview();
        if (m_Root == null)
            return;

        Vector2 size = new Vector2(m_CellSize.x > 0f ? m_CellSize.x : PreviewDefaultSize, m_CellSize.y > 0f ? m_CellSize.y : PreviewDefaultSize);
        for (int i = 0; i < count; ++i)
        {
            GameObject go = new GameObject(PreviewName, typeof(RectTransform));
            go.hideFlags = HideFlags.DontSave; // 씬·프리팹에 저장되지 않는다
            go.transform.SetParent(m_Root, false);

            UnityEngine.UI.Image image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(1f, 0.5f, 0f, 0.5f);
            image.raycastTarget = false;

            RectTransform rt = go.transform as RectTransform;
            if (rt == null)
                continue;

            rt.sizeDelta = size;
            m_ListRect.Add(rt);
        }

        Arrange(count, true);
        m_ListRect.Clear();
    }

    // 미리보기 칸을 지운다
    public void EditorClearPreview()
    {
        if (m_Root == null)
            return;

        for (int i = m_Root.childCount - 1; i >= 0; --i)
        {
            Transform child = m_Root.GetChild(i);
            if (child.name == PreviewName)
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }
#endif

    #endregion
}