using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 전투 1단계 시험 환경을 만드는 메뉴.
// 애니메이터(상태 이름 = 클립 이름), 프리팹(채집꾼·버끼·송이 줍기), 버끼 데이터, Battle 씬(Map_Field01 + 스폰 지점)을 만든다.
// !!) 데이터·프리팹은 이미 있으면 건드리지 않는다(대장이 고친 값 보호). 다시 만들려면 "프리팹 다시 만들기"
public static class BattleSetup
{
    private const string ModelDir = "Assets/Art/Voxel/Models/";
    private const string PrefabDir = "Assets/Prefabs/Battle/";
    private const string AnimatorDir = "Assets/Prefabs/Battle/Animators/";
    private const string MobDataDir = "Assets/Data/Mobs/";
    private const string ScenePath = "Assets/Scenes/Battle.unity";
    private const string MapName = "Map_Field01";
    private const float ModelYaw = 180f;

    private const string PlayerPrefabPath = PrefabDir + "Player.prefab";
    private const string BuggiPrefabPath = PrefabDir + "Mob_Buggi.prefab";
    private const string SongiPickupPath = PrefabDir + "Pickup_Songi.prefab";
    private const string BuggiDefPath = MobDataDir + "Mob_Buggi.asset";
    private const string BugomPrefabPath = PrefabDir + "Mob_Bugom.prefab";
    private const string CloudPickupPath = PrefabDir + "Pickup_Cloud.prefab";
    private const string BugomDefPath = MobDataDir + "Mob_Bugom.asset";
    private const string MushroomDataDir = "Assets/Data/Mushrooms/";

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
        Directory.CreateDirectory(MobDataDir);
        Directory.CreateDirectory(MushroomDataDir);

        BuildMushroomDef("Mushroom_Songi", "송이");
        BuildMushroomDef("Mushroom_Cloud", "구름버섯");

        MushroomPickup songiPickup = BuildPickup(SongiPickupPath, "Mush_Songi", "Mushroom_Songi", isForce);
        MobDef buggiDef = BuildBuggiDef(songiPickup);
        BuildBuggi(buggiDef, isForce);

        MushroomPickup cloudPickup = BuildPickup(CloudPickupPath, "Mush_Cloud", "Mushroom_Cloud", isForce);
        MobDef bugomDef = BuildBugomDef(cloudPickup);
        BuildBugom(bugomDef, isForce);

        BuildPlayer(isForce);

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

