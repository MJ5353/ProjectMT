using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// ==================================================

// 전투 한 판(하룻밤)의 데이터와 처리. 스폰·리스폰, 밤 시간, 주운 버섯, 떨구기·부활, 끝난 이유별 보상을 맡는다.
// 언제 무엇을 할지(단계 전환, 팝업)는 BattleFlow 상태가 정하고 여기 함수를 부른다. 스스로 "지금 진행 중인가"를 검사하지 않는다
public sealed class BattleService : ServiceBase
{
    public const float SpawnLift = 0.3f; // !!) 캐릭터 컨트롤러가 바닥에 걸친 채 생기면 한 겹짜리 지형 면을 뚫고 떨어진다. 살짝 띄워서 놓는다
    private const string DefaultPlayerID = "Player";

    private readonly Dictionary<LootKey, int> m_DicLoot = new(); // 주운 버섯. 신선도별
    private readonly Dictionary<string, int> m_DicReward = new(); // 끝날 때 실제로 인벤토리에 넣은 것(결과 화면용)
    private readonly Dictionary<string, int> m_DicPickedCurrency = new(); // 전투 중 주워 바로 인벤토리에 넣은 재화(골드 동전). 결과 화면에 더해 보여 준다
    private readonly List<SpawnState> m_ListSpawn = new();
    private readonly Dictionary<int, SpawnState> m_DicSpawnByUnitID = new();
    private readonly List<MushroomPickup> m_ListPickup = new(); // 땅에 떨어져 있는 픽업. 판이 끝나면(Clear) 남은 것을 돌려받는다
    
    private readonly MapRoot m_MapRoot; // 씬 컨트롤러가 만들어 넘긴 맵. 다시 하기에도 같은 맵을 쓴다
    private UnitController m_Player;
    private static DefineDef Define => GameManager.Instance.Define; // 조절값(Define.asset)
    private bool m_IsMarketRewarded; // 시장이 보상을 이미 인벤토리에 넣었다(SetMarketReward)

    public MapDef Map { get; }
    public float ElapsedTime { get; private set; } // Play 상태에서만 흐른다(AdvanceNight)

    public event Action PlayerDied; // BattleFlow가 받아 Down으로 넘어간다

    // 슬롯 하나의 플레이 중 상태
    private class SpawnState
    {
        public readonly MapDef.SpawnEntry Entry;
        public readonly Transform Point;
        public bool IsAlive;
        public float RespawnTimer;

        public SpawnState(MapDef.SpawnEntry entry, Transform point)
        {
            Entry = entry;
            Point = point;
        }
    }
    
    public UnitController Player => m_Player;
    public IReadOnlyDictionary<LootKey, int> DicLoot => m_DicLoot;
    public IReadOnlyDictionary<string, int> DicReward => m_DicReward;
    
    public float NightTime => Map.E_Alive()?.NightTime ?? 0f;
    public float RemainTime => Mathf.Max(0f, NightTime - ElapsedTime);
    public bool IsNightOver => NightTime > 0f && ElapsedTime >= NightTime;
    public bool IsPlayerDown => m_Player != null && m_Player.IsDead;
    public int ReviveGold => Define.Battle.ReviveGold;
    public bool CanRevive => IsPlayerDown && PlayerDataManager.Instance.Inventory.Has(CurrencyDef.GoldID, ReviveGold);
    
    // ------------------------------------------------

    public BattleService(MapDef mapDef, MapRoot map)
    {
        Map = mapDef;
        m_MapRoot = map;
    }

    // 밤을 흘리고 비어 있는 슬롯을 다시 채운다. Play 상태만 부른다(쓰러짐·시장·결과 동안은 밤이 멈춘다)
    public void AdvanceNight(float deltaTime)
    {
        ElapsedTime += deltaTime;
        UpdateRespawn(deltaTime);
    }

    // virtual ---

