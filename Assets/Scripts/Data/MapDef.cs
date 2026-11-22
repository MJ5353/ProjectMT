using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// ==================================================

// 토벌 지역 정의. 로비 토벌 지도에 노드로 찍히고, 골라서 입장하면 전투가 시작된다.
// 깬 지역은 플래그(ClearFlag)로 저장하고, 앞 지역을 깨면 다음 지역이 열린다
[CreateAssetMenu(menuName = "MushTaker/Data/Map", fileName = "Map_")]
public class MapDef : DataDef
{
    // 맵 슬롯 하나에 나올 몬스터
    [Serializable]
    public class SpawnEntry
    {
        [field: SerializeField, Tooltip("맵 슬롯 번호(SpawnSlot_NNN의 NNN)")]
        public int Slot { get; private set; } = 0;

        [field: SerializeField]
        public MonsterDef Monster { get; private set; } = null;

        [field: SerializeField, Tooltip("죽은 뒤 다시 나오기까지(초). 0보다 작으면 다시 안 나옴(보스)")]
        public float RespawnDelay { get; private set; } = 0f;

        // ------------------------------------------------

        public SpawnEntry(int slot, MonsterDef monster, float respawnDelay)
        {
            Slot = slot;
            Monster = monster;
            RespawnDelay = respawnDelay;
        }
    }

    // 희귀도 하나의 드롭 가중치
    [Serializable]
    public class RarityWeight
    {
        [field: SerializeField]
        public ERarity Rarity { get; private set; } = ERarity.Common;

        [field: SerializeField, Tooltip("다른 희귀도와의 비율. 합이 100일 필요는 없다")]
        public float Weight { get; private set; } = 0f;

        // ------------------------------------------------

        public RarityWeight(ERarity rarity, float weight)
        {
            Rarity = rarity;
            Weight = weight;
        }
    }

    public const string SpawnSlotPrefix = "SpawnSlot_"; // 맵 FBX의 스폰 지점 이름(SpawnSlot_001~)
    public const int DefaultPlayerSlot = 1;

    public IReadOnlyList<SpawnEntry> ListSpawn => m_ListSpawn;
    public IReadOnlyList<MushroomDef> ListMushroom => m_ListMushroom;
    public IReadOnlyList<RarityWeight> ListRarityWeight => m_ListRarityWeight;
    public IReadOnlyList<StatModifier> ListMonsterModifier => m_ListMonsterModifier;
    public string ClearFlag => $"Clear_{ID}";
    public string PrefabPath => CommonPath.MapDir + ID; // 전투 때 생성할 맵. MapPrefabBuilder가 ID와 같은 이름으로 굽는다

    // data ---
    
    [field: SerializeField, FormerlySerializedAs("m_Order")]
    public int Order { get; private set; } = 0;

    [field: SerializeField, Tooltip("이 지역을 깨야 열린다. 비우면 처음부터 열림")]
    public MapDef RequireMap { get; private set; } = null;

    [field: SerializeField, Tooltip("토벌 지도 위 위치(0~1)"), FormerlySerializedAs("m_MapPosition")]
    public Vector2 MapPosition { get; private set; } = new Vector2(0.5f, 0.5f);

    [SerializeField, Tooltip("이 지역에서 캘 수 있는 버섯(지도 안내용)")]
    private List<MushroomDef> m_ListMushroom = new();

    [field: SerializeField, Tooltip("플레이어가 시작할 맵 슬롯 번호")]
    public int PlayerSlot { get; private set; } = DefaultPlayerSlot;

    [field: SerializeField]
    public float NightTime { get; private set; } = 180f; // 밤 길이(초). 다 되면 쫓겨나고 주운 버섯은 썩은 버섯이 된다.

    [SerializeField]
    private List<SpawnEntry> m_ListSpawn = new();

    [SerializeField, Tooltip("이 지역에서 떨어지는 버섯의 희귀도 가중치. [mj] todo) 가안")]
    private List<RarityWeight> m_ListRarityWeight = new()
    {
        new RarityWeight(ERarity.Common, 80f),
        new RarityWeight(ERarity.Rare, 17f),
        new RarityWeight(ERarity.Epic, 3f),
    };

    [SerializeField, Tooltip("이 지역 몬스터 난이도. 스폰 때 능력치 출처로 들어간다(예: 최대 체력 Rate 1 = 체력 ×2)")]
    private List<StatModifier> m_ListMonsterModifier = new();

    // ------------------------------------------------

    // 떨어지는 버섯 하나의 희귀도를 가중치로 뽑는다. 표가 비었거나 가중치가 모두 0이면 Common
    public ERarity RollRarity()
    {
        int index = CRandom.PickIndex(m_ListRarityWeight, entry => entry.Weight);
        return index >= 0 ? m_ListRarityWeight[index].Rarity : ERarity.Common;
    }

    public static string GetSlotName(int slot) => $"{SpawnSlotPrefix}{slot:000}";

    // SpawnSlot_NN이면 NN을 돌려준다
    public static bool TryParseSlot(string objectName, out int slot)
    {
        slot = 0;
        return objectName.StartsWith(SpawnSlotPrefix, StringComparison.Ordinal)
            && int.TryParse(objectName.Substring(SpawnSlotPrefix.Length), out slot);
    }

#if UNITY_EDITOR
    public void EditorSetSpawns(List<SpawnEntry> listSpawn) => m_ListSpawn = listSpawn;

    public void EditorSetup(int order, MapDef requireMap, Vector2 mapPosition, List<MushroomDef> listMushroom)
    {
        Order = order;
        RequireMap = requireMap;
        MapPosition = mapPosition;
        m_ListMushroom = listMushroom;
    }
#endif
}
