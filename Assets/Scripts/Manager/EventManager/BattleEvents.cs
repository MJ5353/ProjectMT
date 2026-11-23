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

// 전투 흐름 상태가 바뀜(BattleFlow). 종료 버튼처럼 상태에 따라 켜고 끄는 화면이 받는다
public readonly struct BattleStateChanged
{
    public readonly EBattleFlowState State;

    public BattleStateChanged(EBattleFlowState state)
    {
        State = state;
    }
}

// 유닛 체력이 바뀜(피해·회복·Reset·능력치, HealthFeature). 지금 값은 Unit.HealthF에서 읽는다
public readonly struct UnitHpChanged
{
    public readonly UnitController Unit;
    public readonly int Previous; // 바뀌기 전 체력(체력바 잔상 시작점)

    public UnitHpChanged(UnitController unit, int previous)
    {
        Unit = unit;
        Previous = previous;
    }
}

// 주운 버섯 목록(BattleService.DicLoot)이 바뀜. 줍거나, 쓰러져 떨궜을 때
public readonly struct LootChanged
{
}

// 퍼펙트 닷지로 러시가 시작됨. HUD가 어둡게·빈틈 타이머를 띄운다. 남은 시간·단계는 PlayerBrain.Rush에서 읽는다
public readonly struct RushStarted
{
    public readonly UnitController Unit;
    public readonly float Time;          // 빈틈 타이머 전체 길이
    public readonly float FinishWindow;  // 막타로 치는 마지막 구간

    public RushStarted(UnitController unit, float time, float finishWindow)
    {
        Unit = unit;
        Time = time;
        FinishWindow = finishWindow;
    }
}

// 러시가 끝남(막타 착지, 마지막 구간에 안 누름, 전투 정지 등). 슬로우는 이미 풀려 있다
public readonly struct RushEnded
{
    public readonly UnitController Unit;
    public readonly bool IsFinisher; // 막타까지 했는지

    public RushEnded(UnitController unit, bool isFinisher)
    {
        Unit = unit;
        IsFinisher = isFinisher;
    }
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
