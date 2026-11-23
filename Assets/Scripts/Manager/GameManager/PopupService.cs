using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// ==================================================

// 팝업 스택. 공용 Service라 FlowManager가 한 번 만들어 두고 씬마다 연결(Init)·해제(Clear)한다. 마지막에 연 팝업이 맨 위에 오고, 뒤로 가기(UI/Cancel: Esc·패드 B)는 맨 위부터 닫고, 열린 팝업이 없으면 Flow에 넘긴다(타이틀·로비는 종료 확인).
// 팝업 원본은 팝업 ID(EPopupType)로 PopupDef(에디터가 채운 프리팹 표)에서 꺼낸다.
// 팝업은 종류마다 하나만 만들어 두고 닫으면 끄기만 한다. 다시 열면 같은 인스턴스를 쓰고, 씬이 바뀔 때만 지운다

public sealed class PopupService : ServiceBase
{
    private readonly List<PopupBase> m_ListPopup = new();                        // 열려 있는 팝업. 마지막이 맨 위
    private readonly Dictionary<EPopupType, PopupBase> m_DicInstance = new();    // 만들어 둔 팝업(열림·닫힘 모두)

    private RectTransform m_Root;
    private PopupDef m_Def;
    
    public int Count => m_ListPopup.Count;

    // ------------------------------------------------

    // 팝업 원본이 등록돼 있는지(없어도 에러 로그를 남기지 않는다)
    public bool HasPrefab(EPopupType type) => m_Def.E_Alive()?.Has(type) ?? false;
    
    public override void Init()
    {
        m_Root = UIManager.Instance.UIRoot;
        InputManager.Instance.UI.Cancel.performed += OnCancel;
        
        if (GameDatabase.S_TryGet(PopupDef.PopupID, out m_Def) == false)
            CLogger.Error("PopupService", "PopupDef가 DB에 없습니다. UI 프리팹 표 갱신을 실행하세요.");
    }

    // 열린 팝업은 OnClose(구독 해제)를 부르고, 만들어 둔 팝업은 모두 지운다.
    // FlowManager.End가 씬이 내려가기 전에 부른다(OnDestroy 때는 팝업이 씬과 함께 이미 지워졌을 수 있다)
    public override void Clear()
    {
        InputManager.Instance.UI.Cancel.performed -= OnCancel;

        List<PopupBase> listInstance = new(m_DicInstance.Values);
        
        m_ListPopup.Clear();
        m_DicInstance.Clear();

        foreach (PopupBase popup in listInstance)
        {
            if (popup == null)
                continue;

            popup.BeginClose(true); // 이미 닫힌 팝업은 OnClose를 다시 부르지 않는다
            ResourceManager.S_Delete(popup.gameObject);
        }

        m_Root = null;
        m_Def = null;
    }

    // 팝업 ID의 팝업을 띄운다(Flow가 없어도 되는 공용 팝업). 만들어 둔 것이 있으면 다시 쓰고, 이미 열려 있으면 맨 위로 올려 다시 연다.
    // 원본이 없거나 T가 맞지 않으면 null(에러는 PopupDef가 한 번). Flow에 붙는 팝업(PopupFlowBase)은 Open<T, TFlow>로만 연다
    public T Open<T>(EPopupType type) where T : PopupBase
    {
        T popup = GetOrCreate<T>(type);
        if (popup == null)
            return null;

        if (popup is PopupFlowBase)
        {
            CLogger.Error("PopupService", $"{type}은 Flow에 붙는 팝업입니다. Open<T, TFlow>(type, flow)로 여세요.");
            return null;
        }

        Show(popup);
        return popup;
    }

    // Flow에 붙는 팝업을 띄운다. Flow를 먼저 넣고 연다(OnOpen·여는 연출이 Flow를 쓴다)
    public T Open<T, TFlow>(EPopupType type, TFlow flow) where T : PopupFlowBase<TFlow> where TFlow : FlowBase
    {
        T popup = GetOrCreate<T>(type);
        if (popup == null)
            return null;

        popup.SetFlow(flow);
        Show(popup);
        return popup;
    }

    public void Close(PopupBase popupBase)
    {
        if (m_ListPopup.Remove(popupBase) == false)
            return;

        popupBase.BeginClose(false);
    }

    public bool CloseTop()
    {
        if (m_ListPopup.Count <= 0)
            return false;

        PopupBase top = m_ListPopup[^1];
        if (top.IsCloseOnBack == false)
            return false;

        Close(top);
        return true;
    }

    // 뒤로 가기. 팝업이 있으면 맨 위를 닫고(닫을 수 없는 팝업이면 무시), 없으면 지금 Flow가 받는다
    private void OnCancel(InputAction.CallbackContext context)
    {
        if (m_ListPopup.Count > 0)
            CloseTop();
        else
            FlowManager.Instance.Current.Back();
    }

    // 만들어 둔 팝업을 꺼내거나 처음이면 만든다(아직 열지 않는다)
    private T GetOrCreate<T>(EPopupType type) where T : PopupBase
    {
        if (m_Root == null)
            return null;

        if (m_DicInstance.TryGetValue(type, out PopupBase cached) == false || cached == null)
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
            CLogger.Error("PopupService", $"{type} 팝업이 {typeof(T).Name}이 아닙니다.");

        return popup;
    }

    // 맨 위로 올려 연다
    private void Show(PopupBase popup)
    {
        m_ListPopup.Remove(popup);
        m_ListPopup.Add(popup);
        
        popup.transform.SetAsLastSibling();
        popup.gameObject.SetActive(true);
        popup.Open();
    }
}
