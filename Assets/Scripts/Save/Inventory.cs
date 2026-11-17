using System.Collections.Generic;

// ID → 개수. 버섯, 재화, 재료 등 개수로 세는 것은 모두 여기에 둔다.
// 바뀔 때마다 InventoryChanged를 발행하므로 UI는 이벤트만 구독하면 된다
public class Inventory
{
    private readonly Dictionary<string, int> m_DicCountByID = new();

    public IReadOnlyDictionary<string, int> DicCountByID => m_DicCountByID;

    public void Clear()
    {
        m_DicCountByID.Clear();
    }

    public bool Has(string id, int amount = 1) => GetCount(id) >= amount;

    public void Add(string id, int amount)
    {
        if (id.E_IsNullOrEmpty() || amount <= 0)
            return;

        int count = GetCount(id) + amount;
        m_DicCountByID[id] = count;

        EventBus.Publish(new InventoryChanged(id, count, amount));
    }
    
    // !!) 세이브 불러오기 전용. 이벤트를 발행하지 않는다
    public void SetRaw(string id, int count)
    {
        if (id.E_IsNullOrEmpty() || count <= 0)
            return;

        m_DicCountByID[id] = count;
    }

    // get ---
    
    public bool TryRemove(string id, int amount)
    {
        if (amount <= 0 || Has(id, amount) == false)
            return false;

        int count = GetCount(id) - amount;
        if (count == 0)
            m_DicCountByID.Remove(id);
        else
            m_DicCountByID[id] = count;

        EventBus.Publish(new InventoryChanged(id, count, -amount));
        return true;
    }
    
    public int GetCount(string id)
    {
        if (id.E_IsNullOrEmpty())
            return 0;

        return m_DicCountByID.TryGetValue(id, out int count) ? count : 0;
    }
}
