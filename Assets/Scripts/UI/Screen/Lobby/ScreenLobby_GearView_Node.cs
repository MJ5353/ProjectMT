using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

public enum EGearNodeState
{
    Locked,     // 앞 노드가 덜 열림
    Openable,   // 열 수 있음(골드는 따로 본다)
    Opened,
}

// ==================================================

// 정비 강화 트리의 노드 하나. 바탕 색이 상태, 금색 테두리가 스페셜, 흰 테두리가 고른 노드.
// ScreenLobby_GearView가 템플릿을 복제해 GearNodeDef마다 하나씩 만든다

public class ScreenLobby_GearView_Node : MonoBehaviour
{
    private static readonly Color s_LockedColor = new(0.22f, 0.26f, 0.23f, 1f);
    private static readonly Color s_LockedText = new(0.5f, 0.5f, 0.5f, 1f);

    [SerializeField] private Button m_Button = null;
    [SerializeField] private Image m_Bg = null;
    [SerializeField] private Image m_Icon = null;
    [SerializeField] private Image m_Frame = null;      // 스페셜 테두리
    [SerializeField] private GameObject m_Select = null; // 고름 표시
    [SerializeField] private TMP_Text m_NameLabel = null;

    public Button Button => m_Button;
    public GearNodeDef Node { get; private set; }

    // ------------------------------------------------

    public void Set(GearNodeDef node)
    {
        Node = node;
        m_NameLabel.E_SetText(node.DisplayName);
        m_Icon.E_SetImage(node.Icon);
        m_Frame.E_SetActive(node.IsSpecial);
    }

    public void SetState(EGearNodeState state, bool isSelected)
    {
        m_Select.E_SetActive(isSelected);

        switch (state)
        {
            case EGearNodeState.Opened:
                m_Bg.E_SetColor(Node.IsSpecial ? UIStyle.AccentDark : UIStyle.Positive);
                m_NameLabel.E_SetColor(UIStyle.Text);
                break;
            case EGearNodeState.Openable:
                m_Bg.E_SetColor(UIStyle.PanelLight);
                m_NameLabel.E_SetColor(UIStyle.Text);
                break;
            default:
                m_Bg.E_SetColor(s_LockedColor);
                m_NameLabel.E_SetColor(s_LockedText);
                break;
        }
    }
}
