using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 전투 1단계 시험 환경을 만드는 메뉴.
// 애니메이터(상태 이름 = 클립 이름), 프리팹(채집꾼·버섯 줍기, 몬스터는 MonsterPrefabBuilder), 데이터(플레이어·몬스터), Battle 씬(Map_Field01 + 스폰 지점)을 만든다.
// 프리팹의 참조(Def·CharacterController·Animator·몸 렌더러)는 여기서 채워서, 런타임에는 GetComponent를 쓰지 않는다
// !!) 데이터·프리팹은 이미 있으면 건드리지 않는다(대장이 고친 값 보호). 다시 만들려면 "프리팹 다시 만들기"
public static class BattleSetup
{
    internal const string ModelDir = "Assets/Art/Voxel/Models/";              // 하위 폴더(Actor·Mushroom·Map 등)까지 이름으로 찾는다
    internal const string MonsterModelDir = ModelDir + "Actor/Monster/";
    internal const string PrefabDir = "Assets/Prefabs/Actor/";
    private const string AnimatorDir = PrefabDir + "Animators/";
    internal const string MonsterDataDir = "Assets/Data/Monsters/";
    private const string ScenePath = "Assets/Scenes/Battle.unity";
    private const string MapName = "Map_Field01";
    private const float ModelYaw = 180f;

    private const string PlayerPrefabPath = PrefabDir + "Player.prefab";
    private const string BuggiPrefabPath = PrefabDir + "Monster_Buggi.prefab";
    private const string SongiPickupPath = PrefabDir + "Pickup_Songi.prefab";
    private const string BuggiDefPath = MonsterDataDir + "Monster_Buggi.asset";
    private const string BugomPrefabPath = PrefabDir + "Monster_Bugom.prefab";
    private const string CloudPickupPath = PrefabDir + "Pickup_Cloud.prefab";
    private const string BugomDefPath = MonsterDataDir + "Monster_Bugom.asset";
    private const string MushroomDataDir = "Assets/Data/Mushrooms/";
    private const string PlayerDataDir = "Assets/Data/Player/";
    private const string PlayerDefPath = PlayerDataDir + "Player.asset";
    private const string HitMaterialDir = "Assets/Resources/" + HitMaterials.ResourceDir;

    // ==================================================

    [MenuItem("MushTaker/Battle/전투 씬 만들기 (1단계)")]
    public static void CreateBattleScene()
    {
        if (File.Exists(ScenePath) && EditorUtility.DisplayDialog("전투 씬", $"{ScenePath}가 이미 있습니다. 새로 만들까요?", "새로 만들기", "취소") == false)
            return;

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            return;

        BuildAssets(false);
        BuildScene();
    }

    [MenuItem("MushTaker/Battle/프리팹 다시 만들기")]
    public static void RebuildPrefabs()
    {
        BuildAssets(true);
    }

    // ==================================================

    private static void BuildAssets(bool isForce)
    {
        Directory.CreateDirectory(AnimatorDir);
        Directory.CreateDirectory(MonsterDataDir);
        Directory.CreateDirectory(MushroomDataDir);
        Directory.CreateDirectory(PlayerDataDir);
        Directory.CreateDirectory(HitMaterialDir);

        BuildHitMaterial(HitMaterials.FlashName);
        BuildHitMaterial(HitMaterials.ChunkName);

        BuildMushroomDef("Mushroom_Songi", "송이");
        BuildMushroomDef("Mushroom_Cloud", "구름버섯");

        MushroomPickup songiPickup = BuildPickup(SongiPickupPath, "Mush_Songi", "Mushroom_Songi", isForce);
        BuildBuggiDef(songiPickup);

        MushroomPickup cloudPickup = BuildPickup(CloudPickupPath, "Mush_Cloud", "Mushroom_Cloud", isForce);
        BuildBugomDef(cloudPickup);

        // 몬스터 프리팹은 Data/Monsters의 MonsterDef 전부를 한 번에 굽는다(새 몬스터도 같이)
        MonsterPrefabBuilder.BuildAll(isForce);

        PlayerDef playerDef = BuildPlayerDef();
        BuildPlayer(playerDef, isForce);

        AssetDatabase.SaveAssets();
        DataTools.RefreshDatabase(false);
        CLogger.Log("BattleSetup", "프리팹·데이터 준비 완료");
    }

