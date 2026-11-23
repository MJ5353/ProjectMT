using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 맵 슬롯에서 유닛을 만든다. 플레이어(정비 강화 적용)·몬스터(지역 배율 적용)를 놓고, 죽어 빈 슬롯은 시간이 지나면 다시 채운다.
// 언제 할지는 BattleFlow 상태가 정한다(Start: SpawnAll, Play: UpdateRespawn). 만든 유닛 목록·플레이어는 UnitService가 든다
public sealed class SpawnService : ServiceBase
{
    public const float SpawnLift = 0.3f; // !!) 캐릭터 컨트롤러가 바닥에 걸친 채 생기면 한 겹짜리 지형 면을 뚫고 떨어진다. 살짝 띄워서 놓는다
    private const string DefaultPlayerID = "Player";

    private readonly List<SpawnState> m_ListSpawn = new();
    private readonly Dictionary<int, SpawnState> m_DicSpawnByUnitID = new();
    private readonly MapDef m_Map;
    private readonly MapRoot m_MapRoot; // 씬 컨트롤러가 만들어 넘긴 맵. 다시 하기에도 같은 맵을 쓴다

    private static DefineDef Define => GameManager.Instance.Define;

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

    // ------------------------------------------------

    public SpawnService(MapDef mapDef, MapRoot map)
    {
        m_Map = mapDef;
        m_MapRoot = map;
    }

    // virtual ---

    public override void Init()
    {
        FlowManager.Instance.Battle.UnitS.UnitRemoved += ReleaseSpawnSlot;
    }

    public override void Clear()
    {
        FlowManager.Instance.Battle.UnitS.UnitRemoved -= ReleaseSpawnSlot;

        m_DicSpawnByUnitID.Clear();
        m_ListSpawn.Clear();
    }

    // ---

    // 플레이어·몬스터를 만든다(맵은 씬 컨트롤러가 이미 만들었다). BattleFlow Start 상태가 부른다
    public void SpawnAll()
    {
        SpawnPlayer();

        if (m_Map == null)
        {
            CLogger.Warning("SpawnService", "전투에 사용할 지역 데이터가 없습니다.");
            return;
        }

        foreach (MapDef.SpawnEntry entry in m_Map.ListSpawn)
        {
            Transform point = GetSlot(entry.Slot);
            
            // 프리팹이 없으면 UnitFactory가 오류 로그(경로는 MonsterDef.PrefabPath)
            if (point == null || entry.Monster == null)
            {
                string monsterName = entry.Monster.E_Alive()?.name ?? "없음";
                CLogger.Warning("SpawnService", $"스폰 불가: {MapDef.GetSlotName(entry.Slot)} 지점 {(point == null ? "없음" : "있음")}, 몬스터 {monsterName}");

                continue;
            }

            SpawnState spawn = new(entry, point);
            m_ListSpawn.Add(spawn);
            
            SpawnMonster(spawn);
        }
    }

    // 비어 있는 슬롯을 다시 채운다. 밤과 함께 Play 상태만 부른다(쓰러짐·시장·결과 동안은 멈춘다)
    public void UpdateRespawn(float deltaTime)
    {
        foreach (SpawnState spawn in m_ListSpawn)
        {
            if (spawn.IsAlive || spawn.Entry.RespawnDelay < 0f)
                continue;

            spawn.RespawnTimer += deltaTime;
            if (spawn.RespawnTimer < spawn.Entry.RespawnDelay || IsPlayerNear(spawn.Point.position))
                continue;

            SpawnMonster(spawn);
        }
    }

    // 지점의 수평 방향(yaw)만 쓴다
    public static Quaternion GetSpawnRotation(Transform slot)
    {
        // [mj] todo) fbx를 고치는 방법은?
        // !!) 맵 FBX의 SpawnSlot은 X 90도로 누워 있다. 그대로 쓰면 캐릭터 컨트롤러 center도 같이 돌아 캡슐이 땅 밑에서 시작해 떨어진다
        
        Vector3 forward = Vector3.ProjectOnPlane(slot.forward, Vector3.up);

        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.ProjectOnPlane(slot.up, Vector3.up); // 앞이 수직이면(누운 지점) 위 축이 수평 방향

        return forward.sqrMagnitude < 0.01f ? Quaternion.identity : Quaternion.LookRotation(forward, Vector3.up);
    }

    // spawn ---
    
    private void SpawnPlayer()
    {
        if (GameDatabase.S_TryGet<PlayerDef>(DefaultPlayerID, out PlayerDef playerDef) == false)
        {
            CLogger.Warning("SpawnService", $"PlayerDef({DefaultPlayerID})가 DB에 없습니다.");
            return;
        }

        int playerSlot = m_Map.E_Alive()?.PlayerSlot ?? MapDef.DefaultPlayerSlot;
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
            CLogger.Warning("SpawnService", $"플레이어 슬롯 {MapDef.GetSlotName(playerSlot)}이 맵에 없어 원점에서 시작합니다.");
        }

        UnitController player = FlowManager.Instance.Battle.UnitS.Spawn(playerDef, position, rotation);
        if (player == null)
            return;

        ApplyGear(player, PlayerDataManager.Instance.Gear);
    }

    // 정비 강화를 출처로 넣는다. 연 노드(능력치·패시브)와 지금 단계까지 엠블렘(능력치)이 각각 출처 하나.
    // 늘어난 최대 체력만큼 체력도 찬다(HealthFeature가 StatFeature.Changed로)
    private static void ApplyGear(UnitController player, GearData gear)
    {
        foreach (GearNodeDef node in gear.ListNode)
        {
            if (gear.IsOpened(node) == false)
                continue;

            player.StatF.AddSource(node, node.ListModifier);
            player.PassiveF.AddSource(node, node.Passive);
        }

        foreach (EmblemDef emblem in gear.GetReachedEmblems())
        {
            player.StatF.AddSource(emblem, emblem.ListModifier);
        }
    }

    private void SpawnMonster(SpawnState spawn)
    {
        UnitController controller = FlowManager.Instance.Battle.UnitS.Spawn(spawn.Entry.Monster, spawn.Point.position + Vector3.up * SpawnLift, GetSpawnRotation(spawn.Point));
        if (controller == null)
            return;

        controller.StatF.AddSource(m_Map, m_Map.ListMonsterModifier); // 지역 난이도(체력·공격 배율)
        
        m_DicSpawnByUnitID.Add(controller.UnitID, spawn);
        spawn.IsAlive = true;
        spawn.RespawnTimer = 0f;
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

    private bool IsPlayerNear(Vector3 position)
    {
        UnitController player = FlowManager.Instance.Battle.UnitS.Player;
        if (player == null)
            return false;

        float distance = Define.Battle.RespawnBlockDistance;
        return (player.transform.position - position).sqrMagnitude < distance * distance;
    }

    private void ReleaseSpawnSlot(int unitID)
    {
        if (m_DicSpawnByUnitID.Remove(unitID, out SpawnState spawn))
            spawn.IsAlive = false;
    }
}
