using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 전투 1단계 시험 환경을 만드는 메뉴.
// 기존 Def로 누락 프리팹을 복구하고 Battle 씬을 만든다. 맵은 씬에 두지 않고 BattleService가 MapDef.Prefab으로 만든다.
// 프리팹의 참조(Def·CharacterController·Animator·몸 렌더러)는 여기서 채워서, 런타임에는 GetComponent를 쓰지 않는다
// !!) 초기 설정은 누락된 에셋만 복구한다. 수치와 기존 프리팹은 데이터 편집기에서 관리한다.
public static class BattleSetup
{
    internal const string ModelDir = "Assets/Art/Voxel/Models/";              // 하위 폴더(Actor·Mushroom·Map 등)까지 이름으로 찾는다
    internal const string MonsterModelDir = ModelDir + "Actor/Monster/";
    internal const string PrefabDir = "Assets/Resources/Prefabs/Actor/";
    private const string AnimatorDir = PrefabDir + "Animators/";
    internal const string MonsterDataDir = "Assets/Resources/Data/Monsters/";
    private const string ScenePath = "Assets/Scenes/Battle.unity";
    private const string MapName = "Map_001_Field";
    private const float ModelYaw = 180f;

    private const string PlayerPrefabPath = PrefabDir + "Player.prefab";
    private const string PlayerDataDir = "Assets/Resources/Data/Player/";
    private const string PlayerDefPath = PlayerDataDir + "Player.asset";
    internal const string MapDataDir = "Assets/Resources/Data/Maps/";
    private const string HitMaterialDir = "Assets/Resources/" + HitMaterials.ResourceDir;

    // ------------------------------------------------

    [MenuItem("MushTaker/초기 설정/전투 씬 재생성", false, 100)]
    public static void CreateBattleScene()
    {
        if (File.Exists(ScenePath) && EditorUtility.DisplayDialog("전투 씬", $"{ScenePath}가 이미 있습니다. 새로 만들까요?", "새로 만들기", "취소") == false)
            return;

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            return;

        if (PrepareAssets() == false)
            return;

        BuildScene();
    }

    [MenuItem("MushTaker/초기 설정/누락 전투 에셋 복구", false, 20)]
    public static void RepairAssets()
    {
        PrepareAssets();
    }

    // ==================================================

