using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// ==================================================

// 씬마다 하나 두는 팝업 스택. 마지막에 연 팝업이 맨 위에 오고, 뒤로 가기는 맨 위부터 닫는다
public sealed class PopupService : ServiceBase
{
    private RectTransform m_Root;
    private readonly List<Popup> m_ListPopup = new();

    public int Count => m_ListPopup.Count;

    // ------------------------------------------------

    public override void Init()
    {
        m_Root = RootManager.Instance?.UIRoot;
        SEventManager.Subscribe<SceneUnloading>(OnSceneUnloading);
    }

    // 닫는 팝업마다 OnClose를 불러 목록 칸(UIListPool)이 풀로 돌아가게 한다
    public override void Clear()
    {
        SEventManager.Unsubscribe<SceneUnloading>(OnSceneUnloading);

        // 먼저 비워 둔다. OnClose 안에서 다른 팝업을 Close해도 두 번 닫히지 않는다
        Popup[] listClose = m_ListPopup.ToArray();
        m_ListPopup.Clear();

        for (int i = listClose.Length - 1; i >= 0; --i)
        {
            Popup popup = listClose[i];
            if (popup == null)
                continue;

            popup.OnClose();
            Object.Destroy(popup.gameObject);
        }

        m_Root = null;
    }

    // 씬이 내려가기 전에 정리한다(OnDestroy 때는 칸이 팝업과 함께 지워질 수 있다)
    private void OnSceneUnloading(SceneUnloading evt) => Clear();

    public override void Tick(float deltaTime)
    {
        if (m_ListPopup.Count > 0 && IsBackPressed())
            CloseTop();
    }

    public T Open<T>(T prefab) where T : Popup
    {
        if (prefab == null || m_Root == null)
            return null;

        T popup = Object.Instantiate(prefab, m_Root);
        m_ListPopup.Add(popup);
        popup.OnOpen();

        return popup;
    }

    public void Close(Popup popup)
    {
        if (m_ListPopup.Remove(popup) == false)
            return;

        popup.OnClose();
        Object.Destroy(popup.gameObject);
    }

    public bool CloseTop()
    {
        if (m_ListPopup.Count <= 0)
            return false;

        Popup top = m_ListPopup[^1];
        if (top.IsCloseOnBack == false)
            return false;

        Close(top);
        return true;
    }

    private static bool IsBackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        // 안드로이드 뒤로 버튼도 Input System에서는 Escape로 들어온다
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

}
