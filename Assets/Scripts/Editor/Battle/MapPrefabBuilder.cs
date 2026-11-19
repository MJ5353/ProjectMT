using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 맵 프리팹 일괄 제작. Data/Maps의 MapDef 한 장 = 프리팹 한 개(Prefabs/Map/{ID}.prefab, Def는 참조하지 않고 이름으로 찾는다).
// 모델 Map_NNN_Key.fbx를 루트(MapRoot)로 감싸고 스폰 지점을 채운다. 지형 레이어는 VoxelImportSettings가 넣는다
// !!) 프리팹은 덮어쓴다. 프리팹에 손으로 단 것은 사라진다
public static class MapPrefabBuilder
{

    // ==================================================

    // Data/Maps의 MapDef 전부. isForce가 false면 이미 있는 프리팹은 건너뛴다
    public static void BuildAll(bool isForce)
    {
        Directory.CreateDirectory(CommonPath.MapPrefabDir);
        List<MapDef> listDef = AssetDatabase.FindAssets("t:MapDef", new[] { CommonPath.MapDataDir.TrimEnd('/') })
            .Select(guid => AssetDatabase.LoadAssetAtPath<MapDef>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(d => d != null)
            .OrderBy(d => d.name)
            .ToList();

        foreach (MapDef def in listDef)
        {
            // 런타임이 MapDef.PrefabPath(폴더 + ID)로 찾으니 같은 경로에 굽는다
            string path = CommonPath.ResourcesRoot + def.PrefabPath + CommonPath.PrefabExtension;
            if (File.Exists(path) == false || isForce)
                BuildPrefab(def, path);
        }
    }

    private static void BuildPrefab(MapDef def, string path)
    {
        GameObject model = BattleSetup.LoadModel(def.name);
        if (model == null)
        {
            CLogger.Error("MapPrefabBuilder", $"모델이 없어 건너뜀: {def.name}.fbx", def);
            return;
        }

        GameObject root = new GameObject(def.name);
        try
        {
            Transform instance = ((GameObject)PrefabUtility.InstantiatePrefab(model, root.transform)).transform;
            root.AddComponent<MapRoot>().EditorSetSpawnSlots(CollectSpawnSlots(instance));
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // 맵의 SpawnSlot_NN을 [NN-1] 자리에 담는다. 빠진 번호는 null
    private static Transform[] CollectSpawnSlots(Transform map)
    {
        List<Transform> listSlot = BattleSetup.FindAllByPrefix(map, MapDef.SpawnSlotPrefix);
        int maxSlot = 0;
        foreach (Transform t in listSlot)
        {
            if (MapDef.TryParseSlot(t.name, out int slot))
                maxSlot = Mathf.Max(maxSlot, slot);
        }

        Transform[] slots = new Transform[maxSlot];
        foreach (Transform t in listSlot)
        {
            if (MapDef.TryParseSlot(t.name, out int slot) == false || slot <= 0)
                continue;

            if (slots[slot - 1] != null)
                CLogger.Warning("MapPrefabBuilder", $"{t.name}이 둘 이상 있습니다. 먼저 찾은 것을 씁니다.");
            else
                slots[slot - 1] = t;
        }

        return slots;
    }
}
