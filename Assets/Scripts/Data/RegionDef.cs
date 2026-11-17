using System;
using System.Collections.Generic;
using UnityEngine;

// 토벌 지역 정의. 로비 토벌 지도에 노드로 찍히고, 골라서 입장하면 전투가 시작된다.
// 깬 지역은 플래그(ClearFlag)로 저장하고, 앞 지역을 깨면 다음 지역이 열린다
[CreateAssetMenu(menuName = "MushTaker/Data/Region", fileName = "Region_")]
public class RegionDef : DataDef
{
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

        // ------------------------------------------------

        public SpawnGroup(MonsterController prefab, string pointPrefix, int maxAlive, float respawnDelay)
        {
            m_Prefab = prefab;
            m_PointPrefix = pointPrefix;
            m_MaxAlive = maxAlive;
            m_RespawnDelay = respawnDelay;
        }
    }

    [SerializeField] private int m_Order = 0;
    [SerializeField, Tooltip("이 지역을 깨야 열린다. 비우면 처음부터 열림")] private RegionDef m_RequireRegion = null;
    [SerializeField, Tooltip("토벌 지도 위 위치(0~1)")] private Vector2 m_MapPosition = new Vector2(0.5f, 0.5f);
    [SerializeField, Tooltip("이 지역에서 캘 수 있는 버섯(지도 안내용)")] private List<MushroomDef> m_ListMushroom = new();

    [SerializeField] private List<SpawnGroup> m_ListSpawnGroup = new();

    public IReadOnlyList<SpawnGroup> ListSpawnGroup => m_ListSpawnGroup;
    public int Order => m_Order;
    public RegionDef RequireRegion => m_RequireRegion;
    public Vector2 MapPosition => m_MapPosition;
    public IReadOnlyList<MushroomDef> ListMushroom => m_ListMushroom;
    public string ClearFlag => $"Region_Clear_{ID}";

    // ------------------------------------------------

#if UNITY_EDITOR
    public void EditorSetSpawnGroups(List<SpawnGroup> groups) => m_ListSpawnGroup = groups;

    public void EditorSetup(int order, RegionDef requireRegion, Vector2 mapPosition, List<MushroomDef> listMushroom)
    {
        m_Order = order;
        m_RequireRegion = requireRegion;
        m_MapPosition = mapPosition;
        m_ListMushroom = listMushroom;
    }
#endif
}
