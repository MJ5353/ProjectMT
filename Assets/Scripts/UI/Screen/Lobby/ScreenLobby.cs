using UnityEngine;

// ==================================================

// 로비 화면. 위 재화 줄·가운데 탭 뷰·아래 탭 바. 프리팹은 LobbyUISetup.Prefabs 표로 만든다(팝업 루트는 씬에 따로 둔다)

public class ScreenLobby : ScreenBase
{
    [SerializeField] private UITabBar m_TabBar = null;
    [SerializeField] private ScreenLobby_HuntView m_HuntView = null;
    [SerializeField] private UICountLabel m_GoldLabel = null;

    public UITabBar TabBar => m_TabBar;
    public ScreenLobby_HuntView HuntView => m_HuntView;
    public UICountLabel GoldLabel => m_GoldLabel;

#if UNITY_EDITOR
    public void EditorSetup(UITabBar tabBar, ScreenLobby_HuntView huntView, UICountLabel goldLabel)
    {
        m_TabBar = tabBar;
        m_HuntView = huntView;
        m_GoldLabel = goldLabel;
    }
#endif
}