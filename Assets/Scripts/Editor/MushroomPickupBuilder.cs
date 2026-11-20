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
    private const float ModelScale = 0.96f; // 버섯 모델 크기(26.11.20 대장: 0.8의 1.2배)
    private const string LegacyRingName = "RarityRing"; // 예전에 픽업마다 붙였던 고리 자식. 남아 있으면 지운다

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
                FixPickup(path);
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

            return PrefabUtility.SaveAsPrefabAsset(root, path) != null;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // 이미 있는 픽업: 모델 크기를 ModelScale에 맞추고, 예전 고리 자식이 남아 있으면 지운다
    private static void FixPickup(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform legacyRing = root.transform.Find(LegacyRingName);
            if (legacyRing != null)
                Object.DestroyImmediate(legacyRing.gameObject);

            // 모델 자식 이름은 FBX 이름(Mush_Songi 등)이라 Def 값과 다를 수 있다. 픽업 자식은 모델 하나뿐이다
            if (root.transform.childCount > 0)
                root.transform.GetChild(0).localScale = Vector3.one * ModelScale;

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
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
