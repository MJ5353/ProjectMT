using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 로비 화면. 위 재화 줄(오른쪽 끝 종료 버튼)·가운데 탭 뷰·아래 탭 바. 프리팹은 LobbyUISetup.Prefabs 표로 만든다(팝업 루트는 씬에 따로 둔다)

public class ScreenLobby : ScreenBase
{
    [SerializeField] private UITabBar m_TabBar = null;
    [SerializeField] private ScreenLobby_HuntView m_HuntView = null;
    [SerializeField] private UICountLabel m_GoldLabel = null;
    [SerializeField] private Button m_QuitButton = null;

    public UITabBar TabBar => m_TabBar;
    public ScreenLobby_HuntView HuntView => m_HuntView;
    public UICountLabel GoldLabel => m_GoldLabel;

    // ------------------------------------------------

    private void Awake()
    {
        m_QuitButton.E_SetOnClick(OnClickQuit);
    }

    private void OnClickQuit() => GameManager.Instance.ConfirmQuit();

#if UNITY_EDITOR
    public void EditorSetup(UITabBar tabBar, ScreenLobby_HuntView huntView, UICountLabel goldLabel, Button quitButton)
    {
        m_TabBar = tabBar;
        m_HuntView = huntView;
        m_GoldLabel = goldLabel;
        m_QuitButton = quitButton;
    }
#endif
}