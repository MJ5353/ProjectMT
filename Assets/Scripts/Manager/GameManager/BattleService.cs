using System.Collections.Generic;
using UnityEngine;

// ==================================================

public enum EBattleState
{
    Playing,
    Market,     // 토벌 종료 뒤 현장 시장. 밤과 전투가 멈추고, 시장이 닫히면 Exited로 끝난다
    Exited,     // 토벌 종료. 시장에서 판 골드와 못 판 버섯(썩은 버섯)을 받는다
    TimedOut,   // 밤이 끝나 쫓겨남. 주운 버섯은 썩은 버섯이 된다
    GaveUp,     // 쓰러진 뒤 귀가. 아무것도 못 받는다
}

// ==================================================

// 전투 한 판(하룻밤) 진행. 몬스터 스폰·리스폰, 밤 카운트다운, 주운 버섯, 쓰러짐·부활, 끝내는 방식을 맡는다.
// 끝나면 끝난 방식에 따라 보상을 인벤토리에 넣고 게임 흐름을 결과(Result)로 넘긴다.
// 토벌 종료는 바로 끝내지 않고 현장 시장(HuntData)을 연다. 시장이 열린 동안은 시간을 멈추고(TimeManager), MarketClosed를 받으면 끝낸다
public sealed class BattleService : ServiceBase
{
    public const float SpawnLift = 0.3f; // !!) 캐릭터 컨트롤러가 바닥에 걸친 채 생기면 한 겹짜리 지형 면을 뚫고 떨어진다. 살짝 띄워서 놓는다
    private const string DefaultPlayerID = "Player";
    private const string GoldID = "Currency_Gold";
    private const string RottenItemID = "Item_Rotten"; // 밤이 끝나 썩은 버섯

    private readonly Dictionary<LootKey, int> m_DicLoot = new(); // 주운 버섯. 신선도별
    private readonly Dictionary<string, int> m_DicReward = new(); // 끝날 때 실제로 인벤토리에 넣은 것(결과 화면용)
    private readonly List<SpawnState> m_ListSpawn = new();
    private readonly Dictionary<int, SpawnState> m_DicSpawnByUnitID = new();
    
    private Transform[] m_SpawnSlots; // [i] = SpawnSlot_{i+1}. 생성한 맵에서 받는다
    private UnitController m_Player;
    private static DefineDef Define => GameManager.Instance.Define; // 조절값(Define.asset)
    private float m_DownRemain; // 쓰러진 뒤 팝업까지 남은 시간. 0보다 작으면 이미 띄웠다

    public MapDef Map { get; }
    public EBattleState State { get; private set; } = EBattleState.Playing;
    public float ElapsedTime { get; private set; } // 쓰러져 있는 동안은 흐르지 않는다

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
    public bool IsPlayerDown => m_Player != null && m_Player.IsDead;
    public int ReviveGold => Define.ReviveGold;
    public bool CanRevive => IsPlayerDown && (PlayerDataManager.Instance?.Inventory.Has(GoldID, ReviveGold) ?? false);
    
    // ------------------------------------------------

    public BattleService(MapDef mapDef)
    {
        Map = mapDef;
    }

    public override void Tick(float deltaTime)
    {
        if (State != EBattleState.Playing)
            return;

        // 쓰러져 있는 동안은 밤도 리스폰도 멈춘다. 버섯을 떨구는 걸 잠깐 보여 준 뒤 부활/귀가 팝업
        if (IsPlayerDown)
        {
            UpdateDown(deltaTime);
            return;
        }

        ElapsedTime += deltaTime;
        UpdateRespawn(deltaTime);

        if (NightTime > 0f && ElapsedTime >= NightTime)
            End(EBattleState.TimedOut);
    }

    // virtual ---

