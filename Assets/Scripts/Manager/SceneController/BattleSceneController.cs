using UnityEngine;

// ==================================================

// 전투 씬. 지역(MapDef)을 골라 BattleFlow(전투 Service Unit·HitStop·Battle)를 만든다. 이펙트·팝업은 공용(FlowManager)

[DefaultExecutionOrder(-1100)]
public sealed class BattleSceneController : SceneControllerBase
{
    [SerializeField] private MapDef m_DefaultMap = null;

    public ScreenBattle ScreenUI => GetScreen<ScreenBattle>();

    // ------------------------------------------------

    protected override FlowBase CreateFlow() => new BattleFlow(GetMap());

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
