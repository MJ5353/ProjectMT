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
    private const float SnapRayHeight = 50f; // 지점 위 이 높이에서 아래로 쏜다
    private const float SnapRayDistance = 100f;
    private const float SnapFallbackHeight = 3f; // 지형 이름이 다를 때 지점 위 이 높이까지만 땅으로 본다(나무 꼭대기 방지)
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

        Vector3 position = spawn != null ? SnapToGround(spawn, playerDef.Prefab) : Vector3.zero;
        Quaternion rotation = spawn != null ? GetSpawnRotation(spawn) : Quaternion.identity;

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

    public static Vector3 SnapToGround(Transform slot, Component prefab)
    {
        // 지점을 땅 위에 세운다. 위에서 아래로 쏴서 지형(Map_Terrain)에 맞은 가장 높은 점을 쓴다(나무·바위 위는 피함)
        // 지형 이름이 다른 맵이면 지점 3m 위 아래쪽에서 가장 높은 점. 캐릭터 컨트롤러(다른 몬스터·플레이어)는 무시

        Vector3 position = slot.position;
        Vector3 origin = position + Vector3.up * SnapRayHeight;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, SnapRayDistance, ~0, QueryTriggerInteraction.Ignore);

        float terrainY = float.MinValue;
        float fallbackY = float.MinValue;
        float fallbackTop = position.y + SnapFallbackHeight;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider is CharacterController)
                continue;

            if (hit.collider.name == TerrainName)
                terrainY = Mathf.Max(terrainY, hit.point.y);
            else if (hit.point.y <= fallbackTop)
                fallbackY = Mathf.Max(fallbackY, hit.point.y);
        }

        float groundY = terrainY > float.MinValue ? terrainY : fallbackY;
        if (groundY == float.MinValue)
        {
            CLogger.Warning("BattleService", $"{slot.name} {position}: 아래에 땅이 없어 원래 위치에 놓습니다.", slot);
            return position;
        }

        return new Vector3(position.x, groundY + GetFootOffset(prefab) + SpawnLift, position.z);
    }

    // 지점의 수평 방향(yaw)만 쓴다
    // !!) 맵 FBX의 SpawnSlot은 X 90도로 누워 있다. 그대로 쓰면 캐릭터 컨트롤러 center도 같이 돌아 캡슐이 땅 밑에서 시작해 떨어진다
    public static Quaternion GetSpawnRotation(Transform slot)
    {
        Vector3 forward = Vector3.ProjectOnPlane(slot.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.ProjectOnPlane(slot.up, Vector3.up); // 앞이 수직이면(누운 지점) 위 축이 수평 방향

        return forward.sqrMagnitude < 0.01f ? Quaternion.identity : Quaternion.LookRotation(forward, Vector3.up);
    }

    // 캡슐 바닥이 피벗보다 아래면 그만큼 올린다(피벗이 발 밑이면 0)
    private static float GetFootOffset(Component prefab)
    {
        if (prefab == null || prefab.TryGetComponent(out CharacterController cc) == false)
            return 0f;

        float bottom = (cc.center.y - cc.height * 0.5f) * cc.transform.lossyScale.y;
        return Mathf.Max(0f, -bottom);
    }

    private void Spawn(SpawnState spawn)
    {
        MonsterController controller = GameManager.Instance.Actors.Spawn(spawn.Entry.Monster.Prefab, SnapToGround(spawn.Point, spawn.Entry.Monster.Prefab), GetSpawnRotation(spawn.Point));
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
