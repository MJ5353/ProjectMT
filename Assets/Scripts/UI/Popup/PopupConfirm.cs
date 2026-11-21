using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==================================================

// 예/아니오 확인 팝업(공용). 띄운 쪽이 Open 뒤 Set으로 문구와 예/아니오에 할 일을 넣는다.
// 팝업은 고른 결과만 알려 준다(일시정지 등은 띄운 쪽 몫). 뒤로 가기로 닫으면 아니오로 본다

public class PopupConfirm : PopupBase
{
    [SerializeField] private TMP_Text m_Message = null;
    [SerializeField] private Button m_YesButton = null;
    [SerializeField] private Button m_NoButton = null;

    private Action m_OnYes;
    private Action m_OnNo;
    private bool m_IsAnswered; // 예/아니오를 눌렀다. 아니면 뒤로 가기로 닫힌 것

    // ------------------------------------------------

    private void Awake()
    {
        m_YesButton.E_SetOnClick(OnClickYes);
        m_NoButton.E_SetOnClick(OnClickNo);
    }

    public void Set(EStringKey message, Action onYes, Action onNo = null)
    {
        m_Message.E_SetString(message);
        m_OnYes = onYes;
        m_OnNo = onNo;
    }

    // virtual ---

    protected override void OnOpen()
    {
        m_IsAnswered = false;
    }

    // 뒤로 가기로 닫혔으면 아니오를 알린다(닫는 연출이 끝난 뒤)
    protected override void OnClose()
    {
        Action onNo = m_IsAnswered ? null : m_OnNo;
        m_OnYes = null;
        m_OnNo = null;
        onNo?.Invoke();
    }

    // ---

    private void OnClickYes() => Answer(m_OnYes);

    private void OnClickNo() => Answer(m_OnNo);

    // 먼저 닫고 할 일을 한다(할 일이 다른 팝업을 띄울 수 있다)
    private void Answer(Action action)
    {
        m_IsAnswered = true;
        Close();
        action?.Invoke();
    }
}
