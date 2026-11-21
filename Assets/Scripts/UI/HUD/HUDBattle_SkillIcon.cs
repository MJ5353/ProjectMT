using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 작은 스킬 아이콘 하나(반투명 원 바탕 + 아이콘). 이어 쓸 수 있는 스킬 줄에 쓴다. 원본은 UIComponentDef(EUIComponentType.HUDBattle_SkillIcon)에 등록된 프리팹

public class HUDBattle_SkillIcon : PoolableMonoUI
{
    [SerializeField] private CanvasGroup m_Group = null;
    [SerializeField] private Image m_Icon = null;

    // ------------------------------------------------

    public void Set(Sprite icon, float alpha)
    {
        m_Icon.E_SetImage(icon);
        m_Icon.enabled = icon != null;
        m_Group.alpha = alpha;
    }
}
