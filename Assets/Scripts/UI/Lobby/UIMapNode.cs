using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 토벌 지도의 지역 노드 하나. 바탕 없이 지역 아이콘(MapDef.Icon)만 크게, 아래 이름은 상태 색. 잠긴 지역은 아이콘을 어둡게.
// 버튼 바탕(투명)은 누르는 영역으로만 쓴다

public class UIMapNode : MonoBehaviour
{
    private static readonly Color s_LockedTint = new(0.35f, 0.35f, 0.35f, 1f);

    [SerializeField] private Button m_Button = null;
    [SerializeField] private Image m_Icon = null;
    [SerializeField] private TMP_Text m_NameLabel = null;

    public Button Button => m_Button;

    // ------------------------------------------------

    public void Set(MapDef mapDef)
    {
        m_NameLabel.E_SetText(mapDef.DisplayName);
        m_Icon.E_SetImage(mapDef.Icon);
        m_Icon.enabled = mapDef.Icon != null;
    }

    public void SetState(Color color, bool isUnlocked)
    {
        m_NameLabel.E_SetColor(color);
        m_Icon.E_SetColor(isUnlocked ? Color.white : s_LockedTint);
    }
}
