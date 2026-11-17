using System.Collections.Generic;

// 플레이 중 들고 다니는 플레이어 상태. 저장할 때 SaveData로, 불러올 때 SaveData에서 바꾼다.
// 플래그는 해금, 튜토리얼 완료처럼 켜고 끄는 값에 쓴다
public class DataService
{
    private readonly HashSet<string> m_Flags = new();

    public Inventory Inventory { get; } = new();

    public void Clear()
    {
        Inventory.Clear();
        m_Flags.Clear();
    }

    public void Load(SaveData data)
    {
        Clear();

        if (data == null)
            return;

        foreach (ItemStack stack in data.items)
        {
            Inventory.SetRaw(stack.id, stack.count);
        }

        m_Flags.UnionWith(data.flags);
    }

    public SaveData ToSaveData()
    {
        SaveData data = new SaveData();

        foreach (var pair in Inventory.DicCountByID)
        {
            data.items.Add(new ItemStack(pair.Key, pair.Value));
        }

        data.flags.AddRange(m_Flags);
        return data;
    }

    // flag
    
    public bool HasFlag(string flag) => m_Flags.Contains(flag);

    public void SetFlag(string flag, bool value = true)
    {
        bool isChanged = value ? m_Flags.Add(flag) : m_Flags.Remove(flag);
        if (isChanged == false)
            return;

        SEventService.Publish(new FlagChanged(flag, value));
    }
}
