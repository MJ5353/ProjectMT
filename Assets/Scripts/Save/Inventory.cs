using System.Collections.Generic;
using MushTaker.Core;

namespace MushTaker.Save
{
    /// <summary>
    /// id → 개수. 버섯, 재화, 재료 등 개수로 세는 것은 모두 여기에 둔다.
    /// 바뀔 때마다 InventoryChanged를 발행하므로 UI는 이벤트만 구독하면 된다.
    /// </summary>
    public class Inventory
    {
        readonly Dictionary<string, int> _counts = new();

        public IReadOnlyDictionary<string, int> All => _counts;

        public int GetCount(string id) => _counts.TryGetValue(id, out var c) ? c : 0;

        public bool Has(string id, int amount = 1) => GetCount(id) >= amount;

        public void Add(string id, int amount)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0) return;
            var count = GetCount(id) + amount;
            _counts[id] = count;
            EventBus.Publish(new InventoryChanged(id, count, amount));
        }

        public bool TryRemove(string id, int amount)
        {
            if (amount <= 0 || !Has(id, amount)) return false;
            var count = GetCount(id) - amount;
            if (count == 0) _counts.Remove(id);
            else _counts[id] = count;
            EventBus.Publish(new InventoryChanged(id, count, -amount));
            return true;
        }

        internal void Clear() => _counts.Clear();
        internal void SetRaw(string id, int count)
        {
            if (count > 0) _counts[id] = count;
        }
    }
}
