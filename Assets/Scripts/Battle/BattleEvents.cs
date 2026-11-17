using UnityEngine;

// 전투 이벤트 모음. 전역 이벤트(GameEvents)와 섞지 않고 전투 안에서만 쓴다.

public readonly struct MobKilled
{
    public readonly MobDef Def;
    public readonly Vector3 Position;

    public MobKilled(MobDef def, Vector3 position)
    {
        Def = def;
        Position = position;
    }
}

public readonly struct LootPicked
{
    public readonly string ItemId;
    public readonly int Amount;

    public LootPicked(string itemId, int amount)
    {
        ItemId = itemId;
        Amount = amount;
    }
}

public readonly struct BattleEnded
{
    public readonly bool IsWin;

    public BattleEnded(bool isWin)
    {
        IsWin = isWin;
    }
}
