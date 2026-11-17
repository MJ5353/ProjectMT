using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 몬스터 프리팹 일괄 제작. Data/Monsters의 MonsterDef 한 장 = 프리팹 한 개(Prefabs/Actor/{에셋 이름}.prefab).
// 이름: 모델 Xxx.fbx → Def·프리팹 Monster_Xxx, ID도 Monster_Xxx(DataDef 기본 규칙, 에셋 이름 = ID)
// 새 몬스터: FBX를 Art/Voxel/Models/Actor/Monster에 넣고 → 프로젝트 창에서 FBX 선택 → 우클릭 MushTaker/선택한 모델로 몬스터 만들기
// 수치·버섯을 고친 뒤: MushTaker/Battle/몬스터 프리팹 일괄 제작
// !!) 프리팹은 덮어쓴다(GUID 유지라 씬 참조는 안 끊긴다). 프리팹에 손으로 단 것은 사라지니 값은 Def에 둔다
public static class MonsterPrefabBuilder
{
    private const string DefPrefix = "Monster_";
    private const string SocketPrefix = "Socket_";
    private const float SizeStep = 0.05f;      // 자동 크기 반올림 단위
    private const float RadiusRate = 0.4f;     // 모델 가로·세로 중 짧은 쪽에 곱해 반지름으로
    private const float MinRadius = 0.2f;

    // ==================================================

