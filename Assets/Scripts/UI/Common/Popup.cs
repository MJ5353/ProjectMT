using System.Collections;
using UnityEngine;

// ==================================================

// 팝업 기반. 팝업 ID(EPopupType)와 경로를 PopupTable에 등록하고 PopupService.Open<T>(EPopupType)로 띄운다.
// 가운데 창 팝업은 m_Group(전체 페이드)·m_Window(창 튕김)를 연결하면 열고 닫을 때 공용 연출이 돈다. 비워 두면 연출 없이 바로 보이고 사라진다.
// 진입은 PopupService만 부른다: Open(열기) / Dismiss(닫기). 팝업별 처리는 OnOpen·OnClose를 override한다.
// 닫으면 끄기만 하고 PopupService가 들고 있다가 같은 팝업을 다시 열 때 다시 쓴다(씬이 바뀔 때만 지운다). 그래서 OnOpen이 상태를 처음으로 되돌린다

public class Popup : MonoBehaviour
{
    [SerializeField] private CanvasGroup m_Group = null;      // 비어도 된다
    [SerializeField] private RectTransform m_Window = null;   // 비어도 된다

    private const float OpenStartScale = 0.9f;
    private const float OpenTime = 0.25f;
    private const float CloseTime = 0.15f;

    private bool m_IsClosing;  // 닫는 연출 중
    private bool m_IsFinished; // OnClose까지 끝나 꺼져 있다

    public virtual bool IsCloseOnBack => true;  // 뒤로 가기(Esc, 안드로이드 뒤로)로 닫을 수 있는지
    private bool HasTween => m_Group != null || m_Window != null;

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
