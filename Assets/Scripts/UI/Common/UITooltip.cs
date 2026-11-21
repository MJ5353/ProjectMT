using System.Collections;
using TMPro;
using UnityEngine;

// ==================================================

// 대상 칸 위에 뜨는 이름 말풍선. 목록 하나에 하나만 두고 돌려 쓴다(다른 칸을 누르면 그 칸으로 옮겨 다시 뜬다).
// 같은 칸을 다시 누르면 닫힌다. 피벗은 아래 가운데로 두고 대상 윗변 가운데에 붙인다

public class UITooltip : MonoBehaviour
{
    [SerializeField] private CanvasGroup m_Group = null;
    [SerializeField] private TMP_Text m_Label = null;

    #region const
    private const float Gap = 8f;           // 대상 윗변과의 간격
    private const float ShowTime = 0.18f;
    private const float ShowStartScale = 0.6f;
    private const float ShowRise = 12f;     // 아래에서 떠오르는 거리
    #endregion

    private RectTransform m_Rect;
    private RectTransform m_Target;
    private Coroutine m_Routine;

    // ------------------------------------------------

    private void Awake()
    {
        m_Rect = transform as RectTransform;
    }

    // 같은 대상이 이미 떠 있으면 닫는다
    public void Toggle(RectTransform target, string text)
    {
        if (m_Target == target && gameObject.activeSelf)
        {
            Hide();
            return;
        }

        Show(target, text);
    }

    public void Show(RectTransform target, string text)
    {
        if (m_Rect == null || target == null)
            return;

        m_Target = target;
        m_Label.text = text;
        gameObject.SetActive(true);

        if (m_Routine != null)
            StopCoroutine(m_Routine);
        
        m_Routine = StartCoroutine(CoShow(GetAnchorPosition(target)));
    }

    public void Hide()
    {
        m_Target = null;
        m_Routine = null; // 꺼지면 코루틴도 멈춘다
        gameObject.SetActive(false);
    }

    // ==================================================

    // 대상 윗변 가운데를 이 말풍선 부모 좌표로
    private Vector2 GetAnchorPosition(RectTransform target)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;

        m_Rect.position = topCenter;
        return m_Rect.anchoredPosition + new Vector2(0f, Gap);
    }

    // 페이드 + 0.6 → 1 튕김 + 살짝 떠오름. 팝업과 같이 unscaled 시간으로 돈다
    private IEnumerator CoShow(Vector2 position)
    {
        yield return UITween.CoPlay(ShowTime, t => SetShow(m_Rect, position, t, UITween.EaseOutBack(t), UITween.EaseOutCubic(t)));
        m_Routine = null;
    }

    private void SetShow(RectTransform rt, Vector2 position, float alpha, float scaleT, float riseT)
    {
        m_Group.alpha = alpha;
        rt.localScale = Vector3.one * Mathf.LerpUnclamped(ShowStartScale, 1f, scaleT);
        rt.anchoredPosition = position + new Vector2(0f, Mathf.Lerp(-ShowRise, 0f, riseT));
    }

#if UNITY_EDITOR
    public void EditorSetup(CanvasGroup group, TMP_Text label)
    {
        m_Group = group;
        m_Label = label;
    }
#endif
}
