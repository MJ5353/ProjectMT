using System.Collections;
using UnityEngine;

// ==================================================

// 팝업 기반. 프리팹으로 만들고 PopupService.Open으로 띄운다.
// 가운데 창 팝업은 m_Group(전체 페이드)·m_Window(창 튕김)를 연결하면 열 때 공용 연출이 돈다. 비워 두면 연출 없이 바로 보인다

public class Popup : MonoBehaviour
{
    private const float OpenStartScale = 0.9f;

    [SerializeField] private CanvasGroup m_Group = null;      // 비어도 된다
    [SerializeField] private RectTransform m_Window = null;   // 비어도 된다
    [SerializeField] private float m_FadeTime = 0.25f;

    // 뒤로 가기(Esc, 안드로이드 뒤로)로 닫을 수 있는지
    public virtual bool IsCloseOnBack => true;

    // ------------------------------------------------

    public void Close() => GameManager.Instance?.PopupS?.Close(this);

    // PopupService.Open만 부른다. 자식 내용 채우기(OnOpen) 뒤 공용 여는 연출을 돌린다
    public void Open()
    {
        OnOpen();
        SetOpen(0f, OpenStartScale);
        StartCoroutine(CoOpen());
    }

    // 여는 연출: 페이드 + 창 0.9 → 1 튕김, 끝나면 CoAfterOpen(팝업별 이어지는 연출).
    // !!) 시장·전투 종료 중에는 timeScale이 멈춰 있을 수 있어 unscaled 시간으로 돈다
    private IEnumerator CoOpen()
    {
        if (m_Group != null || m_Window != null)
        {
            for (float time = 0f; time < m_FadeTime; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(time / m_FadeTime);
                SetOpen(t, Mathf.Lerp(OpenStartScale, 1f, UITween.EaseOutBack(t)));
                yield return null;
            }

            SetOpen(1f, 1f);
        }

        yield return CoAfterOpen();
    }

    private void SetOpen(float alpha, float scale)
    {
        if (m_Group != null)
            m_Group.alpha = alpha;
        if (m_Window != null)
            m_Window.localScale = Vector3.one * scale;
    }

    // virtual ---

    // 열 때 내용 채우기. base를 부를 필요 없다
    protected virtual void OnOpen() { }

    // 창이 다 열린 뒤 이어지는 연출(결과 칸 튀어나오기 등)
    protected virtual IEnumerator CoAfterOpen()
    {
        yield break;
    }

    public virtual void OnClose() { }
}
