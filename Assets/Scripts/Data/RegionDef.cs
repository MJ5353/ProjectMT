using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// 토벌 지역 정의. 로비 토벌 지도에 노드로 찍히고, 골라서 입장하면 전투가 시작된다.
// 깬 지역은 플래그(ClearFlag)로 저장하고, 앞 지역을 깨면 다음 지역이 열린다
[CreateAssetMenu(menuName = "MushTaker/Data/Region", fileName = "Region_")]
public class RegionDef : DataDef
{
    // 맵 슬롯 하나에 나올 몬스터
    [Serializable]
    public class SpawnEntry
    {
        [field: SerializeField, Tooltip("맵 슬롯 번호(SpawnSlot_NN의 NN)")]
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

    public const string SpawnSlotPrefix = "SpawnSlot_"; // 맵 FBX의 스폰 지점 이름(SpawnSlot_01~)
    public const int DefaultPlayerSlot = 1;

    [field: SerializeField, FormerlySerializedAs("m_Order")]
    public int Order { get; private set; } = 0;

    [field: SerializeField, Tooltip("이 지역을 깨야 열린다. 비우면 처음부터 열림"), FormerlySerializedAs("m_RequireRegion")]
    public RegionDef RequireRegion { get; private set; } = null;

    [field: SerializeField, Tooltip("토벌 지도 위 위치(0~1)"), FormerlySerializedAs("m_MapPosition")]
    public Vector2 MapPosition { get; private set; } = new Vector2(0.5f, 0.5f);

    [SerializeField, Tooltip("이 지역에서 캘 수 있는 버섯(지도 안내용)")]
    private List<MushroomDef> m_ListMushroom = new();

    [field: SerializeField, Tooltip("플레이어가 시작할 맵 슬롯 번호")]
    public int PlayerSlot { get; private set; } = DefaultPlayerSlot;

    [SerializeField]
    private List<SpawnEntry> m_ListSpawn = new();

    public IReadOnlyList<SpawnEntry> ListSpawn => m_ListSpawn;
    public IReadOnlyList<MushroomDef> ListMushroom => m_ListMushroom;
    public string ClearFlag => $"Region_Clear_{ID}";

    // ------------------------------------------------

    public static string GetSlotName(int slot) => $"{SpawnSlotPrefix}{slot:00}";

    // SpawnSlot_NN이면 NN을 돌려준다
    public static bool TryParseSlot(string objectName, out int slot)
    {
        slot = 0;
        return objectName.StartsWith(SpawnSlotPrefix, StringComparison.Ordinal)
            && int.TryParse(objectName.Substring(SpawnSlotPrefix.Length), out slot);
    }

    // ------------------------------------------------

#if UNITY_EDITOR
    public void EditorSetSpawns(List<SpawnEntry> listSpawn) => m_ListSpawn = listSpawn;

    public void EditorSetup(int order, RegionDef requireRegion, Vector2 mapPosition, List<MushroomDef> listMushroom)
    {
        Order = order;
        RequireRegion = requireRegion;
        MapPosition = mapPosition;
        m_ListMushroom = listMushroom;
    }
#endif
}