    private static MobDef BuildBuggiDef(MushroomPickup songiPickup)
    {
        MobDef def = AssetDatabase.LoadAssetAtPath<MobDef>(BuggiDefPath);
        if (def != null)
            return def;

        def = ScriptableObject.CreateInstance<MobDef>();
        AssetDatabase.CreateAsset(def, BuggiDefPath);
        SetString(def, "m_ID", "Mob_Buggi");
        SetString(def, "m_DisplayName", "버끼");

        GameObject songiModel = LoadModel("Mush_Songi");
        List<MobDef.MushroomSocket> listMushroom = new()
        {
            new MobDef.MushroomSocket("Socket_Head", songiModel, 0.8f),
            new MobDef.MushroomSocket("Socket_Back", songiModel, 0.6f),
        };
        List<MobDef.DropEntry> listDrop = new()
        {
            new MobDef.DropEntry(songiPickup, 1, 2, 1f),
        };

        // 박치기: 13프레임 판정, 앞으로 튀어나감
        AttackSpec headbutt = new("Attack", 1, 13, 0.7f, 0.7f, 0.4f, 6f, 0.05f, 5f);
        def.EditorSetup(headbutt, 1.2f, listMushroom, listDrop);
        EditorUtility.SetDirty(def);
        return def;
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
    private static MobDef BuildBugomDef(MushroomPickup cloudPickup)
    {
        MobDef def = AssetDatabase.LoadAssetAtPath<MobDef>(BugomDefPath);
        if (def != null)
            return def;

        def = ScriptableObject.CreateInstance<MobDef>();
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

        GameObject cloudModel = LoadModel("Mush_Cloud");
        List<MobDef.MushroomSocket> listMushroom = new()
        {
            new MobDef.MushroomSocket("Socket_Back1", cloudModel, 1.5f),
            new MobDef.MushroomSocket("Socket_Back2", cloudModel, 1.3f),
            new MobDef.MushroomSocket("Socket_Head", cloudModel, 0.8f),
        };
        List<MobDef.DropEntry> listDrop = new()
        {
            new MobDef.DropEntry(cloudPickup, 2, 3, 1f),
        };

        // 일어서서 내려찍기: 19프레임 판정, 앞쪽 넓게
        AttackSpec slam = new("Attack", 2, 19, 1.5f, 1.3f, 0.5f, 10f, 0.09f, 0f);
        def.EditorSetup(slam, 0f, listMushroom, listDrop);
        EditorUtility.SetDirty(def);
        return def;
    }

    private static void BuildBugom(MobDef def, bool isForce)
    {
        if (File.Exists(BugomPrefabPath) && isForce == false)
            return;

        GameObject root = BuildUnit("Mob_Bugom", "Bugom", 1.6f, 0.75f, ETeam.Mob, 0f, def.MaxHp, new Color(0.55f, 0.4f, 0.3f));
        MobController mob = root.AddComponent<MobController>();
        SetObject(mob, "m_Def", def);
        SetObject(mob, "m_VisualRoot", root.transform.Find("Model"));

        PrefabUtility.SaveAsPrefabAsset(root, BugomPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void BuildBuggi(MobDef def, bool isForce)
    {
        if (File.Exists(BuggiPrefabPath) && isForce == false)
            return;

        GameObject root = BuildUnit("Mob_Buggi", "Buggi", 0.9f, 0.35f, ETeam.Mob, 0f, def.MaxHp, new Color(0.95f, 0.9f, 0.8f));
        MobController mob = root.AddComponent<MobController>();
        SetObject(mob, "m_Def", def);
        SetObject(mob, "m_VisualRoot", root.transform.Find("Model"));

        PrefabUtility.SaveAsPrefabAsset(root, BuggiPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void BuildPlayer(bool isForce)
    {
        if (File.Exists(PlayerPrefabPath) && isForce == false)
            return;

        GameObject root = BuildUnit("Player", "Player", 1.7f, 0.35f, ETeam.Player, 0.6f, 5, new Color(0.9f, 0.3f, 0.3f));
        root.AddComponent<PlayerController>();

        PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        Object.DestroyImmediate(root);
    }

    // 몹·플레이어 공통 몸: 모델 + 이동 + 애니 + 피격 부품 조합
    private static GameObject BuildUnit(string name, string modelName, float height, float radius, ETeam team, float invincibleTime, int maxHp, Color chunkColor)
    {
        GameObject root = new GameObject(name);

        GameObject model = InstantiateModel(modelName, root.transform);
        if (model != null)
        {
            model.name = "Model";
            // 복셀 FBX는 앞이 -Z라서 Unity 앞(+Z)에 맞게 돌린다
            model.transform.localRotation = Quaternion.Euler(0f, ModelYaw, 0f);
            Animator animator = model.GetComponent<Animator>();
            if (animator == null)
                animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = BuildController(modelName);
            animator.applyRootMotion = false;
        }

        CharacterController controller = root.AddComponent<CharacterController>();
        controller.height = height;
        controller.radius = radius;
        controller.center = new Vector3(0f, height * 0.5f, 0f);
        controller.stepOffset = Mathf.Min(0.4f, height * 0.4f);

        root.AddComponent<MoveFeature>();
        AnimFeature anim = root.AddComponent<AnimFeature>();
        if (model != null)
            SetObject(anim, "m_Animator", model.GetComponent<Animator>());

        HealthFeature health = root.AddComponent<HealthFeature>();
        SetInt(health, "m_MaxHp", maxHp);

        HitFeature hitFeature = root.AddComponent<HitFeature>();
        SetEnum(hitFeature, "m_Team", (int)team);
        SetFloat(hitFeature, "m_InvincibleTime", invincibleTime);

        root.AddComponent<KnockbackFeature>();
        root.AddComponent<FlashFeature>();
        root.AddComponent<HitStopFeature>();
        ChunkBurstFeature chunk = root.AddComponent<ChunkBurstFeature>();
        SetColor(chunk, "m_Color", chunkColor);

        MeleeAttackFeature melee = root.AddComponent<MeleeAttackFeature>();
        SetEnum(melee, "m_Team", (int)team);
        return root;
    }

    // 상태 이름 = 클립 이름, 기본 상태 Idle. 전이 없이 코드에서 CrossFade로 부른다
    private static AnimatorController BuildController(string modelName)
    {
        string path = $"{AnimatorDir}{modelName}.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            stateMachine.RemoveState(child.state);
        }

        IEnumerable<AnimationClip> clips = AssetDatabase.LoadAllAssetsAtPath($"{ModelDir}{modelName}.fbx")
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

        Transform map = BuildMap();
        Transform playerSpawn = FindDeep(map, "Spawn_Player");

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath));
        if (playerSpawn != null)
            player.transform.SetPositionAndRotation(playerSpawn.position + Vector3.up * 0.1f, playerSpawn.rotation);

        Camera cam = Object.FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            FollowCamera follow = cam.gameObject.AddComponent<FollowCamera>();
            SetObject(follow, "m_Target", player.transform);
            cam.farClipPlane = 200f;
        }

        GameObject battle = new GameObject("BattleManager");
        BattleManager battleM = battle.AddComponent<BattleManager>();
        battle.AddComponent<BattleHud>();

        SerializedObject so = new SerializedObject(battleM);
        so.FindProperty("m_Player").objectReferenceValue = player.GetComponent<PlayerController>();
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
        group.FindPropertyRelative("m_Prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MobController>(prefabPath);
        group.FindPropertyRelative("m_PointPrefix").stringValue = pointPrefix;
        group.FindPropertyRelative("m_ListPoint").arraySize = 0;
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

    private static GameObject LoadModel(string modelName)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}{modelName}.fbx");
    }

    private static GameObject InstantiateModel(string modelName, Transform parent)
    {
        GameObject asset = LoadModel(modelName);
        if (asset == null)
        {
            CLogger.Error("BattleSetup", $"모델이 없습니다: {ModelDir}{modelName}.fbx");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        instance.transform.SetParent(parent, false);
        return instance;
    }

    private static Transform FindDeep(Transform parent, string targetName)
    {
        if (parent.name == targetName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindDeep(child, targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static List<Transform> FindAllByPrefix(Transform parent, string prefix)
    {
        return parent.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name.StartsWith(prefix))
            .OrderBy(t => t.name)
            .ToList();
    }

    // 직렬화 필드 쓰기 (private 필드라 SerializedObject로)

    private static void SetString(Object target, string propertyName, string value)
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

    private static void SetEnum(Object target, string propertyName, int index)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(propertyName).enumValueIndex = index;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetColor(Object target, string propertyName, Color value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(propertyName).colorValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(Object target, string propertyName, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(propertyName).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
