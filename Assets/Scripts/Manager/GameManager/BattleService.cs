using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

public enum EBattleState
{
    Playing,
    Exited,     // 토벌 종료 버튼. 주운 버섯을 그대로 받는다
    TimedOut,   // 밤이 끝나 쫓겨남. 주운 버섯은 썩은 버섯이 된다
    GaveUp,     // 쓰러진 뒤 귀가. 아무것도 못 받는다
}

// ==================================================

// [mj] todo) 위치를 옮겨야 할 것 같은데?
// 주운 버섯 묶음 키. 같은 버섯도 신선도가 다르면 따로 센다(HUD 목록, 시장 판매가)
public readonly struct LootKey : IEquatable<LootKey>
{
    public readonly string ItemID;
    public readonly EFreshness Freshness;

    public LootKey(string itemID, EFreshness freshness)
    {
        ItemID = itemID;
        Freshness = freshness;
    }

    public bool Equals(LootKey other) => ItemID == other.ItemID && Freshness == other.Freshness;
    public override bool Equals(object obj) => obj is LootKey other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(ItemID, Freshness);
}

// ==================================================

// 전투 한 판(하룻밤) 진행. 몬스터 스폰·리스폰, 밤 카운트다운, 주운 버섯, 쓰러짐·부활, 끝내는 방식을 맡는다.
// 끝나면 끝난 방식에 따라 보상을 인벤토리에 넣고 게임 흐름을 결과(Result)로 넘긴다
public sealed class BattleService : ServiceBase
{
    public const float SpawnLift = 0.3f; // !!) 캐릭터 컨트롤러가 바닥에 걸친 채 생기면 한 겹짜리 지형 면을 뚫고 떨어진다. 살짝 띄워서 놓는다
    private const float RespawnBlockDistance = 15f; // 플레이어가 이보다 가까우면 리스폰을 미룬다(눈앞에 갑자기 생기지 않게)
    private const string DefaultPlayerID = "Player";
    private const string GoldID = "Currency_Gold";
    private const string RottenItemID = "Item_Rotten"; // 밤이 끝나 썩은 버섯

    private readonly Dictionary<LootKey, int> m_DicLoot = new(); // 주운 버섯. 신선도별
    private readonly Dictionary<string, int> m_DicReward = new(); // 끝날 때 실제로 인벤토리에 넣은 것(결과 화면용)
    private readonly List<SpawnState> m_ListSpawn = new();
    private readonly Dictionary<int, SpawnState> m_DicSpawnByUnitID = new();
    private Transform[] m_SpawnSlots; // [i] = SpawnSlot_{i+1}. 생성한 맵에서 받는다
    private UnitController m_Player;
    private PlayerDef m_PlayerDef;

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
    public float NightTime => Map != null ? Map.NightTime : 0f;
    public float RemainTime => Mathf.Max(0f, NightTime - ElapsedTime);
    public bool IsPlayerDown => m_Player != null && m_Player.IsDead;
    public int ReviveGold => m_PlayerDef != null ? m_PlayerDef.ReviveGold : 0;
    public bool CanRevive => IsPlayerDown && (PlayerDataManager.Instance?.Inventory.Has(GoldID, ReviveGold) ?? false);
    
    // ------------------------------------------------

    public BattleService(MapDef mapDef)
    {
        Map = mapDef;
    }

    public override void Tick(float deltaTime)
    {
        // 쓰러져 부활/귀가를 고르는 동안은 밤도 리스폰도 멈춘다
        if (State != EBattleState.Playing || IsPlayerDown)
            return;

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
                string monsterName = entry.Monster == null ? "없음" : entry.Monster.name;
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

        HealthFeature health = m_Player?.HealthF;
        if (health != null)
            health.Died -= OnPlayerDied;

        m_DicSpawnByUnitID.Clear();
        m_ListSpawn.Clear();
        m_DicLoot.Clear();
        m_DicReward.Clear();
        m_Player = null;
        m_PlayerDef = null;
        m_SpawnSlots = null; // 맵 오브젝트는 씬과 함께 사라진다
    }

    public void Retry() => GameManager.Instance?.RetryBattle();

    public void ExitHunt() => End(EBattleState.Exited);

    public void GiveUp() => End(EBattleState.GaveUp);

    // 골드를 내고 쓰러진 플레이어를 일으킨다. 골드가 모자라면 false
    public bool TryRevive()
    {
        if (State != EBattleState.Playing || IsPlayerDown == false)
            return false;

        PlayerDataManager playerDataM = PlayerDataManager.Instance;
        if (playerDataM == null || playerDataM.Inventory.TryRemove(GoldID, ReviveGold) == false)
            return false;

        m_Player.Revive(m_PlayerDef.ReviveInvincibleTime);
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

        MapRoot map = UnityEngine.Object.Instantiate(prefab);
        m_SpawnSlots = map.SpawnSlots;
    }

    private void SpawnPlayer()
    {
        PlayerDef playerDef = GameManager.Instance.Database?.Get<PlayerDef>(DefaultPlayerID);
        if (playerDef == null)
        {
            CLogger.Warning("BattleService", $"PlayerDef({DefaultPlayerID})가 DB에 없습니다.");
            return;
        }

        int playerSlot = Map != null ? Map.PlayerSlot : MapDef.DefaultPlayerSlot;
        Transform spawn = GetSlot(playerSlot);

        if (spawn == null)
            CLogger.Warning("BattleService", $"플레이어 슬롯 {MapDef.GetSlotName(playerSlot)}이 맵에 없어 원점에서 시작합니다.");

        Vector3 position = spawn != null ? spawn.position + Vector3.up * SpawnLift : Vector3.zero;
        Quaternion rotation = spawn != null ? GetSpawnRotation(spawn) : Quaternion.identity;

        m_PlayerDef = playerDef;
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

        return (m_Player.transform.position - position).sqrMagnitude < RespawnBlockDistance * RespawnBlockDistance;
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
        LootKey key = new(evt.ItemID, evt.Freshness);
        m_DicLoot.TryGetValue(key, out int count);
        m_DicLoot[key] = count + evt.Amount;
    }

    // 바로 끝내지 않고 부활/귀가를 고르게 한다(BattleHud가 팝업을 띄운다)
    private void OnPlayerDied()
    {
        if (State == EBattleState.Playing)
            SEventManager.Publish(new PlayerDowned());
    }
    
    private void End(EBattleState result)
    {
        if (State != EBattleState.Playing)
            return;

        State = result;
        if (m_Player != null)
            m_Player.HealthF.Died -= OnPlayerDied;

        FillReward(result);
        PlayerDataManager playerDataM = PlayerDataManager.Instance;
        if (playerDataM != null)
        {
            foreach (var pair in m_DicReward)
            {
                playerDataM.Inventory.Add(pair.Key, pair.Value);
            }

            // 스스로 나오면 지역을 깬 것으로 표시해 다음 지역을 연다(토벌 탭)
            if (result == EBattleState.Exited)
                playerDataM.Hunt.MarkCleared(Map);

            playerDataM.RunSave();
        }

        GameManager gameM = GameManager.Instance;
        if (gameM != null && gameM.Current == EGameState.Battle)
            gameM.ChangeState(EGameState.Result);

        SEventManager.Publish(new BattleEnded(result == EBattleState.Exited));
    }

    // 토벌 종료는 주운 그대로, 밤이 끝나면 주운 개수만큼 썩은 버섯, 귀가는 빈손
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
