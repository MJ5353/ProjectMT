using TMPro;
using UnityEngine;

// 인벤토리의 특정 ID 개수를 보여 주는 TMP 라벨(재화 바 등). 개수가 바뀌면 바로 갱신한다
[RequireComponent(typeof(TMP_Text))]
public class UICountLabel : MonoBehaviour
{
    [SerializeField] private string m_ItemID = "Currency_Gold";
    [SerializeField] private string m_Format = "{0}";

    private TMP_Text m_Text;

    // ==================================================

    private void Awake()
    {
        m_Text = GetComponent<TMP_Text>();
    }

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
        int count = playerDataM != null ? playerDataM.Inventory.GetCount(m_ItemID) : 0;

        m_Text.text = string.Format(m_Format, count.ToString("N0"));
    }

#if UNITY_EDITOR
    public void EditorSetup(string itemID, string format)
    {
        m_ItemID = itemID;
        m_Format = format;
    }
#endif
}