    private static bool PrepareAssets()
    {
        PlayerDef playerDef = AssetDatabase.LoadAssetAtPath<PlayerDef>(PlayerDefPath);
        if (playerDef == null)
        {
            CLogger.Error("BattleSetup", "PlayerDef가 없습니다. 데이터 편집기에서 플레이어 데이터를 준비하세요.");
            return false;
        }

        Directory.CreateDirectory(AnimatorDir);
        Directory.CreateDirectory(HitMaterialDir);
        BuildHitMaterial(HitMaterials.FlashName);
        BuildHitMaterial(HitMaterials.ChunkName);
        MushroomPickupBuilder.BuildAll(false);
        MonsterPrefabBuilder.BuildAll(false);
        MapPrefabBuilder.BuildAll(false);
        BuildPlayer(playerDef, false);
        if (playerDef.Prefab == null)
        {
            playerDef.EditorSetPrefab(AssetDatabase.LoadAssetAtPath<PlayerController>(PlayerPrefabPath));
            EditorUtility.SetDirty(playerDef);
        }

        AssetDatabase.SaveAssets();
        DataTools.RefreshDatabase(false);
        CLogger.Log("BattleSetup", "누락 전투 에셋 복구 완료");
        return true;
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

    internal static void SetBodySize(MonsterDef def, float height, float radius)
    {
        SetFloat(def, nameof(MonsterDef.BodyHeight), height);
        SetFloat(def, nameof(MonsterDef.BodyRadius), radius);
    }

    private static void BuildPlayer(PlayerDef def, bool isForce)
    {
        if (File.Exists(PlayerPrefabPath) && isForce == false)
            return;

        GameObject root = BuildUnit<PlayerController>("Player", LoadModel("Player"), 1.7f, 0.35f, def);

        PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        Object.DestroyImmediate(root);
    }

    // 몬스터·플레이어 공통 몸: 모델 + CharacterController + UnitProperty + 컨트롤러. 참조를 여기서 채운다
    internal static GameObject BuildUnit<TController>(string name, GameObject modelAsset, float height, float radius, UnitDef def) where TController : Component
    {
        GameObject root = new GameObject(name);
        int actorLayer = LayerMask.NameToLayer(GameLayers.Actor);
        if (actorLayer < 0)
            CLogger.Error("BattleSetup", $"{GameLayers.Actor} 레이어가 없습니다. Tags and Layers에 추가하세요.");
        else
            root.layer = actorLayer;

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

        // 프리팹 참조는 UnitProperty에 모은다. 부위 뼈 Hitbox_*와 버섯 소켓 Socket_*(가장 가까운 부위로)
        List<UnitProperty.BonePart> listBonePart = model != null ? BodyPartMap.CollectBoneParts(model.transform, def) : new();
        UnitProperty property = root.AddComponent<UnitProperty>();
        property.EditorSetup(controller, animator, listRenderer, listBonePart);

        TController unit = root.AddComponent<TController>();
        SerializedObject so = new SerializedObject(unit);
        so.FindProperty("m_Def").objectReferenceValue = def;
        so.FindProperty("m_Property").objectReferenceValue = property;
        so.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    // 상태 이름 = 클립 이름, 기본 상태 Idle. 전이 없이 코드에서 CrossFade로 부른다
    // 애니메이터 이름: AC_ + 모델 키(Mon_001_Buggi → AC_001_Buggi, Player → AC_Player)
    private static string GetControllerName(string modelName)
    {
        return "AC_" + MonsterPrefabBuilder.GetKey(modelName);
    }

    internal static AnimatorController BuildController(GameObject modelAsset)
    {
        Directory.CreateDirectory(AnimatorDir);
        string path = $"{AnimatorDir}{GetControllerName(modelAsset.name)}.controller";
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
        AssetDatabase.SaveAssetIfDirty(controller);
        return controller;
    }

    // ==================================================

    private static void BuildScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        SceneController sceneController = new GameObject(nameof(SceneController)).AddComponent<SceneController>();

        // 맵·플레이어는 씬에 두지 않고 플레이 때 BattleService가 만든다
        Camera cam = Object.FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            cam.gameObject.AddComponent<FollowCamera>();
            cam.farClipPlane = 200f;
        }

        new GameObject(nameof(BattleHud)).AddComponent<BattleHud>();
        RectTransform popupRoot = UIBuilder.CreateCanvas("BattlePopupCanvas", 110);
        MapDef mapDef = AssetDatabase.LoadAssetAtPath<MapDef>(MapDataDir + MapName + ".asset");
        sceneController.EditorSetup(popupRoot, mapDef);

        // 결과 화면(UI 스레드 담당)
        BattleUISetup.AddResultScreen();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        CLogger.Log("BattleSetup", $"{ScenePath} 생성");
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

    internal static List<Transform> FindAllByPrefix(Transform parent, string prefix)
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
        FindSerializedField(so, propertyName).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(Object target, string propertyName, int value)
    {
        SerializedObject so = new SerializedObject(target);
        FindSerializedField(so, propertyName).intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        SerializedObject so = new SerializedObject(target);
        FindSerializedField(so, propertyName).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetColor(Object target, string propertyName, Color value)
    {
        SerializedObject so = new SerializedObject(target);
        FindSerializedField(so, propertyName).colorValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static SerializedProperty FindSerializedField(SerializedObject target, string name)
    {
        return target.FindProperty(name) ?? target.FindProperty($"<{name}>k__BackingField");
    }
}
