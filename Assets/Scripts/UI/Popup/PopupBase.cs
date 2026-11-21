using UnityEngine;

// ==================================================

// 팝업 기반. 팝업 ID(EPopupType)에 같은 이름의 프리팹을 만들면 PopupDef에 자동 등록되고, PopupService.Open<T>(EPopupType)로 띄운다.
// 가운데 창 팝업은 m_Group(전체 페이드)·m_Window(창 튕김)를 연결하면 열고 닫을 때 공용 연출이 돈다. 비워 두면 연출 없이 바로 보이고 사라진다.
// 진입은 PopupService만 부른다: Open(열기) / BeginClose(닫기 시작, 끝나면 EndClose). 팝업별 처리는 OnOpen·OnClose를 override한다.
// 연출은 UISequence. 창이 열린 뒤 이어지는 팝업별 연출은 OnBuildOpenSequence에 단계를 덧붙이고, SkipOpen으로 끝 모습으로 넘길 수 있다.
// 닫으면 끄기만 하고 PopupService가 들고 있다가 같은 팝업을 다시 열 때 다시 쓴다(씬이 바뀔 때만 지운다). 그래서 OnOpen이 상태를 처음으로 되돌린다

public class PopupBase : MonoBehaviour
{
    [SerializeField] private CanvasGroup m_Group = null;      // 비어도 된다
    [SerializeField] private RectTransform m_Window = null;   // 비어도 된다

    private const float OpenStartScale = 0.9f;
    private const float OpenTime = 0.25f;
    private const float CloseTime = 0.15f;

    private readonly UISequence m_OpenSequence = new();
    private readonly UISequence m_CloseSequence = new();
    
    private bool m_IsClosing;  // 닫는 연출 중
    private bool m_IsFinished; // OnClose까지 끝나 꺼져 있다

    public virtual bool IsCloseOnBack => true;  // 뒤로 가기(Esc, 안드로이드 뒤로)로 닫을 수 있는지
    private bool HasTween => m_Group != null || m_Window != null;

    // ------------------------------------------------

    public void Close() => GameManager.Instance.PopupS?.Close(this);

    // 여는 연출(이어지는 팝업별 연출 포함)을 끝 모습으로 넘긴다
    public void SkipOpen() => m_OpenSequence.Skip();

    // PopupService.Open만 부른다(켠 뒤). 다시 쓰는 경우 닫기 상태를 풀고, 자식 내용 채우기(OnOpen) 뒤 공용 여는 연출을 돌린다.
    // !!) 닫는 연출 중에 다시 열리면 앞의 OnClose(구독 해제)를 먼저 불러 OnOpen과 짝을 맞춘다
    public void Open()
    {
        m_OpenSequence.Stop();
        m_CloseSequence.Stop();

        if (m_IsClosing && m_IsFinished == false)
            OnClose();

        m_IsClosing = false;
        m_IsFinished = false;
        m_Group.E_SetBlocksRaycasts(true);
        
        OnOpen();

        // 여는 연출: 페이드 + 창 0.9 → 1 튕김, 이어서 팝업별 연출
        SetTween(0f, OpenStartScale);
        m_OpenSequence.Clear();
        
        if (HasTween)
            m_OpenSequence.Tween(OpenTime, t => SetTween(t, Mathf.Lerp(OpenStartScale, 1f, UITween.EaseOutBack(t))));

        OnBuildOpenSequence(m_OpenSequence);
        m_OpenSequence.Play(this);
    }

    // PopupService.Close·Clear만 부른다. 닫는 연출 뒤 OnClose(구독 해제)를 부르고 끈다.
    // isInstant(씬 정리)거나 연출이 없으면 바로 끝낸다. 닫는 연출 중에 다시 불려도 isInstant면 바로 끝낸다
    public void BeginClose(bool isInstant)
    {
        if (isInstant || HasTween == false || gameObject.activeInHierarchy == false)
        {
            m_OpenSequence.Stop();
            m_CloseSequence.Stop();
            
            EndClose();
            return;
        }

        if (m_IsClosing)
            return;

        m_IsClosing = true;
        m_OpenSequence.Stop();

        // 닫는 동안에는 누르지 못하게 한다
        m_Group.E_SetBlocksRaycasts(false);

        // 닫는 연출: 여는 연출의 반대(페이드 아웃 + 창 1 → 0.9). 여는 중에 닫혀도 지금 모습에서 이어 간다
        float startAlpha = m_Group.E_Alive()?.alpha ?? 1f;
        float startScale = m_Window.E_Alive()?.localScale.x ?? 1f;

        m_CloseSequence.Clear()
            .Tween(CloseTime, t =>
            {
                float k = UITween.EaseOutCubic(t);
                SetTween(Mathf.Lerp(startAlpha, 0f, k), Mathf.Lerp(startScale, OpenStartScale, k));
            })
            .Call(EndClose)
            .Play(this);
    }

    private void EndClose()
    {
        if (m_IsFinished)
            return;

        m_IsFinished = true;
        OnClose();

        gameObject.E_SetActive(false);
    }

    private void SetTween(float alpha, float scale)
    {
        if (m_Group != null)
            m_Group.alpha = alpha;
        
        if (m_Window != null)
            m_Window.localScale = Vector3.one * scale;
    }

    // virtual ---

    // 창이 다 열린 뒤 이어지는 연출(결과 칸 튀어나오기 등). 여는 연출 뒤에 단계를 덧붙인다. base를 부를 필요 없다
    protected virtual void OnBuildOpenSequence(UISequence sequence) { }

    // 열 때 내용 채우기. base를 부를 필요 없다
    protected virtual void OnOpen() { }

    // 닫을 때 정리(구독 해제 등). 닫는 연출이 끝난 뒤 불린다. 목록 칸은 돌려주지 않아도 된다(다시 열 때 그대로 쓰고, 씬 전환 때 UIReuseService가 회수). base를 부를 필요 없다
    protected virtual void OnClose() { }
}
