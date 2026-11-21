using TMPro;
using UnityEngine;

// ==================================================

// 인벤토리의 특정 ID 개수를 보여 주는 TMP 라벨(재화 바 등). 개수가 바뀌면 바로 갱신한다

public class UICountLabel : MonoBehaviour
{
    [SerializeField] private TMP_Text m_Text = null;
    [SerializeField] private string m_ItemID = CurrencyDef.GoldID;
    [SerializeField] private string m_Format = "{0}";

    // ------------------------------------------------

    private void OnEnable()
    {
        SEventManager.Subscribe<InventoryChanged>(OnInventoryChanged);
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
        int count = playerDataM.E_Alive()?.Inventory.GetCount(m_ItemID) ?? 0;
        string strCount = string.Format(m_Format, count.ToString("N0"));
        
        m_Text.E_SetText(strCount);
    }

#if UNITY_EDITOR
    public void EditorSetup(TMP_Text text, string itemID, string format)
    {
        m_Text = text;
        m_ItemID = itemID;
        m_Format = format;
    }
#endif
}
