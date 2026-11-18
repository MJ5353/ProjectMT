using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

public enum EBattleState
{
    Playing,
    Won,
    Lost,
}

// ==================================================

// 전투 한 판 진행. 몬스터 스폰·리스폰, 처치 수, 주운 버섯, 승패를 맡는다.
// 끝나면 주운 버섯을 인벤토리에 넣고 게임 흐름을 결과(Result)로 넘긴다
public sealed class BattleService : ServiceBase
{
    // 슬롯 하나의 플레이 중 상태
    private class SpawnState
    {
        public readonly MapDef.SpawnEntry Entry;
        public readonly Transform Point;
        public bool IsAlive;
        public float RespawnTimer;

        // ------------------------------------------------

        public SpawnState(MapDef.SpawnEntry entry, Transform point)
        {
            Entry = entry;
            Point = point;
        }
    }

    public const float SpawnLift = 0.3f; // !!) 캐릭터 컨트롤러가 바닥에 걸친 채 생기면 한 겹짜리 지형 면을 뚫고 떨어진다. 살짝 띄워서 놓는다
    public const string TerrainName = "Map_Terrain";
    private const float RespawnBlockDistance = 15f; // 플레이어가 이보다 가까우면 리스폰을 미룬다(눈앞에 갑자기 생기지 않게)
    private const int GoalKillCount = 10; // 이만큼 잡으면 승리 (보스가 들어오기 전 임시 조건)
    private const string DefaultPlayerID = "Player";

    private readonly Transform[] m_SpawnSlots; // [i] = SpawnSlot_{i+1}. SceneController가 넘겨준다
    private readonly Dictionary<string, int> m_DicLoot = new();
    private readonly List<SpawnState> m_ListSpawn = new();

    public IReadOnlyDictionary<string, int> DicLoot => m_DicLoot;
    private readonly Dictionary<int, SpawnState> m_DicSpawnByActorID = new();

    public PlayerController Player => m_PlayerController;
    private PlayerController m_PlayerController;

    public MapDef Map { get; }
    public EBattleState State { get; private set; } = EBattleState.Playing;
    public int KillCount { get; private set; }
    public int KillGoal => GoalKillCount;
    public float ElapsedTime { get; private set; }

    // ------------------------------------------------

    public BattleService(MapDef mapDef, Transform[] spawnSlots)
    {
        Map = mapDef;
        m_SpawnSlots = spawnSlots;
    }

    public override void Tick(float deltaTime)
    {
        if (State != EBattleState.Playing)
            return;

        ElapsedTime += deltaTime;
        UpdateRespawn(deltaTime);
    }

    // virtual ---

    public override void Init()
    {
        SpawnPlayer();
        if (m_PlayerController != null && m_PlayerController.Health != null)
            m_PlayerController.Health.Died += OnPlayerDied;

        GameManager.Instance.Actors.ActorRemoved += ReleaseSpawnSlot;
        SEventManager.Subscribe<MonsterKilled>(OnMonsterKilled);
        SEventManager.Subscribe<LootPicked>(OnLootPicked);

        if (Map == null)
        {
            CLogger.Warning("BattleService", "전투에 사용할 지역 데이터가 없습니다.");
            return;
        }

        foreach (MapDef.SpawnEntry entry in Map.ListSpawn)
        {
            Transform point = GetSlot(entry.Slot);
            if (point == null || entry.Monster == null || entry.Monster.Prefab == null)
            {
                string monsterName = entry.Monster == null ? "없음" : entry.Monster.name;
                CLogger.Warning("BattleService", $"스폰 불가: {MapDef.GetSlotName(entry.Slot)} 지점 {(point == null ? "없음" : "있음")}, 몬스터 {monsterName}(프리팹은 MonsterDef.Prefab)");
                continue;
            }

            SpawnState spawn = new(entry, point);
            m_ListSpawn.Add(spawn);
            Spawn(spawn);
        }
    }

    public override void Clear()
    {
        GameManager.Instance.Actors.ActorRemoved -= ReleaseSpawnSlot;
        SEventManager.Unsubscribe<MonsterKilled>(OnMonsterKilled);
        SEventManager.Unsubscribe<LootPicked>(OnLootPicked);

        HealthFeature health = m_PlayerController?.Health;
        if (health != null)
            health.Died -= OnPlayerDied;

        m_DicSpawnByActorID.Clear();
        m_ListSpawn.Clear();
        m_DicLoot.Clear();
        m_PlayerController = null;
    }

    public void Retry() => GameManager.Instance?.RetryBattle();

    public bool TryGoLobby()
    {
        GameManager gameM = GameManager.Instance;
        if (gameM == null || Application.CanStreamedLevelBeLoaded(SceneNames.Lobby) == false)
            return false;

        return gameM.ChangeState(EGameState.Lobby);
    }

