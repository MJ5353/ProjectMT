using UnityEngine;

// 노치·펀치홀을 피해 자식 UI를 안전 영역 안에 맞춘다. 캔버스 바로 아래 전체 화면 RectTransform에 붙인다
[RequireComponent(typeof(RectTransform))]
public class UISafeArea : MonoBehaviour
{
    private RectTransform m_Rect;
    private Rect m_LastSafeArea;
    private Vector2Int m_LastScreen;

    // ==================================================

    private void Awake()
    {
        m_Rect = (RectTransform)transform;
        Apply();
    }

    private void Update()
    {
        // 회전(가로 좌우 뒤집힘)하면 안전 영역이 바뀐다
        if (m_LastSafeArea == Screen.safeArea && m_LastScreen.x == Screen.width && m_LastScreen.y == Screen.height)
            return;

        Apply();
    }

    private void Apply()
    {
        Rect safeArea = Screen.safeArea;
        m_LastSafeArea = safeArea;
        m_LastScreen = new Vector2Int(Screen.width, Screen.height);

        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        Vector2 min = safeArea.position;
        Vector2 max = safeArea.position + safeArea.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        m_Rect.anchorMin = min;
        m_Rect.anchorMax = max;
        m_Rect.offsetMin = Vector2.zero;
        m_Rect.offsetMax = Vector2.zero;
    }
}
