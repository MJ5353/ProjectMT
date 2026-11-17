using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 아이콘 + 개수 칸. 버섯, 재화, 키메라 목록에 같이 쓴다.
// 뱃지는 신선 등급처럼 짧은 표시용이고, 아이콘이 없으면 버섯 색(Tint)으로 칸을 칠한다
public class UIItemSlot : MonoBehaviour
{
    [SerializeField] private Image m_Icon = null;
    [SerializeField] private TMP_Text m_Count = null;
    [SerializeField] private TMP_Text m_Name = null;
    [SerializeField] private GameObject m_Badge = null;
    [SerializeField] private TMP_Text m_BadgeText = null;
    [SerializeField] private Button m_Button = null;

    private Action<UIItemSlot> m_OnClick;

    public string ItemID { get; private set; }
    public int Count { get; private set; }

    // ==================================================

    private void Awake()
    {
        if (m_Button != null)
            m_Button.onClick.AddListener(OnClickSlot);
    }

    public void Set(DataDef def, int count)
    {
        if (def == null)
        {
            Clear();
            return;
        }

        Color tint = def is MushroomDef mushroom ? mushroom.Tint : UIStyle.PanelLight;
        Set(def.ID, def.Icon, def.DisplayName, count, tint);
    }

    public void Set(string itemID, Sprite icon, string displayName, int count, Color tint)
    {
        ItemID = itemID;
        Count = count;

        m_Icon.enabled = true;
        m_Icon.sprite = icon;
        m_Icon.color = icon != null ? Color.white : tint;

        SetCount(count);

        if (m_Name != null)
            m_Name.text = displayName;
    }

    public void SetCount(int count)
    {
        Count = count;

        if (m_Count == null)
            return;

        m_Count.text = count > 0 ? $"x{count}" : string.Empty;
    }

    // 빈 문자열이면 뱃지를 숨긴다
    public void SetBadge(string text, Color color)
    {
        if (m_Badge == null)
            return;

        bool isShow = text.E_IsNullOrEmpty() == false;
        m_Badge.SetActive(isShow);
        if (isShow == false)
            return;

        m_BadgeText.text = text;

        Image badgeImage = m_Badge.GetComponent<Image>();
        if (badgeImage != null)
            badgeImage.color = color;
    }

    public void SetOnClick(Action<UIItemSlot> onClick)
    {
        m_OnClick = onClick;
    }

    public void Clear()
    {
        ItemID = null;
        Count = 0;

        m_Icon.enabled = false;
        SetCount(0);
        SetBadge(null, Color.clear);

        if (m_Name != null)
            m_Name.text = string.Empty;
    }

    private void OnClickSlot()
    {
        m_OnClick?.Invoke(this);
    }

#if UNITY_EDITOR
    public void EditorSetup(Image icon, TMP_Text count, TMP_Text displayName, GameObject badge, TMP_Text badgeText, Button button)
    {
        m_Icon = icon;
        m_Count = count;
        m_Name = displayName;
        m_Badge = badge;
        m_BadgeText = badgeText;
        m_Button = button;
    }
#endif
}
