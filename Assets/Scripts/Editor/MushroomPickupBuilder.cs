using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ==================================================

// 버섯 픽업 프리팹 일괄 제작. MushroomDef 한 장 = 픽업 한 개(Prefabs/Actor/Pickup/Pickup_NNN_Key.prefab, 모델 Mush_NNN_Key.fbx).
// Def는 픽업을 참조하지 않는다. 드랍 자동 배정은 MushroomDef.PickupPath(이름 규칙)로 찾는다.
// !!) 이미 있는 픽업은 기본으로 덮어쓰지 않는다(ID만 맞춤). 덮어쓰면 몬스터 드랍 참조가 끊길 수 있으니 강제 빌드 후에는 드랍 자동 배정을 다시 돌릴 것

public static class MushroomPickupBuilder
{
    private const float ModelScale = 0.8f;
    private const string LegacyRingName = "RarityRing"; // 예전에 픽업마다 붙였던 고리 자식. 남아 있으면 지운다
    private const float DefaultRingRadius = 0.3f;

    // 희귀도 고리 FX(FX_Rarity) ---
    private const float RingLifetime = 0.9f;        // 고리 큐브 하나가 떠 있는 시간(초)
    private const float RingEmissionRate = 18f;     // 초당 큐브 수
    private const float RingRiseSpeed = 0.35f;      // 큐브가 떠오르는 속도
    private const float RingParticleSizeMin = 0.04f;
    private const float RingParticleSizeMax = 0.07f;

    // ------------------------------------------------

    public static void BuildAll(bool isForce)
    {
        List<MushroomDef> listDef = DataTools.CollectDefs().OfType<MushroomDef>().ToList();
        Build(listDef, isForce);
    }

    // 만든 개수를 돌려준다. ID만 고친 것은 세지 않는다
    public static int Build(IEnumerable<MushroomDef> listDef, bool isForce)
    {
        Directory.CreateDirectory(EditorPath.PickupPrefabDir);
        BuildRarityEffect(isForce);

        int builtCount = 0;
        List<string> listFailed = new();
        foreach (MushroomDef def in listDef)
        {
            string path = GetPickupPath(def);
            bool isExist = File.Exists(path);
            if (isExist == false || isForce)
            {
                if (BuildPickup(def, path))
                    builtCount++;
                else
                    listFailed.Add(def.name);
            }
            else
            {
                FixItemID(def, path);
                FixRingRadius(path);
            }
        }

        AssetDatabase.SaveAssets();

        if (listFailed.Count > 0)
            CLogger.Warning("MushroomPickupBuilder", $"픽업 {builtCount}개 제작, 실패 {listFailed.Count}개(모델 없음): {string.Join(", ", listFailed)}");
        else
            CLogger.Log("MushroomPickupBuilder", $"픽업 {builtCount}개 제작");

        return builtCount;
    }

    // MushroomDef.PickupPath(폴더 + Pickup_NNN_Key)의 에셋 경로
    public static string GetPickupPath(MushroomDef def) => EditorPath.ResourcesRoot + def.PickupPath + EditorPath.PrefabExtension;

    // 없으면 null(아직 빌드 전)
    public static MushroomPickup LoadPickup(MushroomDef def) => AssetDatabase.LoadAssetAtPath<MushroomPickup>(GetPickupPath(def));

    // ==================================================