    // 땅에 떨어진 픽업을 움직인다(낙하·제자리·빨려 듦). 시장에서 시간이 멈추면 같이 멈춘다
    public override void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        // 주우면 목록에서 빠지므로 뒤에서부터 돈다
        for (int i = m_ListPickup.Count - 1; i >= 0; i--)
        {
            m_ListPickup[i].Tick(deltaTime);
        }
    }

    public override void Init()
    {
        FlowManager.Instance.Battle.UnitS.UnitRemoved += ReleaseSpawnSlot;
        SEventManager.Subscribe<LootPicked>(OnLootPicked);
    }

    // 플레이어·몬스터를 만든다(맵은 씬 컨트롤러가 이미 만들었다). BattleFlow Start 상태가 부른다
    public void StartHunt()
    {
        SpawnPlayer();
        
        if (m_Player != null && m_Player.HealthF != null)
            m_Player.HealthF.Died += OnPlayerDied;

        if (Map == null)
        {
            CLogger.Warning("BattleService", "전투에 사용할 지역 데이터가 없습니다.");
            return;
        }

        foreach (MapDef.SpawnEntry entry in Map.ListSpawn)
        {
            Transform point = GetSlot(entry.Slot);
            // 프리팹이 없으면 UnitFactory가 오류 로그(경로는 MonsterDef.PrefabPath)
            if (point == null || entry.Monster == null)
            {
                string monsterName = entry.Monster.E_Alive()?.name ?? "없음";
                CLogger.Warning("BattleService", $"스폰 불가: {MapDef.GetSlotName(entry.Slot)} 지점 {(point == null ? "없음" : "있음")}, 몬스터 {monsterName}");
                continue;
            }

            SpawnState spawn = new(entry, point);
            m_ListSpawn.Add(spawn);
            Spawn(spawn);
        }
    }

    public override void Clear()
    {
        FlowManager.Instance.Battle.UnitS.UnitRemoved -= ReleaseSpawnSlot;
        SEventManager.Unsubscribe<LootPicked>(OnLootPicked);

        UnitController player = m_Player.E_Alive();
        if (player != null && player.HealthF != null)
            player.HealthF.Died -= OnPlayerDied;

        // 다시 하기에 떨어진 버섯이 남지 않게 돌려받는다(빛기둥은 픽업이 돌아갈 때 끈다). 씬과 함께 지워진 것은 건너뛴다
        foreach (MushroomPickup pickup in m_ListPickup)
        {
            ResourceManager.S_Despawn(pickup);
        }

        m_ListPickup.Clear();

        m_DicSpawnByUnitID.Clear();
        m_ListSpawn.Clear();
        m_DicLoot.Clear();
        m_DicReward.Clear();
        m_DicPickedCurrency.Clear();
        
        m_IsMarketRewarded = false;
        ElapsedTime = 0f;
        m_Player = null;
    }

    // 떨어지는 버섯 하나의 희귀도. 지금 지역(MapDef)의 가중치로 굴린다. 전투 밖이면 Common
    public static ERarity RollRarity()
    {
        MapDef map = FlowManager.Instance.Battle?.BattleS.Map;
        return map.E_Alive()?.RollRarity() ?? ERarity.Common;
    }

    // 골드를 내고 일으킨다. 쓰러져 있지 않거나 골드가 모자라면 false
    public bool TryRevive()
    {
        if (IsPlayerDown == false)
            return false;

        PlayerDataManager playerDataM = PlayerDataManager.Instance;
        if (playerDataM.Inventory.TryRemove(CurrencyDef.GoldID, ReviveGold) == false)
            return false;

        m_Player.Revive(Define.Battle.ReviveInvincibleTime);
        return true;
    }

    // 픽업 ---

    // 땅에 떨어지는 픽업(버섯·골드 동전)을 꺼낸다. 움직임은 Tick이 돌리고, 판이 끝나면 남은 것을 돌려받는다
    public MushroomPickup SpawnPickup(MushroomPickup prefab, Vector3 position)
    {
        MushroomPickup pickup = ResourceManager.Instance.Spawn(prefab, position, Quaternion.identity);
        if (pickup != null)
            m_ListPickup.Add(pickup);

        return pickup;
    }

    // 주운 픽업을 돌려준다
    public void DespawnPickup(MushroomPickup pickup)
    {
        m_ListPickup.Remove(pickup);
        ResourceManager.S_Despawn(pickup);
    }

    #region Spawn

    private void SpawnPlayer()
    {
        if (GameDatabase.S_TryGet<PlayerDef>(DefaultPlayerID, out PlayerDef playerDef) == false)
        {
            CLogger.Warning("BattleService", $"PlayerDef({DefaultPlayerID})가 DB에 없습니다.");
            return;
        }

        int playerSlot = Map.E_Alive()?.PlayerSlot ?? MapDef.DefaultPlayerSlot;
        Transform spawn = GetSlot(playerSlot);
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        if (spawn != null)
        {
            position = spawn.position + Vector3.up * SpawnLift;
            rotation = GetSpawnRotation(spawn);
        }
        else
        {
            CLogger.Warning("BattleService", $"플레이어 슬롯 {MapDef.GetSlotName(playerSlot)}이 맵에 없어 원점에서 시작합니다.");
        }

        m_Player = FlowManager.Instance.Battle.UnitS.Spawn(playerDef, position, rotation);
        if (m_Player == null)
            return;

        // 정비 강화를 능력치·패시브 출처로 넣는다. 늘어난 최대 체력만큼 체력도 찬다(HealthFeature가 StatFeature.Changed로)
        PlayerDataManager.Instance.Gear.ApplyTo(m_Player);
    }

    // 슬롯 번호(1부터) → 맵 지점
    private Transform GetSlot(int slot)
    {
        Transform[] slots = m_MapRoot.E_Alive()?.SpawnSlots;
        int index = slot - 1;
        if (slots == null || index < 0 || index >= slots.Length)
            return null;

        return slots[index];
    }

    // 지점의 수평 방향(yaw)만 쓴다
    // !!) 맵 FBX의 SpawnSlot은 X 90도로 누워 있다. 그대로 쓰면 캐릭터 컨트롤러 center도 같이 돌아 캡슐이 땅 밑에서 시작해 떨어진다
    // [mj] todo) fbx를 고치는 방법은?
    public static Quaternion GetSpawnRotation(Transform slot)
    {
        Vector3 forward = Vector3.ProjectOnPlane(slot.forward, Vector3.up);
        
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.ProjectOnPlane(slot.up, Vector3.up); // 앞이 수직이면(누운 지점) 위 축이 수평 방향

        return forward.sqrMagnitude < 0.01f ? Quaternion.identity : Quaternion.LookRotation(forward, Vector3.up);
    }

    private void Spawn(SpawnState spawn)
    {
        UnitController controller = FlowManager.Instance.Battle.UnitS.Spawn(spawn.Entry.Monster, spawn.Point.position + Vector3.up * SpawnLift, GetSpawnRotation(spawn.Point));
        if (controller == null)
            return;

        controller.StatF.AddSource(Map, Map.ListMonsterModifier); // 지역 난이도(체력·공격 배율)
        m_DicSpawnByUnitID.Add(controller.UnitID, spawn);
        spawn.IsAlive = true;
        spawn.RespawnTimer = 0f;
    }

    private void UpdateRespawn(float deltaTime)
    {
        foreach (SpawnState spawn in m_ListSpawn)
        {
            if (spawn.IsAlive || spawn.Entry.RespawnDelay < 0f)
                continue;

            spawn.RespawnTimer += deltaTime;
            if (spawn.RespawnTimer < spawn.Entry.RespawnDelay || IsPlayerNear(spawn.Point.position))
                continue;

            Spawn(spawn);
        }
    }

    private bool IsPlayerNear(Vector3 position)
    {
        if (m_Player == null)
            return false;

        return (m_Player.transform.position - position).sqrMagnitude < Define.Battle.RespawnBlockDistance * Define.Battle.RespawnBlockDistance;
    }

    private void ReleaseSpawnSlot(int unitID)
    {
        if (m_DicSpawnByUnitID.Remove(unitID, out SpawnState spawn))
            spawn.IsAlive = false;
    }
    
    #endregion

    #region OnAction
    
    // 재화(골드 동전)는 바로 인벤토리에 넣고(귀가해도 남는다), 버섯은 주운 목록에 넣는다. 그다음 플레이어 패시브(주웠을 때)
    private void OnLootPicked(LootPicked evt)
    {
        if (GameDatabase.S_TryGet(evt.ItemID, out CurrencyDef _))
        {
            PlayerDataManager.Instance.Inventory.Add(evt.ItemID, evt.Amount);
            
            m_DicPickedCurrency.TryGetValue(evt.ItemID, out int picked);
            m_DicPickedCurrency[evt.ItemID] = picked + evt.Amount;
        }
        else
        {
            LootKey key = new(evt.ItemID, evt.Freshness, evt.Rarity);
            m_DicLoot.TryGetValue(key, out int count);
            m_DicLoot[key] = count + evt.Amount;
            
            SEventManager.Publish(new LootChanged());
        }

        if (m_Player != null)
            m_Player.PassiveF.OnPickup(evt);
    }

    // 끝내지 않고 BattleFlow에 알린다(Down 상태가 버섯을 떨구고 팝업을 띄운다)
    private void OnPlayerDied() => PlayerDied?.Invoke();

    #endregion

    #region Result

    // 주운 버섯을 진짜 픽업으로 흩뿌린다. 부활하면 다시 주울 수 있고, 귀가하면 그대로 잃는다. Down 상태가 부른다
    public void DropLoot()
    {
        if (m_Player == null)
            return;

        Vector3 origin = m_Player.transform.position + Vector3.up * Define.Drop.DownDrop.Height;
        foreach (var pair in m_DicLoot)
        {
            if (GameDatabase.S_TryGet(pair.Key.ItemID, out MushroomDef def) == false)
                continue;

            MushroomPickup prefab = ResourceManager.Instance.Load<MushroomPickup>(def.PickupPath);
            if (prefab == null)
                continue;

            // shortcut: 픽업 하나를 1개로 본다(MushroomPickup.m_Amount 기본값), 묶음 픽업이 생기면 개수를 나눈다
            for (int i = 0; i < pair.Value; i++)
            {
                MushroomPickup pickup = SpawnPickup(prefab, origin);
                Vector2 spread = Random.insideUnitCircle * Define.Drop.DownDrop.Spread;
                pickup.Launch(new Vector3(spread.x, Define.Drop.DownDrop.UpSpeed, spread.y), pair.Key.Freshness, pair.Key.Rarity);
            }
        }

        m_DicLoot.Clear();
        SEventManager.Publish(new LootChanged());
    }
    
    // 시장이 닫혔다(시장 닫기 또는 밤이 다 감). 골드·썩은 버섯·신뢰도는 HuntData가 이미 넣었다. 결과 화면용으로만 남긴다
    public void SetMarketReward(MarketClosed evt)
    {
        m_DicReward.Clear();
        
        if (evt.Gold > 0)
            m_DicReward[CurrencyDef.GoldID] = evt.Gold;
        
        if (evt.RottenCount > 0)
            m_DicReward[ItemDef.RottenID] = evt.RottenCount;

        m_IsMarketRewarded = true;
    }

    // 끝난 이유대로 보상을 넣고 저장한다. BattleFlow Result 상태가 한 번 부른다
    public void Finish(EHuntResult result)
    {
        PlayerDataManager playerDataM = PlayerDataManager.Instance;
        if (m_IsMarketRewarded == false)
        {
            FillReward(result);
            foreach (var pair in m_DicReward)
            {
                playerDataM.Inventory.Add(pair.Key, pair.Value);
            }
        }

        // 전투 중 주운 재화는 이미 인벤토리에 있다. 결과 화면에만 더한다(귀가·쓰러짐에도 남는다)
        foreach (var pair in m_DicPickedCurrency)
        {
            m_DicReward.TryGetValue(pair.Key, out int count);
            m_DicReward[pair.Key] = count + pair.Value;
        }

        // 스스로 나오면 지역을 깬 것으로 표시해 다음 지역을 연다(토벌 탭)
        if (result == EHuntResult.Exited)
            playerDataM.Hunt.MarkCleared(Map);

        playerDataM.RunSave();
    }

    // 시장을 거치지 않고 끝날 때. 밤이 끝나면 주운 개수만큼 썩은 버섯, 귀가는 빈손(토벌 종료는 늘 시장을 거친다)
    private void FillReward(EHuntResult result)
    {
        m_DicReward.Clear();
        if (result != EHuntResult.TimedOut)
            return;

        int total = 0;
        foreach (var pair in m_DicLoot)
        {
            total += pair.Value;
        }

        if (total > 0)
            m_DicReward[ItemDef.RottenID] = total;
    }
    
    #endregion
}
