using UnityEngine;

// ==================================================

// 전투 씬. 지역(MapDef)을 골라 BattleFlow(전투 Service Unit·HitStop·Spawn·Loot·Battle)를 만든다. 이펙트·팝업은 공용(FlowManager)
// 맵은 Flow를 만들 때 한 번 만들어 넘긴다. 다시 하기는 같은 Flow를 다시 쓰므로 맵도 그대로다. 씬과 함께 지워진다

[DefaultExecutionOrder(-1100)]
public sealed class BattleSceneController : SceneControllerBase
{
    private const string MapMissingHint = "프리팹 이름이 MapDef ID와 같은지, 누락 전투 에셋 복구를 했는지 확인하세요.";

    [SerializeField] private MapDef m_DefaultMap = null;

    public ScreenBattle ScreenUI => GetScreen<ScreenBattle>();

    // ------------------------------------------------

    protected override FlowBase CreateFlow()
    {
        MapDef mapDef = GetMap();
        return new BattleFlow(mapDef, CreateMap(mapDef));
    }

    // 지역 맵을 지금 씬에 만든다. 없으면 null(스폰 지점 없이 원점에서 시작한다)
    private static MapRoot CreateMap(MapDef mapDef)
    {
        if (mapDef == null)
            return null;

        MapRoot prefab = ResourceManager.Instance.Load<MapRoot>(mapDef.PrefabPath, MapMissingHint);
        if (prefab == null)
            return null;

        return ResourceManager.S_Create(prefab);
    }

    // 로비에서 고른 지역 → 씬 기본 지역 → (개발용) 기본 지역 순
    private MapDef GetMap()
    {
        MapDef mapDef = PlayerDataManager.Instance.Hunt.SelectedMap;
        if (mapDef == null)
            mapDef = m_DefaultMap;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Battle 씬을 바로 실행하면 고른 지역이 없다. 개발용 기본 지역을 쓴다
        if (mapDef == null)
            mapDef = DebugPanel.GetDefaultMap(GameManager.Instance.Database);
#endif

        return mapDef;
    }

#if UNITY_EDITOR
    public void EditorSetup(RectTransform uiRoot, Camera mainCamera, ScreenBattle screen, MapDef defaultMap)
    {
        EditorSetup(uiRoot, mainCamera, screen);
        m_DefaultMap = defaultMap;
    }
#endif
}