    #region Spawn
    
    private void SpawnPlayer()
    {
        PlayerDef playerDef = GameManager.Instance.Database?.Get<PlayerDef>(DefaultPlayerID);
        if (playerDef == null || playerDef.Prefab == null)
        {
            CLogger.Warning("BattleService", "PlayerDef에 플레이어 프리팹을 지정하세요.");
            return;
        }

        int playerSlot = Map != null ? Map.PlayerSlot : MapDef.DefaultPlayerSlot;
        Transform spawn = GetSlot(playerSlot);
        if (spawn == null)
            CLogger.Warning("BattleService", $"플레이어 슬롯 {MapDef.GetSlotName(playerSlot)}이 맵에 없어 원점에서 시작합니다.");

        Vector3 position = spawn != null ? SnapToGround(spawn.position) : Vector3.zero;
        Quaternion rotation = spawn != null ? spawn.rotation : Quaternion.identity;

        m_PlayerController = GameManager.Instance.Actors.Spawn(playerDef.Prefab, position, rotation);
    }

    // 슬롯 번호(1부터) → 맵 지점
    private Transform GetSlot(int slot)
    {
        int index = slot - 1;
        if (m_SpawnSlots == null || index < 0 || index >= m_SpawnSlots.Length)
            return null;

        return m_SpawnSlots[index];
    }

    public static Vector3 SnapToGround(Vector3 position)
    {
        // 지점을 땅 위에 세운다. 위에서 아래로 쏴서 지형(Map_Terrain)에 맞은 가장 높은 점을 쓴다(나무·바위 위는 피함)
        
        Vector3 origin = position + Vector3.up * 50f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 100f, ~0, QueryTriggerInteraction.Ignore);

        bool isFound = false;
        float bestY = float.MinValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.name != TerrainName || hit.point.y <= bestY)
                continue;

            bestY = hit.point.y;
            isFound = true;
        }

        if (isFound)
            return new Vector3(position.x, bestY + SpawnLift, position.z);

        // 지형 이름이 다르면 원래 위치 기준 3m 위에서 한 번 더
        if (Physics.Raycast(position + Vector3.up * 3f, Vector3.down, out RaycastHit hitInfo, 30f, ~0, QueryTriggerInteraction.Ignore))
            return hitInfo.point + Vector3.up * SpawnLift;

        return position;
    }

    private void Spawn(SpawnState spawn)
    {
        MonsterController controller = GameManager.Instance.Actors.Spawn(spawn.Entry.Monster.Prefab, SnapToGround(spawn.Point.position), spawn.Point.rotation);
        if (controller == null)
            return;

        m_DicSpawnByActorID.Add(controller.ActorID, spawn);
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
        if (m_PlayerController == null)
            return false;

        return (m_PlayerController.transform.position - position).sqrMagnitude < RespawnBlockDistance * RespawnBlockDistance;
    }

    private void ReleaseSpawnSlot(int actorID)
    {
        if (m_DicSpawnByActorID.Remove(actorID, out SpawnState spawn))
            spawn.IsAlive = false;
    }
    
    #endregion

    private void OnMonsterKilled(MonsterKilled evt)
    {
        if (State != EBattleState.Playing || evt.Actors != GameManager.Instance.Actors)
            return;

        ReleaseSpawnSlot(evt.ActorID);
        KillCount++;
        if (KillCount >= GoalKillCount)
            End(true);
    }

    private void OnLootPicked(LootPicked evt)
    {
        m_DicLoot.TryGetValue(evt.ItemID, out int count);
        m_DicLoot[evt.ItemID] = count + evt.Amount;
    }

    private void OnPlayerDied()
    {
        End(false);
    }
    
    private void End(bool isWin)
    {
        if (State != EBattleState.Playing)
            return;

        State = isWin ? EBattleState.Won : EBattleState.Lost;
        if (m_PlayerController != null)
            m_PlayerController.Health.Died -= OnPlayerDied;

        // [mj] todo) 패배 시 보상 규칙. 지금은 주운 것을 그대로 준다
        PlayerDataManager playerDataM = PlayerDataManager.Instance;
        if (playerDataM != null)
        {
            foreach (var pair in m_DicLoot)
            {
                playerDataM.Inventory.Add(pair.Key, pair.Value);
            }

            // 이긴 지역을 깬 것으로 표시해 다음 지역을 연다(UI 스레드 토벌 탭)
            if (isWin)
                playerDataM.Hunt.MarkCleared(Map);

            playerDataM.RunSave();
        }

        GameManager gameM = GameManager.Instance;
        if (gameM != null && gameM.Current == EGameState.Battle)
            gameM.ChangeState(EGameState.Result);

        SEventManager.Publish(new BattleEnded(isWin));
    }
}
