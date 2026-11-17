using UnityEngine;

// 전투 이벤트 모음. 전역 이벤트(GameEvents)와 섞지 않고 전투 안에서만 쓴다.

public readonly struct MonsterKilled
{
    public readonly ActorService Actors;
    public readonly int ActorID;
    public readonly MonsterDef Def;
    public readonly Vector3 Position;

    public MonsterKilled(ActorService actors, int actorID, MonsterDef def, Vector3 position)
    {
        Actors = actors;
        ActorID = actorID;
        Def = def;
        Position = position;
    }
}

public readonly struct LootPicked
{
    public readonly string ItemID;
    public readonly int Amount;

    public LootPicked(string itemID, int amount)
    {
        ItemID = itemID;
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
