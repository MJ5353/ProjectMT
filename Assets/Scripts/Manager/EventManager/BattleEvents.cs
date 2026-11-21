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

public readonly struct BattleEnded
{
    public readonly bool IsWin;

    public BattleEnded(bool isWin)
    {
        IsWin = isWin;
    }
}

// 플레이어가 쓰러짐. 전투는 끝나지 않고 부활/귀가를 고르게 한다(ScreenBattle가 팝업을 띄운다)
public readonly struct PlayerDowned
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