    public override void Init()
    {
        SpawnMap();
        SpawnPlayer();
        
        if (m_Player != null && m_Player.HealthF != null)
            m_Player.HealthF.Died += OnPlayerDied;

        GameManager.Instance.UnitS.UnitRemoved += ReleaseSpawnSlot;
        SEventManager.Subscribe<LootPicked>(OnLootPicked);

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
        GameManager.Instance.UnitS.UnitRemoved -= ReleaseSpawnSlot;
        SEventManager.Unsubscribe<LootPicked>(OnLootPicked);
        SEventManager.Unsubscribe<MarketClosed>(OnMarketClosed);

        // 시장 도중 씬이 바뀌어도 멈춘 시간이 남지 않게 한다
        if (State == EBattleState.Market)
            TimeManager.Instance?.SetPause(false);

        HealthFeature health = m_Player.E_Alive()?.HealthF;
        if (health != null)
            health.Died -= OnPlayerDied;

        m_DicSpawnByUnitID.Clear();
        m_ListSpawn.Clear();
        m_DicLoot.Clear();
        m_DicReward.Clear();
        m_Player = null;
        m_SpawnSlots = null; // 맵 오브젝트는 씬과 함께 사라진다
    }

    public void Retry() => GameManager.Instance?.RetryBattle();

    // 떨어지는 버섯 하나의 희귀도. 지금 지역(MapDef)의 가중치로 굴린다. 전투 밖이면 Common
    public static ERarity RollRarity()
    {
        MapDef map = GameManager.Instance?.BattleS?.Map;
        return map.E_Alive()?.RollRarity() ?? ERarity.Common;
    }

    // 토벌 종료 버튼. isMarket이면 시장을 열고(팝업은 HUDBattle가 띄운다), 아니면 주운 버섯을 그대로 받고 끝낸다
    public void ExitHunt(bool isMarket = true)
    {
        if (State != EBattleState.Playing || IsPlayerDown)
            return;

        HuntData hunt = PlayerDataManager.Instance?.Hunt;
        if (isMarket == false || hunt == null)
        {
            End(EBattleState.Exited);
            return;
        }

        State = EBattleState.Market;
        TimeManager.Instance?.SetPause(true);
        SEventManager.Subscribe<MarketClosed>(OnMarketClosed);
        hunt.BeginMarket(m_DicLoot, RemainTime, NightTime);
    }

    public void GiveUp() => End(EBattleState.GaveUp);

    public bool TryRevive()
    {
        if (State != EBattleState.Playing || IsPlayerDown == false)
            return false;

        PlayerDataManager playerDataM = PlayerDataManager.Instance;
        if (playerDataM == null || playerDataM.Inventory.TryRemove(GoldID, ReviveGold) == false)
            return false;

        m_Player.Revive(Define.ReviveInvincibleTime);
        return true;
    }

    public bool TryGoLobby()
    {
        GameManager gameM = GameManager.Instance;
        if (gameM == null || Application.CanStreamedLevelBeLoaded(SceneNames.Lobby) == false)
            return false;

        return gameM.ChangeState(EGameState.Lobby);
    }

    #region Spawn

    // 지역 맵을 만든다. 씬은 Single로 불러서 지금 씬에 들어간다
    private void SpawnMap()
    {
        if (Map == null)
            return;

        MapRoot prefab = ResourceManager.Instance?.Load<MapRoot>(Map.PrefabPath, "프리팹 이름이 MapDef ID와 같은지, 누락 전투 에셋 복구를 했는지 확인하세요.");
        if (prefab == null)
            return;

        MapRoot map = ResourceManager.S_Create(prefab);
        if (map == null)
            return;

        m_SpawnSlots = map.SpawnSlots;
    }

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

