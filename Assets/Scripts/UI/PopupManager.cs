using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 씬마다 하나 두는 팝업 스택. 마지막에 연 팝업이 맨 위에 오고, 뒤로 가기는 맨 위부터 닫는다
public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    [SerializeField] private RectTransform m_Root = null;

    private readonly List<Popup> m_ListPopup = new();

    public int Count => m_ListPopup.Count;

    // ==================================================

    private void Awake()
    {
        Instance = this;

        if (m_Root == null)
            m_Root = (RectTransform)transform;
    }

    private void Update()
    {
        if (IsBackPressed())
            CloseTop();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public T Open<T>(T prefab) where T : Popup
    {
        if (prefab == null)
            return null;

        T popup = Instantiate(prefab, m_Root);
        m_ListPopup.Add(popup);
        popup.OnOpen();

        return popup;
    }

    public void Close(Popup popup)
    {
        if (m_ListPopup.Remove(popup) == false)
            return;

        popup.OnClose();
        Destroy(popup.gameObject);
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

#if UNITY_EDITOR
    public void EditorSetRoot(RectTransform root) => m_Root = root;
#endif
}
