using UnityEngine;

// ==================================================

// UIListView가 칸을 놓는 영역과 배치 값. 목록 칸의 부모에 붙인다(LayoutGroup 대신).
// 좌표 계산은 UIListView.Arrange가 한다. 값은 UI 생성기가 EditorSetup으로 넣는다

public class UIListLayout : MonoBehaviour
{
    public enum EAxis
    {
        Vertical,   // 위에서 아래로 한 줄씩
        Horizontal, // 왼쪽에서 오른쪽으로
        Grid,       // 왼쪽에서 오른쪽, 넘치면 다음 줄
    }

    [SerializeField] private EAxis m_Axis = EAxis.Vertical;
    [SerializeField] private Vector2 m_CellSize = Vector2.zero;         // 칸 크기. 0인 축은 칸 원래 크기(격자는 둘 다 필요)
    [SerializeField] private Vector2 m_Spacing = Vector2.zero;          // 칸 사이(가로, 세로)
    [SerializeField] private RectOffset m_Padding = new();              // 영역 안쪽 여백
    [SerializeField] private TextAnchor m_Alignment = TextAnchor.UpperLeft; // 칸 묶음을 영역 어디에 붙일지
    
    [SerializeField] private bool m_IsStretch = false;                  // 세로 목록은 폭, 가로 목록은 높이를 영역에 맞춘다
    [SerializeField] private bool m_IsFitContent = false;               // 영역 크기를 칸 묶음에 맞춘다(세로·격자는 높이, 가로는 폭)

    public RectTransform Rect => transform as RectTransform;
    public EAxis Axis => m_Axis;
    public Vector2 CellSize => m_CellSize;
    public Vector2 Spacing => m_Spacing;
    public RectOffset Padding => m_Padding;
    public TextAnchor Alignment => m_Alignment;
    public bool IsStretch => m_IsStretch;
    public bool IsFitContent => m_IsFitContent;

    // ------------------------------------------------
    
#if UNITY_EDITOR
    public void EditorSetup(EAxis axis, Vector2 cellSize, Vector2 spacing, RectOffset padding, TextAnchor alignment, bool isStretch, bool isFitContent)
    {
        m_Axis = axis;
        m_CellSize = cellSize;
        m_Spacing = spacing;
        m_Padding = padding ?? new RectOffset();
        m_Alignment = alignment;
        m_IsStretch = isStretch;
        m_IsFitContent = isFitContent;
    }
#endif
}