        m_Player = GameManager.Instance.UnitS.Spawn(playerDef, position, rotation);
    }

    // 슬롯 번호(1부터) → 맵 지점
    private Transform GetSlot(int slot)
    {
        int index = slot - 1;
        if (m_SpawnSlots == null || index < 0 || index >= m_SpawnSlots.Length)
            return null;

        return m_SpawnSlots[index];
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
        UnitController controller = GameManager.Instance.UnitS.Spawn(spawn.Entry.Monster, spawn.Point.position + Vector3.up * SpawnLift, GetSpawnRotation(spawn.Point));
        if (controller == null)
            return;

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

        return (m_Player.transform.position - position).sqrMagnitude < Define.RespawnBlockDistance * Define.RespawnBlockDistance;
    }

    private void ReleaseSpawnSlot(int unitID)
    {
        if (m_DicSpawnByUnitID.Remove(unitID, out SpawnState spawn))
            spawn.IsAlive = false;
    }
    
    #endregion

    #region OnAction
    
    private void OnLootPicked(LootPicked evt)
    {
        LootKey key = new(evt.ItemID, evt.Freshness, evt.Rarity);
        m_DicLoot.TryGetValue(key, out int count);
        m_DicLoot[key] = count + evt.Amount;
    }

    // 바로 끝내지 않고 부활/귀가를 고르게 한다(HUDBattle가 팝업을 띄운다)
    // 바로 끝내지 않는다. 주운 버섯을 땅에 떨구고, 잠깐 뒤 부활/귀가를 고르게 한다(HUDBattle가 팝업을 띄운다)
    private void OnPlayerDied()
    {
        if (State != EBattleState.Playing)
            return;

        DropLoot();
        m_DownRemain = Define.DownPopupDelay;
    }

    private void UpdateDown(float deltaTime)
    {
        if (m_DownRemain < 0f)
            return;

        m_DownRemain -= deltaTime;
        if (m_DownRemain < 0f)
            SEventManager.Publish(new PlayerDowned());
    }

    // 주운 버섯을 진짜 픽업으로 흩뿌린다. 부활하면 다시 주울 수 있고, 귀가하면 그대로 잃는다
    private void DropLoot()
    {
        if (m_Player == null)
            return;

        Vector3 origin = m_Player.transform.position + Vector3.up * Define.DownDropHeight;
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
                MushroomPickup pickup = ResourceManager.Instance.Spawn(prefab, origin, Quaternion.identity);
                Vector2 spread = Random.insideUnitCircle * Define.DownDropSpread;
                pickup.Launch(new Vector3(spread.x, Define.DownDropUpSpeed, spread.y), pair.Key.Freshness, pair.Key.Rarity);
            }
        }

        m_DicLoot.Clear();
    }
    
    // 시장이 닫혔다(시장 닫기 또는 밤이 다 감). 골드·썩은 버섯·신뢰도는 HuntData가 이미 넣었다. 결과 화면용으로만 남긴다
    private void OnMarketClosed(MarketClosed evt)
    {
        SEventManager.Unsubscribe<MarketClosed>(OnMarketClosed);
        TimeManager.Instance?.SetPause(false);

        m_DicReward.Clear();
        
        if (evt.Gold > 0)
            m_DicReward[GoldID] = evt.Gold;
        
        if (evt.RottenCount > 0)
            m_DicReward[RottenItemID] = evt.RottenCount;

        End(EBattleState.Exited);
    }

    private void End(EBattleState result)
    {
        if (State != EBattleState.Playing && State != EBattleState.Market)
            return;

        bool isFromMarket = State == EBattleState.Market;
        State = result;
        if (m_Player != null)
            m_Player.HealthF.Died -= OnPlayerDied;

        PlayerDataManager playerDataM = PlayerDataManager.Instance;
        if (isFromMarket == false)
            FillReward(result);

        if (playerDataM != null)
        {
            if (isFromMarket == false)
            {
                foreach (var pair in m_DicReward)
                {
                    playerDataM.Inventory.Add(pair.Key, pair.Value);
                }
            }

            // 스스로 나오면 지역을 깬 것으로 표시해 다음 지역을 연다(토벌 탭)
            if (result == EBattleState.Exited)
                playerDataM.Hunt.MarkCleared(Map);

            playerDataM.RunSave();
        }

        if (GameManager.Instance.Current == EGameState.Battle)
            GameManager.Instance.ChangeState(EGameState.Result);
        
        SEventManager.Publish(new BattleEnded(result == EBattleState.Exited));
    }

    // 시장 없이 끝낼 때. 토벌 종료(시장 팝업이 없을 때)는 주운 그대로, 밤이 끝나면 주운 개수만큼 썩은 버섯, 귀가는 빈손
    private void FillReward(EBattleState result)
    {
        m_DicReward.Clear();

        int total = 0;
        foreach (var pair in m_DicLoot)
        {
            total += pair.Value;
            
            // [mj] todo) 인벤토리는 아직 신선도를 모른다. 시장이 들어오면 신선도별로 넘긴다
            if (result == EBattleState.Exited)
            {
                m_DicReward.TryGetValue(pair.Key.ItemID, out int count);
                m_DicReward[pair.Key.ItemID] = count + pair.Value;
            }
        }

        if (result == EBattleState.TimedOut && total > 0)
            m_DicReward[RottenItemID] = total;
    }
    
    #endregion
}
