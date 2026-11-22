using UnityEngine;

// ==================================================

// 씬을 넘어 맨 위에 뜨는 UI(오버레이)의 기반. UIManager가 [Managers] 아래에 만들어 들고 Show·Hide로 켜고 끈다.
// UI 분류: Screen(씬마다 하나) / Popup(씬 안에서 쌓임) / Overlay(씬을 넘어 맨 위, 이것)
// 자식은 OnShow·OnHide만 구현한다(내용 초기화·정리)

public abstract class OverlayBase : MonoBehaviour
{
    public RectTransform RectTransform => transform as RectTransform;
    public bool IsShowing => gameObject.activeSelf;

    // ------------------------------------------------

    public void Show()
    {
        gameObject.SetActive(true);
        OnShow();
    }

    public void Hide()
    {
        if (IsShowing == false)
            return;

        OnHide();
        gameObject.SetActive(false);
    }

    // virtual ---

    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
}
