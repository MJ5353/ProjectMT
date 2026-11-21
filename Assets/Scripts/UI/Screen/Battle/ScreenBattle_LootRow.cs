using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 전투 HUD의 주운 버섯 한 줄: 작은 아이콘 + "이름 신선 x2"

public class ScreenBattle_LootRow : PoolableMonoUI
{
    [SerializeField] private Image m_Icon = null;
    [SerializeField] private TMP_Text m_Text = null;

    // ------------------------------------------------

    public void Set(Sprite icon, Color tint, string text)
    {
        m_Icon.E_SetImage(icon, icon != null ? Color.white : tint); // 아이콘이 없으면 버섯 색 칸
        m_Text.E_SetText(text);
    }
}
