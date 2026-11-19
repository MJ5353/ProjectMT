using UnityEngine;

// ==================================================

// 전투 이벤트 모음. 전역 이벤트(GameEvents)와 섞지 않고 전투 안에서만 쓴다.

public readonly struct MonsterKilled
{
    public readonly UnitService Units;
    public readonly int UnitID;
    public readonly MonsterDef Def;
    public readonly Vector3 Position;

    public MonsterKilled(UnitService units, int unitID, MonsterDef def, Vector3 position)
    {
        Units = units;
        UnitID = unitID;
        Def = def;
        Position = position;
    }
}

public readonly struct LootPicked
{
    public readonly string ItemID;
    public readonly int Amount;
    public readonly EFreshness Freshness;

    public LootPicked(string itemID, int amount, EFreshness freshness)
    {
        ItemID = itemID;
        Amount = amount;
        Freshness = freshness;
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
