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
    // 한 종류 몹을 정해진 지점들에 유지한다
    [Serializable]
    public class SpawnGroup
    {
        [SerializeField] private MobController m_Prefab = null;
        [SerializeField] private List<Transform> m_ListPoint = new();
        [SerializeField] private int m_MaxAlive = 6;
        [SerializeField] private float m_RespawnDelay = 4f;

        public MobController Prefab => m_Prefab;
        public IReadOnlyList<Transform> ListPoint => m_ListPoint;
        public int MaxAlive => m_MaxAlive;
        public float RespawnDelay => m_RespawnDelay;

        public SpawnGroup(MobController prefab, List<Transform> listPoint, int maxAlive, float respawnDelay)
        {
            m_Prefab = prefab;
            m_ListPoint = listPoint;
            m_MaxAlive = maxAlive;
            m_RespawnDelay = respawnDelay;
        }
    }

    [SerializeField] private PlayerController m_Player = null;
    [SerializeField] private List<SpawnGroup> m_ListGroup = new();
    [SerializeField, Tooltip("이만큼 잡으면 승리 (보스가 들어오기 전 임시 조건)")] private int m_KillGoal = 10;

    private readonly Dictionary<string, int> m_DicLoot = new();
    private readonly List<int> m_ListAlive = new();
    private readonly List<float> m_ListRespawnTimer = new();
    private readonly Dictionary<MobController, int> m_DicGroupByMob = new();

    public static BattleManager Instance { get; private set; }

    public EBattleState State { get; private set; }
    public int KillCount { get; private set; }
    public int KillGoal => m_KillGoal;
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
        m_DicGroupByMob.Clear();
        m_ListAlive.Clear();
        m_ListRespawnTimer.Clear();

        if (m_Player == null)
            m_Player = PlayerController.Current;
        if (m_Player != null)
            m_Player.Health.Died += OnPlayerDied;

        for (int i = 0; i < m_ListGroup.Count; i++)
        {
            m_ListAlive.Add(0);
            m_ListRespawnTimer.Add(0f);

            SpawnGroup group = m_ListGroup[i];
            int count = Mathf.Min(group.MaxAlive, group.ListPoint.Count);
            for (int k = 0; k < count; k++)
            {
                Spawn(i, group.ListPoint[k]);
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

    private void Spawn(int groupIndex, Transform point)
    {
        SpawnGroup group = m_ListGroup[groupIndex];
        if (group.Prefab == null || point == null)
            return;

        MobController mob = Instantiate(group.Prefab, point.position, point.rotation);
        m_DicGroupByMob[mob] = groupIndex;
        m_ListAlive[groupIndex]++;
    }

    private void UpdateRespawn(float deltaTime)
    {
        for (int i = 0; i < m_ListGroup.Count; i++)
        {
            SpawnGroup group = m_ListGroup[i];
            if (m_ListAlive[i] >= group.MaxAlive || group.ListPoint.Count == 0)
                continue;

            m_ListRespawnTimer[i] += deltaTime;
            if (m_ListRespawnTimer[i] < group.RespawnDelay)
                continue;

            m_ListRespawnTimer[i] = 0f;
            Spawn(i, PickFarPoint(group));
        }
    }

    // 플레이어 눈앞에 갑자기 생기지 않게 가장 먼 지점 근처에서 고른다
    private Transform PickFarPoint(SpawnGroup group)
    {
        if (m_Player == null)
            return group.ListPoint[UnityEngine.Random.Range(0, group.ListPoint.Count)];

        Transform best = null;
        float bestDistance = -1f;
        foreach (Transform point in group.ListPoint)
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
        foreach (var pair in m_DicGroupByMob)
        {
            if (pair.Key != null && pair.Key.IsDead == false)
                continue;

            m_ListAlive[pair.Value] = Mathf.Max(0, m_ListAlive[pair.Value] - 1);
            m_DicGroupByMob.Remove(pair.Key);
            break;
        }

        if (KillCount >= m_KillGoal)
            End(true);
    }

    private void OnLootPicked(LootPicked evt)
    {
        m_DicLoot.TryGetValue(evt.ItemId, out int count);
        m_DicLoot[evt.ItemId] = count + evt.Amount;
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

            gameM.SaveNow();
            if (gameM.Flow.Current == EGameState.Battle)
                gameM.Flow.ChangeState(EGameState.Result);
        }

        EventBus.Publish(new BattleEnded(isWin));
    }
}
