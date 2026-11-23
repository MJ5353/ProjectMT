using System;
using UnityEngine;

// ==================================================

// 전투 한 판(하룻밤)의 상태. 지역, 밤 시간, 플레이어 쓰러짐·부활, 끝날 때 클리어 표시와 저장을 맡는다.
// 스폰은 SpawnService, 픽업·주운 버섯·보상 정산은 LootService, 유닛 목록·플레이어는 UnitService가 든다.
// 언제 무엇을 할지(단계 전환, 팝업)는 BattleFlow 상태가 정하고 여기 함수를 부른다. 스스로 "지금 진행 중인가"를 검사하지 않는다
public sealed class BattleService : ServiceBase
{
    public MapDef Map { get; }
    public float ElapsedTime { get; private set; } // Play 상태에서만 흐른다(AdvanceNight)
    
    public event Action PlayerDied; // BattleFlow가 받아 Down으로 넘어간다

    private static DefineDef Define => GameManager.Instance.Define;
    private static UnitController Player => FlowManager.Instance.Battle.UnitS.Player; // 플레이어는 UnitService가 든다
    
    public float NightTime => Map.E_Alive()?.NightTime ?? 0f;
    public float RemainTime => Mathf.Max(0f, NightTime - ElapsedTime);
    public bool IsNightOver => NightTime > 0f && ElapsedTime >= NightTime;
    public bool IsPlayerDown => Player != null && Player.IsDead;
    public int ReviveGold => Define.Battle.ReviveGold;
    public bool CanRevive => IsPlayerDown && PlayerDataManager.Instance.Inventory.Has(CurrencyDef.GoldID, ReviveGold);

    // ------------------------------------------------

    public BattleService(MapDef mapDef)
    {
        Map = mapDef;
    }

    // virtual ---

    public override void Clear()
    {
        UnitController player = Player.E_Alive();
        if (player != null)
            player.HealthF.Died -= OnPlayerDied;

        ElapsedTime = 0f;
    }

    // ---

    // 스폰한 플레이어의 사망 이벤트를 받는다.
    public void WatchPlayer()
    {
        UnitController player = Player;
        if (player != null)
            player.HealthF.Died += OnPlayerDied;
    }

    // 인게임 타임을 누적한다. (쓰러짐·시장·결과 동안은 밤이 멈춘다)
    public void AdvanceNight(float deltaTime)
    {
        ElapsedTime += deltaTime;
    }

    public bool TryRevive()
    {
        // 쓰러져 있지 않거나 골드가 모자라면 false
        if (IsPlayerDown == false)
            return false;

        if (PlayerDataManager.Instance.Inventory.TryRemove(CurrencyDef.GoldID, ReviveGold) == false)
            return false;

        Player.Revive(Define.Battle.ReviveInvincibleTime);
        return true;
    }

    public void Finish(EHuntResult result)
    {
        PlayerDataManager playerDataM = PlayerDataManager.Instance;
        if (result == EHuntResult.Exited)
            playerDataM.Hunt.MarkCleared(Map);

        // 스스로 나오면 지역을 깬 것으로 표시해 다음 지역을 연다(토벌 탭). 보상은 LootService.Settle이 먼저 넣었다. BattleFlow Result 상태가 한 번 부른다
        playerDataM.RunSave();
    }

    // 끝내지 않고 BattleFlow에 알린다(Down 상태가 버섯을 떨구고 팝업을 띄운다)
    private void OnPlayerDied() => PlayerDied?.Invoke();
}