    [MenuItem("MushTaker/Battle/몬스터 프리팹 일괄 제작")]
    private static void BuildAllFromMenu()
    {
        BuildAll(true);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Assets/MushTaker/선택한 모델로 몬스터 만들기")]
    private static void CreateFromSelection()
    {
        Directory.CreateDirectory(BattleSetup.MonsterDataDir);
        Directory.CreateDirectory(BattleSetup.PrefabDir);

        List<MonsterDef> listDef = new();
        foreach (GameObject model in GetSelectedModels())
        {
            MonsterDef def = LoadOrCreateDef(model);
            if (BuildPrefab(def))
                listDef.Add(def);
        }

        AssetDatabase.SaveAssets();
        DataTools.RefreshDatabase(false);
        Selection.objects = listDef.ToArray();
        CLogger.Log("MonsterPrefabBuilder", $"몬스터 {listDef.Count}종 생성. 수치·버섯은 Def에서 고친 뒤 일괄 제작을 다시 누르세요");
    }

    [MenuItem("Assets/MushTaker/선택한 모델로 몬스터 만들기", true)]
    private static bool CanCreateFromSelection()
    {
        return GetSelectedModels().Count > 0;
    }

    // ==================================================

    // Data/Monsters의 MonsterDef 전부. isForce가 false면 이미 있는 프리팹은 건너뛴다
    public static void BuildAll(bool isForce)
    {
        Directory.CreateDirectory(BattleSetup.PrefabDir);

        List<MonsterDef> listDef = AssetDatabase.FindAssets("t:MonsterDef", new[] { BattleSetup.MonsterDataDir.TrimEnd('/') })
            .Select(guid => AssetDatabase.LoadAssetAtPath<MonsterDef>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(d => d != null)
            .OrderBy(d => d.name)
            .ToList();

        int builtCount = 0;
        List<string> listFailed = new();
        foreach (MonsterDef def in listDef)
        {
            if (File.Exists(GetPrefabPath(def)) && isForce == false)
                continue;

            if (BuildPrefab(def))
                builtCount++;
            else
                listFailed.Add(def.name);
        }

        if (listFailed.Count > 0)
            CLogger.Warning("MonsterPrefabBuilder", $"몬스터 프리팹 {builtCount}개 제작, 실패 {listFailed.Count}개: {string.Join(", ", listFailed)}");
        else
            CLogger.Log("MonsterPrefabBuilder", $"몬스터 프리팹 {builtCount}개 제작 (Def {listDef.Count}장)");
    }

    public static string GetPrefabPath(MonsterDef def)
    {
        return $"{BattleSetup.PrefabDir}{def.name}.prefab";
    }

    // ==================================================

    private static bool BuildPrefab(MonsterDef def)
    {
        string modelName = GetModelName(def);
        GameObject model = BattleSetup.LoadModel(modelName);
        if (model == null)
        {
            CLogger.Error("MonsterPrefabBuilder", $"모델이 없어 건너뜀: {def.name} → {modelName}.fbx (Def의 Model Name 확인)", def);
            return false;
        }

        FillBodySize(def, model);
        CheckSockets(def, model);

        GameObject root = BattleSetup.BuildUnit<MonsterController>(def.name, model, def.BodyHeight, def.BodyRadius, def);
        PrefabUtility.SaveAsPrefabAsset(root, GetPrefabPath(def));
        Object.DestroyImmediate(root);
        return true;
    }

    // 비어 있으면 에셋 이름에서 접두사를 뗀 이름(Monster_Buggi → Buggi)
    private static string GetModelName(MonsterDef def)
    {
        if (def.ModelName.E_IsNullOrEmpty() == false)
            return def.ModelName;

        return def.name.StartsWith(DefPrefix) ? def.name.Substring(DefPrefix.Length) : def.name;
    }

    // 캡슐 크기가 0이면 모델 크기로 계산해 Def에 써 둔다(이후엔 Def 값을 손으로 고침)
    private static void FillBodySize(MonsterDef def, GameObject model)
    {
        if (def.BodyHeight > 0f && def.BodyRadius > 0f)
            return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Object.DestroyImmediate(instance);
            CLogger.Warning("MonsterPrefabBuilder", $"렌더러가 없어 크기를 못 잼: {def.name}", def);
            return;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        Object.DestroyImmediate(instance);

        float height = def.BodyHeight > 0f ? def.BodyHeight : RoundSize(bounds.max.y);
        float radius = def.BodyRadius > 0f ? def.BodyRadius : Mathf.Max(MinRadius, RoundSize(Mathf.Min(bounds.size.x, bounds.size.z) * RadiusRate));
        BattleSetup.SetBodySize(def, height, radius);
        EditorUtility.SetDirty(def);
    }

    private static float RoundSize(float value)
    {
        return Mathf.Round(value / SizeStep) * SizeStep;
    }

    // Def의 버섯 소켓 이름이 모델 뼈에 없으면 알린다(오타·모델 교체 확인용)
    private static void CheckSockets(MonsterDef def, GameObject model)
    {
        HashSet<string> setBone = new(model.GetComponentsInChildren<Transform>(true).Select(t => t.name));
        foreach (MonsterDef.MushroomSocket socket in def.ListMushroom)
        {
            if (setBone.Contains(socket.SocketName) == false)
                CLogger.Warning("MonsterPrefabBuilder", $"{def.name}: 모델 {model.name}에 소켓 뼈 없음: {socket.SocketName}", def);
        }
    }

    // ==================================================

    // 같은 이름의 Def가 있으면 그대로 쓰고, 없으면 기본값 + 모델의 Socket_* 뼈를 버섯 칸으로 채워 만든다
    private static MonsterDef LoadOrCreateDef(GameObject model)
    {
        string path = $"{BattleSetup.MonsterDataDir}{DefPrefix}{model.name}.asset";
        MonsterDef def = AssetDatabase.LoadAssetAtPath<MonsterDef>(path);
        if (def != null)
            return def;

        def = ScriptableObject.CreateInstance<MonsterDef>();
        AssetDatabase.CreateAsset(def, path);
        BattleSetup.SetString(def, "m_ID", def.name);
        BattleSetup.SetString(def, "m_DisplayName", model.name);
        BattleSetup.SetString(def, "m_ModelName", model.name);

        // 버섯 모델은 비워 둔다. 빈 칸은 붙이지 않으니 Def에서 골라 넣으면 된다
        List<MonsterDef.MushroomSocket> listSocket = model.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name.StartsWith(SocketPrefix))
            .Select(t => new MonsterDef.MushroomSocket(t.name, null, 1f))
            .ToList();
        def.EditorSetMushrooms(listSocket);

        EditorUtility.SetDirty(def);
        return def;
    }

    // 프로젝트 창에서 고른 것 중 Art/Voxel/Models/Actor/Monster 안의 FBX만
    private static List<GameObject> GetSelectedModels()
    {
        return Selection.GetFiltered<GameObject>(SelectionMode.Assets)
            .Where(go =>
            {
                string path = AssetDatabase.GetAssetPath(go);
                return path.StartsWith(BattleSetup.MonsterModelDir) && path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);
            })
            .ToList();
    }
}
