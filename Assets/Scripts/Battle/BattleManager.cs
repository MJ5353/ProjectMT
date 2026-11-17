using System;
using System.Collections.Generic;
using UnityEngine;

public enum EBattleState
{
    Playing,
    Won,
    Lost,
}

// 전투 한 판 진행. 몬스터 스폰·리스폰, 처치 수, 주운 버섯, 승패를 맡는다.
// 끝나면 주운 버섯을 인벤토리에 넣고 게임 흐름을 결과(Result)로 넘긴다
public class BattleManager : MonoSingleton<BattleManager>
{
    // 한 종류 몬스터을 정해진 지점들에 유지한다(씬의 BattleManager 인스펙터에서 설정)
    // 지점은 이름 접두사(Spawn_Buggi 등)로 플레이 시작 때 씬에서 찾는다. 맵 FBX를 다시 만들어도 끊기지 않게
    [Serializable]
    public class SpawnGroup
    {
        [SerializeField] private MonsterController m_Prefab = null;
        [SerializeField] private string m_PointPrefix = "Spawn_Buggi";   // 스폰 지점 이름 접두사
        [SerializeField] private int m_MaxAlive = 6;                     // 동시에 살아 있는 최대 수
        [SerializeField] private float m_RespawnDelay = 4f;              // 빈자리가 생긴 뒤 다시 나오기까지(초)

        public MonsterController Prefab => m_Prefab;
        public string PointPrefix => m_PointPrefix;
        public int MaxAlive => m_MaxAlive;
        public float RespawnDelay => m_RespawnDelay;

        public SpawnGroup(MonsterController prefab, string pointPrefix, int maxAlive, float respawnDelay)
        {
            m_Prefab = prefab;
            m_PointPrefix = pointPrefix;
            m_MaxAlive = maxAlive;
            m_RespawnDelay = respawnDelay;
        }
    }

    // 스폰 그룹 하나의 플레이 중 상태
    private class SpawnState
    {
        public readonly SpawnGroup Group;
        public readonly List<Transform> ListPoint = new();
        public int AliveCount;
        public float RespawnTimer;

        public SpawnState(SpawnGroup group)
        {
            Group = group;
        }
    }

    public const string TerrainName = "Map_Terrain";
    // !!) 캐릭터 컨트롤러가 바닥에 걸친 채 생기면 한 겹짜리 지형 면을 뚫고 떨어진다. 살짝 띄워서 놓는다
    public const float SpawnLift = 0.3f;
    private const string PlayerSpawnName = "Spawn_Player";   // 시작할 때 플레이어를 옮길 지점 이름
    private const int GoalKillCount = 10;                    // 이만큼 잡으면 승리 (보스가 들어오기 전 임시 조건)

    [SerializeField] private PlayerController m_PlayerPrefab = null;   // 시작할 때 Spawn_Player 지점에 만든다
    [SerializeField] private List<SpawnGroup> m_ListGroup = new();

    private readonly List<SpawnState> m_ListSpawn = new();
    private readonly Dictionary<int, SpawnState> m_DicSpawnByActorID = new();
    private BattleSession m_Session;
    private PlayerController m_PlayerController;

    public EBattleState State => m_Session.State;
    public int KillCount => m_Session.KillCount;
    public int KillGoal => GoalKillCount;
    public float ElapsedTime => m_Session.ElapsedTime;
    public PlayerController Player => m_PlayerController;
    public IReadOnlyDictionary<string, int> DicLoot => m_Session.Loot;

    // ==================================================

    public void Init(BattleSession session)
    {
        if (Instance != null)
            return;

        m_Session = session;
        Init();
    }

    public void Tick(float deltaTime)
    {
        if (State != EBattleState.Playing)
            return;

        m_Session.ElapsedTime += deltaTime;
        UpdateRespawn(deltaTime);
    }

    // virtual---

    protected override void OnInit()
    {
        m_DicSpawnByActorID.Clear();
        m_ListSpawn.Clear();
        SpawnPlayer();
        if (m_PlayerController != null && m_PlayerController.Health != null)
            m_PlayerController.Health.Died += OnPlayerDied;

        m_Session.Actors.ActorRemoved += ReleaseSpawnSlot;
        EventBus.Subscribe<MonsterKilled>(OnMonsterKilled);
        EventBus.Subscribe<LootPicked>(OnLootPicked);

        foreach (SpawnGroup group in m_ListGroup)
        {
            SpawnState spawn = new(group);
            m_ListSpawn.Add(spawn);

            FindPoints(spawn);
            if (group.Prefab == null || spawn.ListPoint.Count == 0)
            {
                CLogger.Warning("BattleManager", $"스폰 불가: 프리팹 {(group.Prefab == null ? "없음" : group.Prefab.name)}, '{group.PointPrefix}' 지점 {spawn.ListPoint.Count}개", this);
                continue;
            }

            CLogger.Log("BattleManager", $"{group.Prefab.name} 스폰 지점 {spawn.ListPoint.Count}개");
            int count = Mathf.Min(group.MaxAlive, spawn.ListPoint.Count);
            for (int k = 0; k < count; k++)
            {
                Spawn(spawn, spawn.ListPoint[k]);
            }
        }
    }

