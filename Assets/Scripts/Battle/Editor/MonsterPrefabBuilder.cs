using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 몬스터 프리팹 일괄 제작. Data/Monsters의 MonsterDef 한 장 = 프리팹 한 개(Prefabs/Actor/{에셋 이름}.prefab).
// 이름: 모델 Mon_NNN_Key.fbx → Def·프리팹 Monster_NNN_Key, ID도 Monster_NNN_Key(DataDef 기본 규칙, 에셋 이름 = ID)
// 모델 등록과 빌드는 MushTaker/데이터 편집기에서 한다.
// !!) 프리팹은 덮어쓴다(GUID 유지라 씬 참조는 안 끊긴다). 프리팹에 손으로 단 것은 사라지니 값은 Def에 둔다
public static class MonsterPrefabBuilder
{
    private const string DefPrefix = "Monster_";
    private const string ModelPrefix = "Mon_";
    private const string SocketPrefix = "Socket_";
    private const float SizeStep = 0.05f;      // 자동 크기 반올림 단위
    private const float RadiusRate = 0.4f;     // 모델 가로·세로 중 짧은 쪽에 곱해 반지름으로
    private const float MinRadius = 0.2f;

    // ==================================================

    // Data/Monsters의 MonsterDef 전부. isForce가 false면 이미 있는 프리팹은 건너뛴다
    public static void BuildAll(bool isForce)
    {
        List<MonsterDef> listDef = AssetDatabase.FindAssets("t:MonsterDef", new[] { BattleSetup.MonsterDataDir.TrimEnd('/') })
            .Select(guid => AssetDatabase.LoadAssetAtPath<MonsterDef>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(d => d != null)
            .OrderBy(d => d.name)
            .ToList();

        Build(listDef, isForce);
    }

    public static int Build(IEnumerable<MonsterDef> listDef, bool isForce)
    {
        Directory.CreateDirectory(BattleSetup.PrefabDir);
        int builtCount = 0;
        List<string> listFailed = new();
        foreach (MonsterDef def in listDef)
        {
            if (File.Exists(GetPrefabPath(def)) == false || isForce)
            {
                if (BuildPrefab(def))
                    builtCount++;
                else
                    listFailed.Add(def.name);
            }

            LinkPrefab(def);
        }

        if (listFailed.Count > 0)
            CLogger.Warning("MonsterPrefabBuilder", $"몬스터 프리팹 {builtCount}개 제작, 실패 {listFailed.Count}개: {string.Join(", ", listFailed)}");
        else
            CLogger.Log("MonsterPrefabBuilder", $"몬스터 프리팹 {builtCount}개 제작");

        return builtCount;
    }

    // Def.Prefab을 구운 프리팹으로 채운다(지역 스폰이 Def에서 프리팹을 꺼내 쓴다)
    private static void LinkPrefab(MonsterDef def)
    {
        MonsterController prefab = AssetDatabase.LoadAssetAtPath<MonsterController>(GetPrefabPath(def));
        if (prefab == null || def.Prefab == prefab)
            return;

        def.EditorSetPrefab(prefab);
        EditorUtility.SetDirty(def);
    }

    public static string GetPrefabPath(MonsterDef def)
    {
        if (def.Prefab != null && AssetDatabase.Contains(def.Prefab))
            return AssetDatabase.GetAssetPath(def.Prefab);

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
        try
        {
            return PrefabUtility.SaveAsPrefabAsset(root, GetPrefabPath(def)) != null;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // 비어 있으면 에셋 이름의 키에 모델 접두사를 붙인 이름(Monster_001_Buggi → Mon_001_Buggi)
    public static string GetModelName(MonsterDef def)
    {
        if (def.ModelName.E_IsNullOrEmpty() == false)
            return def.ModelName;

        return def.name.StartsWith(DefPrefix) ? ModelPrefix + def.name.Substring(DefPrefix.Length) : def.name;
    }

    // 모델 이름에서 접두사를 뗀 키(Mon_001_Buggi → 001_Buggi). 접두사가 없으면 그대로
    public static string GetKey(string modelName)
    {
        return modelName.StartsWith(ModelPrefix) ? modelName.Substring(ModelPrefix.Length) : modelName;
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
    public static MonsterDef CreateDef(GameObject model)
    {
        string modelPath = AssetDatabase.GetAssetPath(model);
        if (model == null || modelPath.StartsWith(BattleSetup.MonsterModelDir, System.StringComparison.Ordinal) == false
            || modelPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) == false)
            throw new System.ArgumentException("몬스터 모델 폴더의 FBX를 선택하세요.");

        MonsterDef existing = DataTools.CollectDefs().OfType<MonsterDef>()
            .FirstOrDefault(candidate => GetModelName(candidate) == model.name);
        if (existing != null)
            return existing;

        Directory.CreateDirectory(BattleSetup.MonsterDataDir);
        string defName = DefPrefix + GetKey(model.name);
        string path = $"{BattleSetup.MonsterDataDir}{defName}.asset";
        if (File.Exists(path) || DataTools.CollectDefs().Any(candidate => candidate.ID == defName))
            throw new System.InvalidOperationException($"에셋 경로 또는 ID가 이미 있습니다: {path}");

        MonsterDef def = ScriptableObject.CreateInstance<MonsterDef>();
        def.EditorSetIdentity(defName, GetKey(model.name));
        AssetDatabase.CreateAsset(def, path);
        BattleSetup.SetString(def, nameof(MonsterDef.ModelName), model.name);

        // 버섯 모델은 비워 둔다. 빈 칸은 붙이지 않으니 Def에서 골라 넣으면 된다
        List<MonsterDef.MushroomSocket> listSocket = model.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name.StartsWith(SocketPrefix))
            .Select(t => new MonsterDef.MushroomSocket(t.name, null, 1f))
            .ToList();
        def.EditorSetMushrooms(listSocket);

        EditorUtility.SetDirty(def);
        return def;
    }

    public static List<GameObject> FindUnregisteredModels(IEnumerable<MonsterDef> listDef)
    {
        HashSet<string> setModel = new(listDef.Select(GetModelName), System.StringComparer.Ordinal);
        return AssetDatabase.FindAssets("t:Model", new[] { BattleSetup.MonsterModelDir.TrimEnd('/') })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .Where(model => model != null && setModel.Contains(model.name) == false)
            .OrderBy(model => model.name, System.StringComparer.Ordinal)
            .ToList();
    }
}
