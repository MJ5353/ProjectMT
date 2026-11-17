using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum EBattleState
{
    Playing,
    Won,
    Lost,
}

// 전투 한 판 진행. 몹 스폰·리스폰, 처치 수, 주운 버섯, 승패를 맡는다.
// 끝나면 주운 버섯을 인벤토리에 넣고 게임 흐름을 결과(Result)로 넘긴다
public class BattleManager : MonoBehaviour
{
    // 한 종류 몹을 정해진 지점들에 유지한다(씬의 BattleManager 인스펙터에서 설정)
    // 지점은 이름 접두사(Spawn_Buggi 등)로 플레이 시작 때 씬에서 찾는다. 맵 FBX를 다시 만들어도 끊기지 않게
    [Serializable]
    public class SpawnGroup
    {
        [SerializeField] private MobController m_Prefab = null;
        [SerializeField] private string m_PointPrefix = "Spawn_Buggi";   // 스폰 지점 이름 접두사
        [SerializeField] private int m_MaxAlive = 6;                     // 동시에 살아 있는 최대 수
        [SerializeField] private float m_RespawnDelay = 4f;              // 빈자리가 생긴 뒤 다시 나오기까지(초)

        public MobController Prefab => m_Prefab;
        public string PointPrefix => m_PointPrefix;
        public int MaxAlive => m_MaxAlive;
        public float RespawnDelay => m_RespawnDelay;

        public SpawnGroup(MobController prefab, string pointPrefix, int maxAlive, float respawnDelay)
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

    [SerializeField] private List<SpawnGroup> m_ListGroup = new();

    private readonly Dictionary<string, int> m_DicLoot = new();
    private readonly List<SpawnState> m_ListSpawn = new();
    private readonly Dictionary<MobController, SpawnState> m_DicSpawnByMob = new();
    private PlayerController m_Player;

    public static BattleManager Instance { get; private set; }

    public EBattleState State { get; private set; }
    public int KillCount { get; private set; }
    public int KillGoal => GoalKillCount;
    public float ElapsedTime { get; private set; }
    public PlayerController Player => m_Player;
    public IReadOnlyDictionary<string, int> DicLoot => m_DicLoot;

    // ==================================================

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<MobKilled>(OnMobKilled);
        EventBus.Subscribe<LootPicked>(OnLootPicked);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<MobKilled>(OnMobKilled);
        EventBus.Unsubscribe<LootPicked>(OnLootPicked);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        Init();
    }

    private void Update()
    {
        if (State != EBattleState.Playing)
            return;

        float deltaTime = Time.deltaTime;
        ElapsedTime += deltaTime;
        UpdateRespawn(deltaTime);
    }

    public void Init()
    {
        State = EBattleState.Playing;
        KillCount = 0;
        ElapsedTime = 0f;
        m_DicLoot.Clear();
        m_DicSpawnByMob.Clear();
        m_ListSpawn.Clear();

        m_Player = PlayerController.Current;

        if (m_Player != null)
        {
            m_Player.Health.Died += OnPlayerDied;
            PlacePlayer();
        }

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

    // 다시 하기: 같은 씬을 새로 연다
    public void Retry()
    {
        GameManager gameM = GameManager.Instance;
        if (gameM != null && gameM.Flow.Current == EGameState.Result)
            gameM.Flow.ChangeState(EGameState.Battle);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 로비 씬이 있을 때만 true
    public bool TryGoLobby()
    {
        GameManager gameM = GameManager.Instance;
        if (gameM == null || Application.CanStreamedLevelBeLoaded(SceneNames.Lobby) == false)
            return false;

        return gameM.Flow.ChangeState(EGameState.Lobby);
    }

    // ==================================================

    private void PlacePlayer()
    {
        GameObject spawn = GameObject.Find(PlayerSpawnName);
        if (spawn == null)
            return;

        // CharacterController가 켜져 있으면 위치를 덮어써 버려서 잠깐 끈다
        // !!) 보류: GetComponent 대신 PlayerController.Teleport()로 바꿀지 검토
        CharacterController controller = m_Player.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        m_Player.transform.SetPositionAndRotation(SnapToGround(spawn.transform.position), spawn.transform.rotation);
        if (controller != null)
            controller.enabled = true;
    }

    // 이름 접두사로 스폰 지점을 찾는다
    private static void FindPoints(SpawnState spawn)
    {
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

        MobController mob = Instantiate(spawn.Group.Prefab, SnapToGround(point.position), point.rotation);
        m_DicSpawnByMob[mob] = spawn;
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

    // 플레이어 눈앞에 갑자기 생기지 않게 가장 먼 지점 근처에서 고른다
    private Transform PickFarPoint(SpawnState spawn)
    {
        if (m_Player == null)
            return spawn.ListPoint[UnityEngine.Random.Range(0, spawn.ListPoint.Count)];

        Transform best = null;
        float bestDistance = -1f;
        foreach (Transform point in spawn.ListPoint)
        {
            float distance = Vector3.Distance(point.position, m_Player.transform.position) + UnityEngine.Random.Range(0f, 4f);
            if (distance <= bestDistance)
                continue;

            best = point;
            bestDistance = distance;
        }

        return best;
    }

    private void OnMobKilled(MobKilled evt)
    {
        if (State != EBattleState.Playing)
            return;

        KillCount++;
        foreach (var pair in m_DicSpawnByMob)
        {
            if (pair.Key != null && pair.Key.IsDead == false)
                continue;

            pair.Value.AliveCount = Mathf.Max(0, pair.Value.AliveCount - 1);
            m_DicSpawnByMob.Remove(pair.Key);
            break;
        }

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
        if (m_Player != null)
            m_Player.Health.Died -= OnPlayerDied;

        // [mj] todo) 패배 시 보상 규칙 미정. 지금은 주운 것을 그대로 준다
        GameManager gameM = GameManager.Instance;
        if (gameM != null)
        {
            foreach (var pair in m_DicLoot)
            {
                gameM.Player.Inventory.Add(pair.Key, pair.Value);
            }

            // 이긴 지역을 깬 것으로 표시해 다음 지역을 연다(UI 스레드 토벌 탭)
            if (isWin)
                HuntContext.MarkCleared(HuntContext.SelectedRegion);

            gameM.SaveNow();
            if (gameM.Flow.Current == EGameState.Battle)
                gameM.Flow.ChangeState(EGameState.Result);
        }

        EventBus.Publish(new BattleEnded(isWin));
    }
}
