using UnityEngine;

// ==================================================

// 하단 탭 하나가 보여 주는 화면. 탭 화면 스크립트는 이 클래스를 상속해 OnOpen/OnClose를 채운다

public class UITabView : MonoBehaviour
{
    public void SetVisible(bool isVisible)
    {
        if (gameObject.activeSelf == isVisible)
            return;

        gameObject.SetActive(isVisible);

        if (isVisible)
            OnOpen();
        else
            OnClose();
    }

    // virtual ---

    protected virtual void OnOpen() { }

    protected virtual void OnClose() { }
}
