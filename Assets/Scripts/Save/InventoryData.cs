using System.Collections.Generic;

// ==================================================

// ID → 개수. 버섯, 재화, 재료 등 개수로 세는 것은 모두 여기에 둔다.
// 바뀔 때마다 InventoryChanged를 발행하므로 UI는 이벤트만 구독하면 된다

public class InventoryData : PlayerDataBase
{
    public IReadOnlyDictionary<string, int> DicCountByID => m_DicCountByID;
    private readonly Dictionary<string, int> m_DicCountByID = new();

    // ------------------------------------------------
    
    public override void Clear()
    {
        m_DicCountByID.Clear();
    }

    public bool Has(string itemID, int amount = 1) => GetCount(itemID) >= amount;

    public void Add(string itemID, int amount)
    {
        if (itemID.E_IsNullOrEmpty() || amount <= 0)
            return;

        int count = GetCount(itemID) + amount;
        m_DicCountByID[itemID] = count;

        SEventManager.Publish(new InventoryChanged(itemID, count, amount));
    }

    public override void Load(SaveData data)
    {
        foreach (ItemStack stack in data.items)
        {
            if (stack.id.E_IsNullOrEmpty() || stack.count <= 0)
                continue;

            m_DicCountByID[stack.id] = stack.count;
        }
    }

    public override void Save(SaveData data)
    {
        foreach (var pair in m_DicCountByID)
        {
            data.items.Add(new ItemStack(pair.Key, pair.Value));
        }
    }

    // get ---

    public bool TryRemove(string itemID, int amount)
    {
        if (amount <= 0 || Has(itemID, amount) == false)
            return false;

        int count = GetCount(itemID) - amount;
        if (count == 0)
            m_DicCountByID.Remove(itemID);
        else
            m_DicCountByID[itemID] = count;

        SEventManager.Publish(new InventoryChanged(itemID, count, -amount));
        return true;
    }

    public int GetCount(string itemID)
    {
        if (itemID.E_IsNullOrEmpty())
            return 0;

        return m_DicCountByID.TryGetValue(itemID, out int count) ? count : 0;
    }
}
