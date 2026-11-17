using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    // 스폰 그룹 하나의 플레이 중 상태
    private class SpawnState
    {
        public readonly RegionDef.SpawnGroup Group;
        public readonly List<Transform> ListPoint = new();
        public int AliveCount;
        public float RespawnTimer;

        // ------------------------------------------------

        public SpawnState(RegionDef.SpawnGroup group)
        {
            Group = group;
        }
    }

    public const float SpawnLift = 0.3f; // !!) 캐릭터 컨트롤러가 바닥에 걸친 채 생기면 한 겹짜리 지형 면을 뚫고 떨어진다. 살짝 띄워서 놓는다
    public const string TerrainName = "Map_Terrain";
    private const string PlayerSpawnName = "Spawn_Player"; // 시작할 때 플레이어를 옮길 지점 이름
    private const int GoalKillCount = 10; // 이만큼 잡으면 승리 (보스가 들어오기 전 임시 조건)
    private const string DefaultPlayerID = "Player";

    private readonly Scene m_Scene;
    private readonly Dictionary<string, int> m_DicLoot = new();
    private readonly List<SpawnState> m_ListSpawn = new();

    public IReadOnlyDictionary<string, int> DicLoot => m_DicLoot;
    private readonly Dictionary<int, SpawnState> m_DicSpawnByActorID = new();

    public PlayerController Player => m_PlayerController;
    private PlayerController m_PlayerController;

    public RegionDef Region { get; }
    public EBattleState State { get; private set; } = EBattleState.Playing;
    public int KillCount { get; private set; }
    public int KillGoal => GoalKillCount;
    public float ElapsedTime { get; private set; }

    // ------------------------------------------------

    public BattleService(Scene scene, RegionDef region)
    {
        m_Scene = scene;
        Region = region;
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

        if (Region == null)
        {
            CLogger.Warning("BattleService", "전투에 사용할 지역 데이터가 없습니다.");
            return;
        }

        foreach (RegionDef.SpawnGroup group in Region.ListSpawnGroup)
        {
            SpawnState spawn = new(group);
            m_ListSpawn.Add(spawn);

            FindPoints(spawn);
            if (group.Prefab == null || spawn.ListPoint.Count == 0)
            {
                CLogger.Warning("BattleService", $"스폰 불가: 프리팹 {(group.Prefab == null ? "없음" : group.Prefab.name)}, '{group.PointPrefix}' 지점 {spawn.ListPoint.Count}개");
                continue;
            }

            CLogger.Log("BattleService", $"{group.Prefab.name} 스폰 지점 {spawn.ListPoint.Count}개");
            int count = Mathf.Min(group.MaxAlive, spawn.ListPoint.Count);
            for (int k = 0; k < count; k++)
            {
                Spawn(spawn, spawn.ListPoint[k]);
            }
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

        GameObject spawn = GameObject.Find(PlayerSpawnName);
        Vector3 position = spawn != null ? SnapToGround(spawn.transform.position) : Vector3.zero;
        Quaternion rotation = spawn != null ? spawn.transform.rotation : Quaternion.identity;

        m_PlayerController = GameManager.Instance.Actors.Spawn(playerDef.Prefab, position, rotation);
    }

    private void FindPoints(SpawnState spawn)
    {
        // 이름 접두사로 스폰 지점을 찾는다
        string prefix = spawn.Group.PointPrefix;
        if (prefix.E_IsNullOrEmpty())
            return;

        Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Transform t in all)
        {
            if (t.gameObject.scene == m_Scene && t.name.StartsWith(prefix, StringComparison.Ordinal))
                spawn.ListPoint.Add(t);
        }

        spawn.ListPoint.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
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

    private void Spawn(SpawnState spawn, Transform point)
    {
        if (spawn.Group.Prefab == null || point == null)
            return;

        MonsterController controller = GameManager.Instance.Actors.Spawn(spawn.Group.Prefab, SnapToGround(point.position), point.rotation);
        if (controller == null)
            return;

        m_DicSpawnByActorID.Add(controller.ActorID, spawn);
        spawn.AliveCount++;
    }

    private void UpdateRespawn(float deltaTime)
    {
        foreach (SpawnState spawn in m_ListSpawn)
        {
            if (spawn.AliveCount >= spawn.Group.MaxAlive || spawn.ListPoint.Count == 0)
                continue;

            spawn.RespawnTimer += deltaTime;
            if (spawn.RespawnTimer < spawn.Group.RespawnDelay)
                continue;

            spawn.RespawnTimer = 0f;
            Spawn(spawn, PickFarPoint(spawn));
        }
    }

    private void ReleaseSpawnSlot(int actorID)
    {
        if (m_DicSpawnByActorID.Remove(actorID, out SpawnState spawn))
            spawn.AliveCount--;
    }

    private Transform PickFarPoint(SpawnState spawn)
    {
        // 플레이어 눈앞에 갑자기 생기지 않게 가장 먼 지점 근처에서 고른다
        if (m_PlayerController == null)
            return spawn.ListPoint[UnityEngine.Random.Range(0, spawn.ListPoint.Count)];

        Transform best = null;
        float bestDistance = -1f;

        foreach (Transform point in spawn.ListPoint)
        {
            float distance = Vector3.Distance(point.position, m_PlayerController.transform.position) + UnityEngine.Random.Range(0f, 4f);
            if (distance <= bestDistance)
                continue;

            best = point;
            bestDistance = distance;
        }

        return best;
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
                playerDataM.Hunt.MarkCleared(Region);

            playerDataM.RunSave();
        }

        GameManager gameM = GameManager.Instance;
        if (gameM != null && gameM.Current == EGameState.Battle)
            gameM.ChangeState(EGameState.Result);

        SEventManager.Publish(new BattleEnded(isWin));
    }
}
