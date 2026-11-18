using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ==================================================

// 버섯 픽업 프리팹 일괄 제작. MushroomDef 한 장 = 픽업 한 개(Prefabs/Actor/Pickup/Pickup_NNN_Key.prefab, 모델 Mush_NNN_Key.fbx).
// 만든 픽업은 MushroomDef.Pickup에 연결해서 드랍 자동 배정이 버섯 Def에서 바로 꺼내 쓴다.
// !!) 이미 있는 픽업은 기본으로 덮어쓰지 않는다(ID·연결만 맞춤). 덮어쓰면 몬스터 드랍 참조가 끊길 수 있으니 강제 빌드 후에는 드랍 자동 배정을 다시 돌릴 것

public static class MushroomPickupBuilder
{
    public const string PickupDir = "Assets/Resources/Prefabs/Actor/Pickup/";
    private const float ModelScale = 0.8f;

    // ------------------------------------------------

    public static void BuildAll(bool isForce)
    {
        List<MushroomDef> listDef = DataTools.CollectDefs().OfType<MushroomDef>().ToList();
        Build(listDef, isForce);
    }

    // 만든 개수를 돌려준다. 연결만 고친 것은 세지 않는다
    public static int Build(IEnumerable<MushroomDef> listDef, bool isForce)
    {
        Directory.CreateDirectory(PickupDir);

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
            }

            LinkPickup(def, path);
        }

        AssetDatabase.SaveAssets();

        if (listFailed.Count > 0)
            CLogger.Warning("MushroomPickupBuilder", $"픽업 {builtCount}개 제작, 실패 {listFailed.Count}개(모델 없음): {string.Join(", ", listFailed)}");
        else
            CLogger.Log("MushroomPickupBuilder", $"픽업 {builtCount}개 제작");

        return builtCount;
    }

    public static string GetPickupPath(MushroomDef def)
    {
        if (def.Pickup != null && AssetDatabase.Contains(def.Pickup))
            return AssetDatabase.GetAssetPath(def.Pickup);

        return $"{PickupDir}{def.PickupName}.prefab";
    }

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

    // 이름만 바뀐 옛 픽업은 아이템 ID가 옛 값일 수 있어 맞춘다
    private static void FixItemID(MushroomDef def, string path)
    {
        MushroomPickup pickup = AssetDatabase.LoadAssetAtPath<MushroomPickup>(path);
        if (pickup == null || pickup.ItemID == def.ID)
            return;

        BattleSetup.SetString(pickup, "m_ItemID", def.ID);
        EditorUtility.SetDirty(pickup);
    }

    private static void LinkPickup(MushroomDef def, string path)
    {
        MushroomPickup pickup = AssetDatabase.LoadAssetAtPath<MushroomPickup>(path);
        if (pickup == null || def.Pickup == pickup)
            return;

        def.EditorSetPickup(pickup);
        EditorUtility.SetDirty(def);
    }
}
