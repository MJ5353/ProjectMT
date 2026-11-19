using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 작은 스킬 아이콘 하나(반투명 원 바탕 + 아이콘). 이어 쓸 수 있는 스킬 줄에 쓴다

public class UISkillIcon : MonoBehaviour
{
    [SerializeField] private CanvasGroup m_Group = null;
    [SerializeField] private Image m_Icon = null;

    // ------------------------------------------------

    public void Set(Sprite icon, float alpha)
    {
        m_Icon.sprite = icon;
        m_Icon.enabled = icon != null;
        m_Group.alpha = alpha;
    }
}
