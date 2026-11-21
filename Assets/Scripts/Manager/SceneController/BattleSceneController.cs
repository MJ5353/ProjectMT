using UnityEngine;

// ==================================================

// 전투 씬. 지역(MapDef)을 골라 전투 Service(Unit·Effect·HitStop·Battle)를 등록한다

[DefaultExecutionOrder(-1100)]
public sealed class BattleSceneController : SceneControllerBase
{
    [SerializeField] private MapDef m_DefaultMap = null;

    public override EGameState State => EGameState.Battle;

    public ScreenBattle ScreenUI => GetScreen<ScreenBattle>();

    // ------------------------------------------------

    public override void RegisterServices(GameManager gameM)
    {
        gameM.RegisterBattleServices(GetMap(gameM));
    }

    // 로비에서 고른 지역 → 씬 기본 지역 → (개발용) 기본 지역 순
    private MapDef GetMap(GameManager gameM)
    {
        MapDef mapDef = PlayerDataManager.Instance.Hunt.SelectedMap;
        if (mapDef == null)
            mapDef = m_DefaultMap;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Battle 씬을 바로 실행하면 고른 지역이 없다. 개발용 기본 지역을 쓴다
        if (mapDef == null)
            mapDef = DebugPanel.GetDefaultMap(gameM.Database);
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
