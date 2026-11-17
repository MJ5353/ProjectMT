using MushTaker.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MushTaker.UI
{
    /// 인벤토리의 특정 id 개수를 보여 주는 라벨. 이벤트 구독 예시도 겸한다.
    [RequireComponent(typeof(Text))]
    public class CurrencyLabel : MonoBehaviour
    {
        [SerializeField] string itemId = "Currency_Gold";
        [SerializeField] string format = "Gold {0}";

        Text _text;

        void Awake() => _text = GetComponent<Text>();

        void OnEnable()
        {
            EventBus.Subscribe<InventoryChanged>(OnInventoryChanged);
            Refresh();
        }

        void OnDisable() => EventBus.Unsubscribe<InventoryChanged>(OnInventoryChanged);

        void OnInventoryChanged(InventoryChanged e)
        {
            if (e.Id == itemId) Refresh();
        }

        void Refresh()
        {
            var gm = GameManager.Instance;
            _text.text = string.Format(format, gm != null ? gm.Player.Inventory.GetCount(itemId) : 0);
        }

#if UNITY_EDITOR
        public void EditorSetup(string id, string fmt) { itemId = id; format = fmt; }
#endif
    }
}
