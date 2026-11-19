// ==================================================

// 에디터 도구(빌더·셋업·데이터 편집기)에서만 쓰는 에셋 경로. 프로젝트 기준(Assets/...)·확장자 포함, 폴더는 끝에 / 를 붙인다.
// 런타임 Resources 경로는 CommonPath에 둔다

public static class EditorPath
{
    // 에셋 폴더 (에디터) ---
    public const string AssetsRoot = "Assets/";
    public const string ResourcesRoot = AssetsRoot + "Resources/";
    public const string ScenesDir = AssetsRoot + "Scenes/";
    public const string ModelDir = AssetsRoot + "Art/Voxel/Models/"; // 하위 폴더(Actor·Mushroom·Map 등)까지 이름으로 찾는다
    public const string MonsterModelDir = ModelDir + "Actor/Monster/";
    public const string MushroomModelDir = ModelDir + "Mushroom/";
    public const string FontDir = AssetsRoot + "Art/Fonts/Galmuri/";
    public const string UISpriteDir = AssetsRoot + "Art/UI/Sprites/";

    public const string DataDir = ResourcesRoot + "Data/";
    public const string MonsterDataDir = DataDir + "Monsters/";
    public const string PlayerDataDir = DataDir + "Player/";
    public const string MapDataDir = DataDir + "Maps/";
    public const string MushroomDataDir = DataDir + "Mushrooms/";
    public const string SkillDataDir = DataDir + "Skills/";
    public const string BalanceDataDir = DataDir + "Balance/";

    public const string PrefabDir = ResourcesRoot + "Prefabs/";
    public const string ActorPrefabDir = ResourcesRoot + CommonPath.ActorDir;
    public const string MonsterPrefabDir = ResourcesRoot + CommonPath.MonsterDir;
    public const string AnimatorDir = ActorPrefabDir + "Animators/";
    public const string PickupPrefabDir = ResourcesRoot + CommonPath.PickupDir;
    public const string MapPrefabDir = ResourcesRoot + CommonPath.MapDir;
    public const string PrefabExtension = ".prefab";             // Resources 경로 → 에셋 경로: ResourcesRoot + 경로 + 확장자
    public const string BattleUIPrefabDir = PrefabDir + "UI/Battle/";
    public const string CommonUIPrefabDir = PrefabDir + "UI/Common/";
    public const string MaterialAssetDir = ResourcesRoot + CommonPath.MaterialDir;

    // 에셋 파일 (에디터) ---
    public const string GameDatabaseAsset = ResourcesRoot + CommonPath.GameDatabase + ".asset";
    public const string BattleScene = ScenesDir + SceneNames.Battle + ".unity";
    public const string LobbyScene = ScenesDir + SceneNames.Lobby + ".unity";
    public const string PlayerDef = PlayerDataDir + "Player.asset";
    public const string DefaultMapDef = MapDataDir + "Map_001_Field.asset"; // Battle 씬을 만들 때 넣는 지역
    public const string MonsterBalance = BalanceDataDir + "MonsterBalance.asset";
    public const string ResultScreenPrefab = BattleUIPrefabDir + "ResultScreen.prefab";
    public const string BattleHudPrefab = BattleUIPrefabDir + "BattleHud.prefab";
    public const string DeathPopupPrefab = BattleUIPrefabDir + "DeathPopup.prefab";
    public const string GaugePrefab = CommonUIPrefabDir + "UIGauge.prefab";
    public const string ItemSlotPrefab = CommonUIPrefabDir + "UIItemSlot.prefab";
    public const string ButtonPrefab = CommonUIPrefabDir + "UIButton.prefab";
    public const string Font = FontDir + "Galmuri11.ttf";
    public const string FontAsset = FontDir + "Galmuri11 SDF.asset";
    public const string DataEditorStyle = AssetsRoot + "Scripts/Editor/DataEditorWindow.uss";

    // 데이터 편집 검사 (DataEditorChecks가 거부되는지 보는 잘못된 경로) ---
    public const string CheckAbstractDef = AssetsRoot + "Invalid.asset";
    public const string CheckOutsideDef = AssetsRoot + "../Invalid.asset";
}
