// ==================================================

// 코드에서 쓰는 모든 에셋·파일 경로. 경로를 바꿀 때는 여기만 고친다.
// Resources.Load 경로는 Resources 폴더 기준·확장자 없음, 에셋 경로는 프로젝트 기준(Assets/...)·확장자 포함. 폴더는 끝에 / 를 붙인다

public static class CommonPath
{
    // Resources.Load (런타임) ---
    public const string GameDatabase = "GameDatabase";
    public const string MaterialDir = "Materials/";
    public const string HitFlashMaterial = MaterialDir + "HitFlash";
    public const string HitChunkMaterial = MaterialDir + "HitChunk";
    public const string EffectDir = "Prefabs/Effect/";
    public const string PartBreakEffect = EffectDir + "FX_PartBreak";
    public const string DeathEffect = EffectDir + "FX_Death";
    public const string PickupEffect = EffectDir + "FX_Pickup";
    public const string TMPSettings = "TMP Settings";
    public const string BuiltinCube = "Cube.fbx"; // Resources.GetBuiltinResource

    // 저장 파일 (Application.persistentDataPath 기준) ---
    public const string SaveFile = "save.json";

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
    public const string ActorPrefabDir = PrefabDir + "Actor/";
    public const string AnimatorDir = ActorPrefabDir + "Animators/";
    public const string PickupPrefabDir = ActorPrefabDir + "Pickup/";
    public const string MapPrefabDir = PrefabDir + "Map/";
    public const string BattleUIPrefabDir = PrefabDir + "UI/Battle/";
    public const string CommonUIPrefabDir = PrefabDir + "UI/Common/";
    public const string MaterialAssetDir = ResourcesRoot + MaterialDir;

    // 에셋 파일 (에디터) ---
    public const string GameDatabaseAsset = ResourcesRoot + GameDatabase + ".asset";
    public const string BattleScene = ScenesDir + SceneNames.Battle + ".unity";
    public const string LobbyScene = ScenesDir + SceneNames.Lobby + ".unity";
    public const string PlayerPrefab = ActorPrefabDir + "Player.prefab";
    public const string PlayerDef = PlayerDataDir + "Player.asset";
    public const string DefaultMapDef = MapDataDir + "Map_001_Field.asset"; // Battle 씬을 만들 때 넣는 지역
    public const string MonsterBalance = BalanceDataDir + "MonsterBalance.asset";
    public const string ResultScreenPrefab = BattleUIPrefabDir + "ResultScreen.prefab";
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