    protected override void OnClear()
    {
        m_Session.Actors.ActorRemoved -= ReleaseSpawnSlot;
        EventBus.Unsubscribe<MonsterKilled>(OnMonsterKilled);
        EventBus.Unsubscribe<LootPicked>(OnLootPicked);

        HealthFeature health = m_PlayerController?.Health;
        if (health != null)
            health.Died -= OnPlayerDied;
    }

    // 다시 하기: 같은 씬을 새로 연다
    public void Retry() => GameManager.Instance?.Flow.RetryBattle();

    // 로비 씬이 있을 때만 true
    public bool TryGoLobby()
    {
        GameManager gameM = GameManager.Instance;
        if (gameM == null || Application.CanStreamedLevelBeLoaded(SceneNames.Lobby) == false)
            return false;

        return gameM.Flow.ChangeState(EGameState.Lobby);
    }

    // ==================================================

    // Spawn_Player 지점(없으면 원점)에 플레이어를 만든다
    private void SpawnPlayer()
    {
        if (m_PlayerPrefab == null)
        {
            CLogger.Warning("BattleManager", "플레이어 프리팹이 비어 있습니다. 메뉴 MushTaker/Battle/전투 씬 만들기로 씬을 다시 만드세요.", this);
            return;
        }

        GameObject spawn = GameObject.Find(PlayerSpawnName);
        Vector3 position = spawn != null ? SnapToGround(spawn.transform.position) : Vector3.zero;
        Quaternion rotation = spawn != null ? spawn.transform.rotation : Quaternion.identity;

        m_PlayerController = m_Session.Actors.Spawn(m_PlayerPrefab, position, rotation);
    }

    private static void FindPoints(SpawnState spawn)
    {
        // 이름 접두사로 스폰 지점을 찾는다
        string prefix = spawn.Group.PointPrefix;
        if (prefix.E_IsNullOrEmpty())
            return;

        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Transform t in all)
        {
            if (t.name.StartsWith(prefix))
                spawn.ListPoint.Add(t);
        }

        spawn.ListPoint.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
    }

    // 지점을 땅 위에 세운다. 위에서 아래로 쏴서 지형(Map_Terrain)에 맞은 가장 높은 점을 쓴다(나무·바위 위는 피함)
    public static Vector3 SnapToGround(Vector3 position)
    {
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

        MonsterController controller = m_Session.Actors.Spawn(spawn.Group.Prefab, SnapToGround(point.position), point.rotation);
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

    private void OnMonsterKilled(MonsterKilled evt)
    {
        if (State != EBattleState.Playing || evt.Actors != m_Session.Actors)
            return;

        ReleaseSpawnSlot(evt.ActorID);
        m_Session.KillCount++;
        if (KillCount >= GoalKillCount)
            End(true);
    }

    private void ReleaseSpawnSlot(int actorID)
    {
        if (m_DicSpawnByActorID.Remove(actorID, out SpawnState spawn))
            spawn.AliveCount--;
    }

    private void OnLootPicked(LootPicked evt)
    {
        m_Session.Loot.TryGetValue(evt.ItemID, out int count);
        m_Session.Loot[evt.ItemID] = count + evt.Amount;
    }

    private void OnPlayerDied()
    {
        End(false);
    }

    private void End(bool isWin)
    {
        if (State != EBattleState.Playing)
            return;

        m_Session.State = isWin ? EBattleState.Won : EBattleState.Lost;
        if (m_PlayerController != null)
            m_PlayerController.Health.Died -= OnPlayerDied;

        // [mj] todo) 패배 시 보상 규칙. 지금은 주운 것을 그대로 준다
        GameManager gameM = GameManager.Instance;
        if (gameM != null)
        {
            foreach (var pair in m_Session.Loot)
            {
                gameM.Player.Inventory.Add(pair.Key, pair.Value);
            }

            // 이긴 지역을 깬 것으로 표시해 다음 지역을 연다(UI 스레드 토벌 탭)
            if (isWin)
                HuntContext.MarkCleared(HuntContext.SelectedRegion);

            gameM.RunSave();
            if (gameM.Flow.Current == EGameState.Battle)
                gameM.Flow.ChangeState(EGameState.Result);
        }

        EventBus.Publish(new BattleEnded(isWin));
    }
}
