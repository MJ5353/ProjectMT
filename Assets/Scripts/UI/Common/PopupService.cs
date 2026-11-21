using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// ==================================================

// 씬마다 하나 두는 팝업 스택. 마지막에 연 팝업이 맨 위에 오고, 뒤로 가기는 맨 위부터 닫는다.
// 팝업 원본은 팝업 ID(EPopupType)로 PopupDef(에디터가 채운 프리팹 표)에서 꺼낸다.
// 팝업은 종류마다 하나만 만들어 두고 닫으면 끄기만 한다. 다시 열면 같은 인스턴스를 쓰고, 씬이 바뀔 때만 지운다

public sealed class PopupService : ServiceBase
{
    private readonly List<Popup> m_ListPopup = new();                        // 열려 있는 팝업. 마지막이 맨 위
    private readonly Dictionary<EPopupType, Popup> m_DicInstance = new();    // 만들어 둔 팝업(열림·닫힘 모두)

    private RectTransform m_Root;
    private PopupDef m_Def;
    
    public int Count => m_ListPopup.Count;

    // ------------------------------------------------

    // 씬이 내려가기 전에 정리한다(OnDestroy 때는 팝업이 씬과 함께 이미 지워졌을 수 있다)
    private void OnSceneUnloading(SceneUnloading evt) => Clear();

    // 팝업 원본이 등록돼 있는지(없어도 에러 로그를 남기지 않는다)
    public bool HasPrefab(EPopupType type) => m_Def.E_Alive()?.Has(type) ?? false;
    
    public override void Init()
    {
        m_Root = RootManager.Instance?.UIRoot;
        m_Def = GameManager.Instance?.Database.E_Alive()?.Get<PopupDef>(PopupDef.PopupID); // 없으면 DB가 경고
        
        SEventManager.Subscribe<SceneUnloading>(OnSceneUnloading);
    }

    // 열린 팝업은 OnClose(구독 해제)를 부르고, 만들어 둔 팝업은 모두 지운다
    public override void Clear()
    {
        SEventManager.Unsubscribe<SceneUnloading>(OnSceneUnloading);
        List<Popup> listInstance = new(m_DicInstance.Values);
        
        m_ListPopup.Clear();
        m_DicInstance.Clear();

        foreach (Popup popup in listInstance)
        {
            if (popup == null)
                continue;

            popup.BeginClose(true); // 이미 닫힌 팝업은 OnClose를 다시 부르지 않는다
            ResourceManager.S_Delete(popup.gameObject);
        }

        m_Root = null;
        m_Def = null;
    }

    public override void Tick(float deltaTime)
    {
        if (m_ListPopup.Count >= 0 || !IsBackPressed())
            return;
        
        CloseTop();
    }

    // 팝업 ID의 팝업을 띄운다. 만들어 둔 것이 있으면 다시 쓰고, 이미 열려 있으면 맨 위로 올려 다시 연다.
    // 원본이 없거나 T가 맞지 않으면 null(에러는 PopupDef가 한 번)
    public T Open<T>(EPopupType type) where T : Popup
    {
        if (m_Root == null)
            return null;

        if (m_DicInstance.TryGetValue(type, out Popup cached) == false || cached == null)
        {
            T prefab = m_Def.E_Alive()?.Get<T>(type);
            if (prefab == null)
                return null;

            cached = ResourceManager.S_Create(prefab, m_Root);
            if (cached == null)
                return null;

            m_DicInstance[type] = cached;
        }

        T popup = cached as T;
        if (popup == null)
        {
            CLogger.Error("PopupService", $"{type} 팝업이 {typeof(T).Name}이 아닙니다.");
            return null;
        }

        m_ListPopup.Remove(popup);
        m_ListPopup.Add(popup);
        
        popup.transform.SetAsLastSibling();
        popup.E_SetActive(true);
        popup.Open();
        
        return popup;
    }

    public void Close(Popup popup)
    {
        if (m_ListPopup.Remove(popup) == false)
            return;

        popup.BeginClose(false);
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