    private static bool BuildPickup(MushroomDef def, string path)
    {
        GameObject modelAsset = BattleSetup.LoadModel(def.ModelName);
        if (modelAsset == null)
        {
            CLogger.Error("MushroomPickupBuilder", $"모델이 없어 건너뜀: {def.name} → {def.ModelName}.fbx", def);
            return false;
        }

        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(path));
        try
        {
            MushroomPickup pickup = root.AddComponent<MushroomPickup>();
            BattleSetup.SetString(pickup, "m_ItemID", def.ID);

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            model.transform.SetParent(root.transform, false);
            model.transform.localScale = Vector3.one * ModelScale;

            SetRingRadius(pickup, MeasureRadius(root));

            return PrefabUtility.SaveAsPrefabAsset(root, path) != null;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // 희귀도 고리 FX: 모든 픽업이 같이 쓰는 프리팹 하나. 버섯 둘레 바닥 테두리에서 작은 복셀 큐브가 피어오른다.
    // 색·반지름은 런타임(EffectService.ShowRarity)에서 정한다
    private static void BuildRarityEffect(bool isForce)
    {
        string path = EditorPath.ResourcesRoot + CommonPath.RarityEffect + EditorPath.PrefabExtension;
        if (File.Exists(path) && isForce == false)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(path));

        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(path));
        try
        {
            ParticleSystem particle = root.AddComponent<ParticleSystem>();
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particle.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startLifetime = RingLifetime;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(RingParticleSizeMin, RingParticleSizeMax);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;

            ParticleSystem.EmissionModule emission = particle.emission;
            emission.rateOverTime = RingEmissionRate;

            ParticleSystem.ShapeModule shape = particle.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = DefaultRingRadius;
            shape.radiusThickness = 0f;                 // 테두리에서만 나온다
            shape.rotation = new Vector3(-90f, 0f, 0f); // 원을 바닥(XZ)에 눕힌다

            ParticleSystem.VelocityOverLifetimeModule velocity = particle.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = 0f;
            velocity.y = RingRiseSpeed;
            velocity.z = 0f;

            ParticleSystem.SizeOverLifetimeModule size = particle.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = Resources.GetBuiltinResource<Mesh>(CommonPath.BuiltinCube);
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{EditorPath.ResourcesRoot}{CommonPath.FXCubeMaterial}.mat");
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (renderer.sharedMaterial == null)
                CLogger.Warning("MushroomPickupBuilder", "FX_Cube 머티리얼이 없어 희귀도 고리가 분홍으로 보입니다.");

            RarityEffectController controller = root.AddComponent<RarityEffectController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("m_Particle").objectReferenceValue = particle;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // 버섯 모델의 가로 반지름(XZ)
    private static float MeasureRadius(GameObject root)
    {
        float radius = 0f;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            Bounds bounds = renderer.bounds;
            Vector3 center = bounds.center - root.transform.position;
            radius = Mathf.Max(radius, Mathf.Abs(center.x) + bounds.extents.x, Mathf.Abs(center.z) + bounds.extents.z);
        }

        return radius > 0f ? radius : DefaultRingRadius;
    }

    private static void SetRingRadius(MushroomPickup pickup, float radius)
    {
        SerializedObject serialized = new SerializedObject(pickup);
        serialized.FindProperty("m_RingRadius").floatValue = radius;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // 이미 있는 픽업: 반지름을 다시 재고, 예전에 붙였던 고리 자식이 남아 있으면 지운다
    private static void FixRingRadius(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        float radius;
        try
        {
            Transform legacyRing = root.transform.Find(LegacyRingName);
            if (legacyRing != null)
            {
                Object.DestroyImmediate(legacyRing.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }

            radius = MeasureRadius(root);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        MushroomPickup pickup = AssetDatabase.LoadAssetAtPath<MushroomPickup>(path);
        if (pickup == null)
            return;

        SetRingRadius(pickup, radius);
        EditorUtility.SetDirty(pickup);
    }

    // 이름만 바뀐 옛 픽업은 아이템 ID가 옛 값일 수 있어 맞춘다
    private static void FixItemID(MushroomDef def, string path)
    {
        MushroomPickup pickup = AssetDatabase.LoadAssetAtPath<MushroomPickup>(path);
        if (pickup == null || pickup.ItemID == def.ID)
            return;

        BattleSetup.SetString(pickup, "m_ItemID", def.ID);
        EditorUtility.SetDirty(pickup);
    }
}
