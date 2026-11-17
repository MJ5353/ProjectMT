using System.Collections.Generic;

// ==================================================
// 해금, 튜토리얼 완료처럼 켜고 끄는 값. 바뀌면 FlagChanged를 발행한다

public class FlagData : PlayerDataBase
{
    private readonly HashSet<string> m_Flags = new();

    public IReadOnlyCollection<string> Flags => m_Flags;

    // ------------------------------------------------

    public override void Clear()
    {
        m_Flags.Clear();
    }

    public override void Load(SaveData data)
    {
        m_Flags.UnionWith(data.flags);
    }

    public override void Save(SaveData data)
    {
        data.flags.AddRange(m_Flags);
    }

    public bool Has(string flag) => m_Flags.Contains(flag);

    public void Set(string flag, bool value = true)
    {
        bool isChanged = value ? m_Flags.Add(flag) : m_Flags.Remove(flag);
        if (isChanged == false)
            return;

        SEventService.Publish(new FlagChanged(flag, value));
    }
}
