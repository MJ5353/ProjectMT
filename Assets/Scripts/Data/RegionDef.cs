using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// 토벌 지역 정의. 로비 토벌 지도에 노드로 찍히고, 골라서 입장하면 전투가 시작된다.
// 깬 지역은 플래그(ClearFlag)로 저장하고, 앞 지역을 깨면 다음 지역이 열린다
[CreateAssetMenu(menuName = "MushTaker/Data/Region", fileName = "Region_")]
public class RegionDef : DataDef
{
    [Serializable]
    public class SpawnGroup
    {
        [field: SerializeField, FormerlySerializedAs("m_Prefab")]
        public MonsterController Prefab { get; private set; } = null;

        [field: SerializeField, FormerlySerializedAs("m_PointPrefix")]
        public string PointPrefix { get; private set; } // 스폰 지점 이름 접두사

        [field: SerializeField, FormerlySerializedAs("m_MaxAlive")]
        public int MaxAlive { get; private set; } // 동시에 살아 있는 최대 수

        [field: SerializeField, FormerlySerializedAs("m_RespawnDelay")]
        public float RespawnDelay { get; private set; } // 빈자리가 생긴 뒤 다시 나오기까지(초)

        // ------------------------------------------------

        public SpawnGroup(MonsterController prefab, string pointPrefix, int maxAlive, float respawnDelay)
        {
            Prefab = prefab;
            PointPrefix = pointPrefix;
            MaxAlive = maxAlive;
            RespawnDelay = respawnDelay;
        }
    }

    [field: SerializeField, FormerlySerializedAs("m_Order")]
    public int Order { get; private set; } = 0;

    [field: SerializeField, Tooltip("이 지역을 깨야 열린다. 비우면 처음부터 열림"), FormerlySerializedAs("m_RequireRegion")]
    public RegionDef RequireRegion { get; private set; } = null;

    [field: SerializeField, Tooltip("토벌 지도 위 위치(0~1)"), FormerlySerializedAs("m_MapPosition")]
    public Vector2 MapPosition { get; private set; } = new Vector2(0.5f, 0.5f);

    [SerializeField, Tooltip("이 지역에서 캘 수 있는 버섯(지도 안내용)")]
    private List<MushroomDef> m_ListMushroom = new();

    [SerializeField]
    private List<SpawnGroup> m_ListSpawnGroup = new();

    public IReadOnlyList<SpawnGroup> ListSpawnGroup => m_ListSpawnGroup;
    public IReadOnlyList<MushroomDef> ListMushroom => m_ListMushroom;
    public string ClearFlag => $"Region_Clear_{ID}";

    // ------------------------------------------------

#if UNITY_EDITOR
    public void EditorSetSpawnGroups(List<SpawnGroup> groups) => m_ListSpawnGroup = groups;

    public void EditorSetup(int order, RegionDef requireRegion, Vector2 mapPosition, List<MushroomDef> listMushroom)
    {
        Order = order;
        RequireRegion = requireRegion;
        MapPosition = mapPosition;
        m_ListMushroom = listMushroom;
    }
#endif
}
