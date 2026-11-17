using System.Collections.Generic;
using MushTaker.Core;

namespace MushTaker.Save
{
    /// <summary>
    /// 플레이 중 들고 다니는 플레이어 상태. 저장할 때 SaveData로, 불러올 때 SaveData에서 바꾼다.
    /// 플래그는 해금, 튜토리얼 완료처럼 켜고 끄는 값에 쓴다.
    /// </summary>
    public class PlayerData
    {
        public Inventory Inventory { get; } = new();

        readonly HashSet<string> _flags = new();

        public bool HasFlag(string flag) => _flags.Contains(flag);

        public void SetFlag(string flag, bool value = true)
        {
            var changed = value ? _flags.Add(flag) : _flags.Remove(flag);
            if (changed) EventBus.Publish(new FlagChanged(flag, value));
        }

        public void LoadFrom(SaveData data)
        {
            Inventory.Clear();
            foreach (var stack in data.items) Inventory.SetRaw(stack.id, stack.count);
            _flags.Clear();
            _flags.UnionWith(data.flags);
        }

        public SaveData ToSaveData()
        {
            var data = new SaveData();
            foreach (var pair in Inventory.All) data.items.Add(new ItemStack(pair.Key, pair.Value));
            data.flags.AddRange(_flags);
            return data;
        }
    }
}
