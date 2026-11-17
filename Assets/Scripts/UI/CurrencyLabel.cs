using UnityEngine;
using UnityEngine.UI;

// 인벤토리의 특정 ID 개수를 보여 주는 라벨. 이벤트 구독 예시도 겸한다
[RequireComponent(typeof(Text))]
public class CurrencyLabel : MonoBehaviour
{
    [SerializeField] private string m_ItemID = "Currency_Gold";
    [SerializeField] private string m_Format = "Gold {0}";

    private Text m_Text;

    // ==================================================

    private void Awake()
    {
        m_Text = GetComponent<Text>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<InventoryChanged>(OnInventoryChanged);
        Refresh();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<InventoryChanged>(OnInventoryChanged);
    }

    private void OnInventoryChanged(InventoryChanged evt)
    {
        if (evt.ID != m_ItemID)
            return;

        Refresh();
    }

    private void Refresh()
    {
        GameManager gameM = GameManager.Instance;
        int count = gameM != null ? gameM.Player.Inventory.GetCount(m_ItemID) : 0;

        m_Text.text = string.Format(m_Format, count);
    }

#if UNITY_EDITOR
    public void EditorSetup(string itemID, string format)
    {
        m_ItemID = itemID;
        m_Format = format;
    }
#endif
}
