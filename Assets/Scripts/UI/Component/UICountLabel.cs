using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 인벤토리의 특정 ID 개수를 보여 주는 TMP 라벨(재화 바 등). 개수가 바뀌면 바로 갱신한다.
// 아이콘 칸을 넣어 두면 ID의 Def 아이콘(DataDef.Icon)으로 채운다

public class UICountLabel : MonoBehaviour
{
    [SerializeField] private TMP_Text m_Text = null;
    [SerializeField] private Image m_Icon = null; // 없으면 글자만
    [SerializeField] private string m_ItemID = CurrencyDef.GoldID;
    [SerializeField] private string m_Format = "{0}";

    // ------------------------------------------------

    private void OnEnable()
    {
        SEventManager.Subscribe<InventoryChanged>(OnInventoryChanged);
        
        RefreshIcon();
        Refresh();
    }

    private void OnDisable()
    {
        SEventManager.Unsubscribe<InventoryChanged>(OnInventoryChanged);
    }

    private void OnInventoryChanged(InventoryChanged evt)
    {
        if (evt.ID != m_ItemID)
            return;

        Refresh();
    }

    private void Refresh()
    {
        PlayerDataManager playerDataM = PlayerDataManager.Instance;
        int count = playerDataM.Inventory.GetCount(m_ItemID);
        string strCount = string.Format(m_Format, count.ToString("N0"));
        
        m_Text.E_SetText(strCount);
    }

    // Def에 아이콘이 없으면 칸에 넣어 둔 그림을 그대로 둔다
    private void RefreshIcon()
    {
        if (m_Icon == null || GameDatabase.S_TryGet(m_ItemID, out DataDef def) == false || def.Icon == null)
            return;

        m_Icon.sprite = def.Icon;
        m_Icon.color = Color.white;
    }

#if UNITY_EDITOR
    public void EditorSetup(TMP_Text text, Image icon, string itemID, string format)
    {
        m_Text = text;
        m_Icon = icon;
        m_ItemID = itemID;
        m_Format = format;
    }
#endif
}
