// ==================================================

// 런타임 코드에서 쓰는 에셋·파일 경로. 에디터 도구 전용 경로(Assets/...)는 Editor/EditorPath에 둔다. UI 프리팹은 경로를 두지 않고 PopupDef·UIComponentDef(에디터가 채우는 프리팹 표)로 찾는다.
// Resources.Load 경로는 Resources 폴더 기준·확장자 없음. 폴더는 끝에 / 를 붙인다

public static class CommonPath
{
    // Resources.Load (런타임) ---
    public const string GameDatabase = "GameDatabase";
    public const string MaterialDir = "Materials/";
    public const string HitFlashMaterial = MaterialDir + "HitFlash";
    public const string HitChunkMaterial = MaterialDir + "HitChunk";
    public const string EffectDir = "Prefabs/Effect/";
    public const string TelegraphCircle = EffectDir + "FX_TelegraphCircle";
    public const string TelegraphLine = EffectDir + "FX_TelegraphLine";
    public const string OverlayLoading = "Prefabs/UI/Overlay/OverlayLoading"; // 예외: 씬 밖에서 늘 사는 UI(오버레이)라 프리팹 표(PopupDef·UIComponentDef) 대신 경로로 읽는다
    
    // 콘텐츠 리소스는 폴더 + 이름(Def ID)으로 찾는다. Def는 프리팹을 참조하지 않는다
    public const string ActorDir = "Prefabs/Actor/";           // 플레이어 프리팹(Player)
    public const string MonsterDir = ActorDir + "Monster/";    // 몬스터 프리팹(Monster_NNN_Key)
    public const string PickupDir = ActorDir + "Pickup/";      // 버섯·재화 픽업 프리팹(Pickup_NNN_Key, Pickup_Gold)
    public const string MapDir = "Prefabs/Map/";               // 맵 프리팹(Map_NNN_Key)
    
    // else
    public const string PickupPrefix = "Pickup_";              // 버섯 ID Mushroom_NNN_Key → 픽업 Pickup_NNN_Key
    public const string TMPSettings = "TMP Settings";
    public const string BuiltinCube = "Cube.fbx"; // Resources.GetBuiltinResource
    public const string SaveFile = "save.json"; // 저장 파일 (Application.persistentDataPath 기준) ---
}