    private static MushroomPickup BuildPickup(string path, string modelName, string itemID, bool isForce)
    {
        if (File.Exists(path) && isForce == false)
            return AssetDatabase.LoadAssetAtPath<MushroomPickup>(path);

        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(path));
        MushroomPickup pickup = root.AddComponent<MushroomPickup>();
        SetString(pickup, "m_ItemID", itemID);

        GameObject model = InstantiateModel(modelName, root.transform);
        if (model != null)
            model.transform.localScale = Vector3.one * 0.8f;

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved.GetComponent<MushroomPickup>();
    }

    private static MonsterDef BuildBuggiDef(MushroomPickup songiPickup)
    {
        MonsterDef def = AssetDatabase.LoadAssetAtPath<MonsterDef>(BuggiDefPath);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<MonsterDef>();
            AssetDatabase.CreateAsset(def, BuggiDefPath);
            SetString(def, "m_ID", "Mob_Buggi");
            SetString(def, "m_DisplayName", "버끼");

            List<MonsterDef.DropEntry> listDrop = new()
            {
                new MonsterDef.DropEntry(songiPickup, 1, 2, 1f),
            };

            // 박치기: 13프레임 판정, 앞으로 튀어나감
            AttackSpec headbutt = new("Attack", 1, 13, 0.7f, 0.7f, 0.4f, 6f, 0.05f, 5f);
            def.EditorSetup(headbutt, 1.2f, listDrop);
        }

        // 새로 만들 때, 또는 버섯 모델 칸이 비었을 때만(이전 형식 데이터) 채운다. 조각 색도 이때 같이
        if (HasEmptyMushroom(def))
        {
            Renderer songi = LoadMushroom("Mush_Songi");
            def.EditorSetMushrooms(new List<MonsterDef.MushroomSocket>
            {
                new("Socket_Head", songi, 0.8f),
                new("Socket_Back", songi, 0.6f),
            });
            SetColor(def, "m_ChunkColor", new Color(0.95f, 0.9f, 0.8f));
        }

        // 몸 크기가 비었으면(이전 형식 데이터) 원래 쓰던 캡슐 크기로 채운다
        if (def.BodyHeight <= 0f)
            SetBodySize(def, 0.9f, 0.35f);

        EditorUtility.SetDirty(def);
        return def;
    }

    private static bool HasEmptyMushroom(MonsterDef def)
    {
        return def.ListMushroom.Count == 0 || def.ListMushroom.Any(socket => socket.Model == null);
    }

    // 버섯 FBX는 메시 하나라서 렌더러를 바로 꽂는다
    private static Renderer LoadMushroom(string modelName)
    {
        GameObject model = LoadModel(modelName);
        return model != null ? model.GetComponentInChildren<Renderer>() : null;
    }

    // 채집꾼 수치. 나머지는 PlayerDef 기본값
    private static PlayerDef BuildPlayerDef()
    {
        PlayerDef def = AssetDatabase.LoadAssetAtPath<PlayerDef>(PlayerDefPath);
        if (def != null)
            return def;

        def = ScriptableObject.CreateInstance<PlayerDef>();
        AssetDatabase.CreateAsset(def, PlayerDefPath);
        SetString(def, "m_ID", "Player");
        SetString(def, "m_DisplayName", "채집꾼");
        SetInt(def, "m_MaxHp", 5);
        SetFloat(def, "m_HitStunTime", 0.3f);
        SetFloat(def, "m_InvincibleTime", 0.6f);
        SetColor(def, "m_ChunkColor", new Color(0.9f, 0.3f, 0.3f));

        // 내려치기: 9프레임 판정, 살짝 내딛음
        def.EditorSetAttack(new AttackSpec("Attack", 1, 9, 1.1f, 1f, 0.7f, 7f, 0.07f, 2f));
        EditorUtility.SetDirty(def);
        return def;
    }

    // 피격 연출용 흰색 Unlit 머티리얼. 조각 색은 MaterialPropertyBlock으로 바꾸므로 인스턴싱을 켠다
    private static void BuildHitMaterial(string materialName)
    {
        string path = $"{HitMaterialDir}{materialName}.mat";
        if (File.Exists(path))
            return;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            CLogger.Error("BattleSetup", "URP Unlit 셰이더를 찾지 못했습니다.");
            return;
        }

        Material material = new Material(shader) { enableInstancing = true };
        material.color = Color.white;
        AssetDatabase.CreateAsset(material, path);
    }

    private static void BuildMushroomDef(string id, string displayName)
    {
        string path = $"{MushroomDataDir}{id}.asset";
        if (File.Exists(path))
            return;

        MushroomDef def = ScriptableObject.CreateInstance<MushroomDef>();
        AssetDatabase.CreateAsset(def, path);
        SetString(def, "m_ID", id);
        SetString(def, "m_DisplayName", displayName);
    }

    // 버곰: 느리고 단단하다. 넉백이 덜 먹고, 예비 동작이 길고, 한 방이 세다
    private static MonsterDef BuildBugomDef(MushroomPickup cloudPickup)
    {
        MonsterDef def = AssetDatabase.LoadAssetAtPath<MonsterDef>(BugomDefPath);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<MonsterDef>();
            AssetDatabase.CreateAsset(def, BugomDefPath);
            SetString(def, "m_ID", "Mob_Bugom");
            SetString(def, "m_DisplayName", "버곰");
            SetInt(def, "m_MaxHp", 7);
            SetFloat(def, "m_MoveSpeed", 2.2f);
            SetFloat(def, "m_WanderSpeed", 0.9f);
            SetFloat(def, "m_KnockbackScale", 0.35f);
            SetFloat(def, "m_SightRange", 8f);
            SetFloat(def, "m_AttackRange", 2.2f);
            SetFloat(def, "m_WindupTime", 0.7f);
            SetFloat(def, "m_AttackCooldown", 1.8f);
            SetFloat(def, "m_HitStunTime", 0.2f);

            List<MonsterDef.DropEntry> listDrop = new()
            {
                new MonsterDef.DropEntry(cloudPickup, 2, 3, 1f),
            };

            // 일어서서 내려찍기: 19프레임 판정, 앞쪽 넓게
            AttackSpec slam = new("Attack", 2, 19, 1.5f, 1.3f, 0.5f, 10f, 0.09f, 0f);
            def.EditorSetup(slam, 0f, listDrop);
        }

        // 새로 만들 때, 또는 버섯 모델 칸이 비었을 때만(이전 형식 데이터) 채운다. 조각 색도 이때 같이
        if (HasEmptyMushroom(def))
        {
            Renderer cloud = LoadMushroom("Mush_Cloud");
            def.EditorSetMushrooms(new List<MonsterDef.MushroomSocket>
            {
                new("Socket_Back1", cloud, 1.5f),
                new("Socket_Back2", cloud, 1.3f),
                new("Socket_Head", cloud, 0.8f),
            });
            SetColor(def, "m_ChunkColor", new Color(0.55f, 0.4f, 0.3f));
        }

        if (def.BodyHeight <= 0f)
            SetBodySize(def, 1.6f, 0.75f);

        EditorUtility.SetDirty(def);
        return def;
    }

    internal static void SetBodySize(MonsterDef def, float height, float radius)
    {
        SetFloat(def, "m_BodyHeight", height);
        SetFloat(def, "m_BodyRadius", radius);
    }

    private static void BuildPlayer(PlayerDef def, bool isForce)
    {
        if (File.Exists(PlayerPrefabPath) && isForce == false)
            return;

        GameObject root = BuildUnit<PlayerController>("Player", LoadModel("Player"), 1.7f, 0.35f, def);

        PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        Object.DestroyImmediate(root);
    }

    // 몬스터·플레이어 공통 몸: 모델 + CharacterController + 컨트롤러. 컨트롤러의 참조를 여기서 채운다
    internal static GameObject BuildUnit<TController>(string name, GameObject modelAsset, float height, float radius, UnitDef def) where TController : Component
    {
        GameObject root = new GameObject(name);

        Animator animator = null;
        List<Renderer> listRenderer = new();
        GameObject model = InstantiateModel(modelAsset, root.transform);
        if (model != null)
        {
            model.name = "Model";
            // 복셀 FBX는 앞이 -Z라서 Unity 앞(+Z)에 맞게 돌린다
            model.transform.localRotation = Quaternion.Euler(0f, ModelYaw, 0f);
            animator = model.GetComponent<Animator>();
            if (animator == null)
                animator = model.AddComponent<Animator>();

            animator.runtimeAnimatorController = BuildController(modelAsset);
            animator.applyRootMotion = false;
            listRenderer.AddRange(model.GetComponentsInChildren<Renderer>(true).Where(r => r is ParticleSystemRenderer == false));
        }

        CharacterController controller = root.AddComponent<CharacterController>();
        controller.height = height;
        controller.radius = radius;
        controller.center = new Vector3(0f, height * 0.5f, 0f);
        controller.stepOffset = Mathf.Min(0.4f, height * 0.4f);

        TController unit = root.AddComponent<TController>();
        SerializedObject so = new SerializedObject(unit);
        so.FindProperty("m_Def").objectReferenceValue = def;
        so.FindProperty("m_CharacterController").objectReferenceValue = controller;
        so.FindProperty("m_Animator").objectReferenceValue = animator;
        SerializedProperty listBody = so.FindProperty("m_ListBodyRenderer");
        listBody.arraySize = listRenderer.Count;
        for (int i = 0; i < listRenderer.Count; i++)
        {
            listBody.GetArrayElementAtIndex(i).objectReferenceValue = listRenderer[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    // 상태 이름 = 클립 이름, 기본 상태 Idle. 전이 없이 코드에서 CrossFade로 부른다
    private static AnimatorController BuildController(GameObject modelAsset)
    {
        string path = $"{AnimatorDir}{modelAsset.name}.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            stateMachine.RemoveState(child.state);
        }

        IEnumerable<AnimationClip> clips = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(modelAsset))
            .OfType<AnimationClip>()
            .Where(c => c.name.StartsWith("__preview__") == false);

        foreach (AnimationClip clip in clips)
        {
            AnimatorState state = stateMachine.AddState(clip.name);
            state.motion = clip;
            if (clip.name == "Idle")
                stateMachine.defaultState = state;
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }

    // ==================================================

    private static void BuildScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        SceneController sceneController = new GameObject(nameof(SceneController)).AddComponent<SceneController>();

        Transform map = BuildMap();
        Transform playerSpawn = map.E_FindDeep("Spawn_Player");

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath));
        if (playerSpawn != null)
            player.transform.SetPositionAndRotation(playerSpawn.position + Vector3.up * 0.1f, playerSpawn.rotation);

        Camera cam = Object.FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            cam.gameObject.AddComponent<FollowCamera>();
            cam.farClipPlane = 200f;
        }

        GameObject battle = new GameObject("BattleManager");
        BattleManager battleM = battle.AddComponent<BattleManager>();
        sceneController.EditorSetup(battleManager: battleM);
        battle.AddComponent<BattleHud>();

        SerializedObject so = new SerializedObject(battleM);
        SerializedProperty listGroup = so.FindProperty("m_ListGroup");
        listGroup.arraySize = 2;
        SetGroup(listGroup.GetArrayElementAtIndex(0), BuggiPrefabPath, "Spawn_Buggi", 6, 4f);
        SetGroup(listGroup.GetArrayElementAtIndex(1), BugomPrefabPath, "Spawn_Bugom", 3, 8f);
        so.ApplyModifiedPropertiesWithoutUndo();

        // 결과 화면(UI 스레드 담당)
        BattleUISetup.AddResultScreen();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        int buggiCount = FindAllByPrefix(map, "Spawn_Buggi").Count;
        int bugomCount = FindAllByPrefix(map, "Spawn_Bugom").Count;
        CLogger.Log("BattleSetup", $"{ScenePath} 생성. 스폰 지점 버끼 {buggiCount}개, 버곰 {bugomCount}개");
    }

    // 지점은 플레이 때 이름 접두사로 찾는다(맵을 다시 만들어도 끊기지 않게)
    private static void SetGroup(SerializedProperty group, string prefabPath, string pointPrefix, int maxAlive, float respawnDelay)
    {
        group.FindPropertyRelative("m_Prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MonsterController>(prefabPath);
        group.FindPropertyRelative("m_PointPrefix").stringValue = pointPrefix;
        group.FindPropertyRelative("m_MaxAlive").intValue = maxAlive;
        group.FindPropertyRelative("m_RespawnDelay").floatValue = respawnDelay;
    }

    // 맵 FBX가 있으면 그것을, 없으면 평지와 임시 스폰 지점을 만든다
    private static Transform BuildMap()
    {
        GameObject mapModel = LoadModel(MapName);
        if (mapModel != null)
            return ((GameObject)PrefabUtility.InstantiatePrefab(mapModel)).transform;

        CLogger.Warning("BattleSetup", $"{MapName}.fbx가 없어 평지로 만듭니다.");
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(6f, 1f, 6f);

        GameObject playerSpawn = new GameObject("Spawn_Player");
        playerSpawn.transform.SetParent(ground.transform, false);
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI * 2f / 6f;
            GameObject point = new GameObject($"Spawn_Buggi_{i + 1}");
            point.transform.position = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 10f;
            point.transform.SetParent(ground.transform, true);
        }

        return ground.transform;
    }

    private static void AddToBuildSettings()
    {
        List<EditorBuildSettingsScene> listScene = EditorBuildSettings.scenes.ToList();
        if (listScene.Any(s => s.path == ScenePath))
            return;

        listScene.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = listScene.ToArray();
    }

    // ==================================================

    // 모델 폴더 아래 어디에 있든 파일 이름(확장자 제외)이 같은 FBX
    internal static GameObject LoadModel(string modelName)
    {
        string path = AssetDatabase.FindAssets($"{modelName} t:Model", new[] { ModelDir.TrimEnd('/') })
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == modelName && p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase));

        return path != null ? AssetDatabase.LoadAssetAtPath<GameObject>(path) : null;
    }

    private static GameObject InstantiateModel(string modelName, Transform parent)
    {
        GameObject asset = LoadModel(modelName);
        if (asset == null)
        {
            CLogger.Error("BattleSetup", $"모델이 없습니다: {ModelDir} 아래 {modelName}.fbx");
            return null;
        }

        return InstantiateModel(asset, parent);
    }

    private static GameObject InstantiateModel(GameObject asset, Transform parent)
    {
        if (asset == null)
        {
            CLogger.Error("BattleSetup", "모델이 비어 있습니다.");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        instance.transform.SetParent(parent, false);
        return instance;
    }

    private static List<Transform> FindAllByPrefix(Transform parent, string prefix)
    {
        return parent.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name.StartsWith(prefix))
            .OrderBy(t => t.name)
            .ToList();
    }

    // 직렬화 필드 쓰기 (private 필드라 SerializedObject로)

    internal static void SetString(Object target, string propertyName, string value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(propertyName).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(Object target, string propertyName, int value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(propertyName).intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(propertyName).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetColor(Object target, string propertyName, Color value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(propertyName).colorValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
