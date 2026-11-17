using System.Collections.Generic;
using UnityEngine;

// 토벌 지역 정의. 로비 토벌 지도에 노드로 찍히고, 골라서 입장하면 전투가 시작된다.
// 깬 지역은 플래그(ClearFlag)로 저장하고, 앞 지역을 깨면 다음 지역이 열린다
[CreateAssetMenu(menuName = "MushTaker/Data/Region", fileName = "Region_")]
public class RegionDef : DataDef
{
    [SerializeField] private int m_Order = 0;
    [SerializeField, Tooltip("이 지역을 깨야 열린다. 비우면 처음부터 열림")] private RegionDef m_RequireRegion = null;
    [SerializeField, Tooltip("토벌 지도 위 위치(0~1)")] private Vector2 m_MapPosition = new Vector2(0.5f, 0.5f);
    [SerializeField, Tooltip("이 지역에서 캘 수 있는 버섯(지도 안내용)")] private List<MushroomDef> m_ListMushroom = new();

    public int Order => m_Order;
    public RegionDef RequireRegion => m_RequireRegion;
    public Vector2 MapPosition => m_MapPosition;
    public IReadOnlyList<MushroomDef> ListMushroom => m_ListMushroom;
    public string ClearFlag => $"Region_Clear_{ID}";

#if UNITY_EDITOR
    public void EditorSetup(int order, RegionDef requireRegion, Vector2 mapPosition, List<MushroomDef> listMushroom)
    {
        m_Order = order;
        m_RequireRegion = requireRegion;
        m_MapPosition = mapPosition;
        m_ListMushroom = listMushroom;
    }
#endif
}
