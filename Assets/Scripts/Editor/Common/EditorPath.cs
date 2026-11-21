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
    public const string UISpriteDir = AssetsRoot + "Art/UI/Sprites/"; // 하위 폴더 Common·HUD·Lobby·Icon/*. 생성기는 UIBuilder.LoadSprite(이름)로 찾는다

    public const string DataDir = ResourcesRoot + "Data/";
    public const string MonsterDataDir = DataDir + "Monsters/";
    public const string PlayerDataDir = DataDir + "Player/";
    public const string MapDataDir = DataDir + "Maps/";
    public const string MushroomDataDir = DataDir + "Mushrooms/";
    public const string SkillDataDir = DataDir + "Skills/";
    public const string BalanceDataDir = DataDir + "Balance/";
    public const string UIDataDir = DataDir + "UI/";
    public const string MerchantDataDir = DataDir + "Merchants/";

    public const string PrefabDir = ResourcesRoot + "Prefabs/";
    public const string ActorPrefabDir = ResourcesRoot + CommonPath.ActorDir;
    public const string MonsterPrefabDir = ResourcesRoot + CommonPath.MonsterDir;
    public const string AnimatorDir = ActorPrefabDir + "Animators/";
    public const string PickupPrefabDir = ResourcesRoot + CommonPath.PickupDir;
    public const string MapPrefabDir = ResourcesRoot + CommonPath.MapDir;
    public const string PrefabExtension = ".prefab";             // Resources 경로 → 에셋 경로: ResourcesRoot + 경로 + 확장자
    public const string UIPrefabDir = PrefabDir + "UI/";        // UI 프리팹 저장 위치. 런타임은 경로를 쓰지 않고 PopupDef·UIComponentDef가 프리팹을 직접 참조한다(등록 때 하위 폴더까지 enum 이름으로 찾는다)
    public const string ComponentUIPrefabDir = UIPrefabDir + "Component/"; // 스크립트 폴더(UI/Component·Popup·Screen)와 같게 둔다
    public const string PopupUIPrefabDir = UIPrefabDir + "Popup/";
    public const string MarketPopupUIPrefabDir = PopupUIPrefabDir + "Market/";
    public const string ScreenUIPrefabDir = UIPrefabDir + "Screen/";
    public const string LobbyScreenUIPrefabDir = ScreenUIPrefabDir + "Lobby/";
    public const string BattleScreenUIPrefabDir = ScreenUIPrefabDir + "Battle/";
    public const string MaterialAssetDir = ResourcesRoot + CommonPath.MaterialDir;

    // 에셋 파일 (에디터) ---
    public const string GameDatabaseAsset = ResourcesRoot + CommonPath.GameDatabase + ".asset";
    public const string BattleScene = ScenesDir + SceneNames.Battle + ".unity";
    public const string LobbyScene = ScenesDir + SceneNames.Lobby + ".unity";
    public const string PlayerDef = PlayerDataDir + "Player.asset";
    public const string DefaultMapDef = MapDataDir + "Map_001_Field.asset"; // Battle 씬을 만들 때 넣는 지역
    public const string MonsterBalance = BalanceDataDir + "MonsterBalance.asset";
    public const string PopupDefAsset = UIDataDir + PopupDef.PopupID + ".asset";
    public const string UIComponentDefAsset = UIDataDir + UIComponentDef.UIComponentID + ".asset";
    public const string StringDefAsset = UIDataDir + StringDef.StringID + ".asset";
    public const string RarityDefAsset = UIDataDir + RarityDef.RarityID + ".asset";
    public const string PopupResultPrefab = PopupUIPrefabDir + nameof(EPopupType.PopupResult) + PrefabExtension;
    public const string ScreenLobbyPrefab = LobbyScreenUIPrefabDir + nameof(ScreenLobby) + PrefabExtension;
    public const string ScreenBattlePrefab = BattleScreenUIPrefabDir + nameof(ScreenBattle) + PrefabExtension;
    public const string PopupDeathPrefab = PopupUIPrefabDir + nameof(EPopupType.PopupDeath) + PrefabExtension;
    public const string PopupMarketPrefab = MarketPopupUIPrefabDir + nameof(EPopupType.PopupMarket) + PrefabExtension;
    public const string PopupMarketCounterPrefab = MarketPopupUIPrefabDir + nameof(EPopupType.PopupMarketCounter) + PrefabExtension;

    // UI 요소 프리팹. 파일 이름은 EUIComponentType 이름과 같아야 UIComponentDef에 등록된다
    public const string MarketMerchantRowPrefab = MarketPopupUIPrefabDir + nameof(EUIComponentType.PopupMarket_MerchantRow) + PrefabExtension;
    public const string BattleLootRowPrefab = BattleScreenUIPrefabDir + nameof(EUIComponentType.ScreenBattle_LootRow) + PrefabExtension;
    public const string BattleSkillIconPrefab = BattleScreenUIPrefabDir + nameof(EUIComponentType.ScreenBattle_SkillIcon) + PrefabExtension;
    public const string BattleUnitHpBarPrefab = BattleScreenUIPrefabDir + nameof(EUIComponentType.ScreenBattle_UnitHpBar) + PrefabExtension; // UIGauge
    public const string GaugePrefab = ComponentUIPrefabDir + "UIGauge.prefab";
    public const string CommonSlotPrefab = ComponentUIPrefabDir + nameof(EUIComponentType.UICommonSlot) + PrefabExtension;
    public const string ButtonPrefab = ComponentUIPrefabDir + "UIButton.prefab";
    public const string Font = FontDir + "Galmuri11.ttf";
    public const string FontAsset = FontDir + "Galmuri11 SDF.asset";
    public const string FontOutlineAsset = FontDir + "Galmuri11_OutLine SDF.asset"; // 배경 없이 화면에 바로 뜨는 라벨용
    public const string DataEditorStyle = AssetsRoot + "Scripts/Editor/Data/DataEditorWindow.uss";

    // 데이터 편집 검사 (DataEditorChecks가 거부되는지 보는 잘못된 경로) ---
    public const string CheckAbstractDef = AssetsRoot + "Invalid.asset";
    public const string CheckOutsideDef = AssetsRoot + "../Invalid.asset";
}
