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
    public readonly ERarity Rarity;

    public LootPicked(string itemID, int amount, EFreshness freshness, ERarity rarity)
    {
        ItemID = itemID;
        Amount = amount;
        Freshness = freshness;
        Rarity = rarity;
    }
}

// 주운 버섯 목록(BattleService.DicLoot)이 바뀜. 줍거나, 쓰러져 떨궜을 때
public readonly struct LootChanged
{
}

// 보스가 포효함(첫 발견·분노). HUD가 붉은 가장자리·이름 배너를 띄운다
public readonly struct BossRoared
{
    public readonly UnitController Unit;
    public readonly bool IsEnraged;

    public BossRoared(UnitController unit, bool isEnraged)
    {
        Unit = unit;
        IsEnraged = isEnraged;
    }
}
