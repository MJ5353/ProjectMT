using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MushroomDefBuilder
{
    private const string ModelPrefix = "Mush_";
    private const string DefPrefix = "Mushroom_";

    // ------------------------------------------------

    public static List<GameObject> FindUnregisteredModels(IEnumerable<MushroomDef> listDef)
    {
        List<MushroomDef> listExisting = listDef.ToList();
        return AssetDatabase.FindAssets("t:Model", new[] { EditorPath.MushroomModelDir.TrimEnd('/') })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .Where(model => model != null && TryGetIdentity(model.name, out string dataID, out int dexNo)
                && FindExisting(listExisting, dataID, dexNo) == null)
            .OrderBy(model => model.name, StringComparer.Ordinal).ToList();
    }

    public static MushroomDef CreateDef(GameObject model)
    {
        string modelPath = AssetDatabase.GetAssetPath(model);
        if (model == null || modelPath.StartsWith(EditorPath.MushroomModelDir, StringComparison.Ordinal) == false
            || modelPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) == false)
            throw new ArgumentException("버섯 모델 폴더의 원형 FBX를 선택하세요.");
        if (TryGetIdentity(model.name, out string dataID, out int dexNo) == false)
            throw new ArgumentException("Mush_번호_이름 형식의 원형 모델만 등록할 수 있습니다. 파손 모델은 제외합니다.");

        MushroomDef existing = FindExisting(DataTools.CollectDefs().OfType<MushroomDef>(), dataID, dexNo);
        if (existing != null)
            return existing;

        Directory.CreateDirectory(EditorPath.MushroomDataDir);
        MushroomDef created = (MushroomDef)DataEditorUtility.CreateDef(typeof(MushroomDef), EditorPath.MushroomDataDir + dataID + ".asset");
        created.EditorSetIdentity(dataID, model.name.Split(new[] { '_' }, 3)[2]);
        created.EditorSetDexNo(dexNo);
        EditorUtility.SetDirty(created);
        return created;
    }

    private static MushroomDef FindExisting(IEnumerable<MushroomDef> listDef, string dataID, int dexNo)
    {
        return listDef.FirstOrDefault(def => def.DexNo == dexNo || def.ID == dataID || def.name == dataID);
    }

    internal static bool TryGetIdentity(string modelName, out string dataID, out int dexNo)
    {
        dataID = null;
        dexNo = 0;
        if (string.IsNullOrWhiteSpace(modelName) || modelName.StartsWith(ModelPrefix, StringComparison.Ordinal) == false
            || modelName.EndsWith("_Broken", StringComparison.OrdinalIgnoreCase))
            return false;

        string[] parts = modelName.Split(new[] { '_' }, 3);
        if (parts.Length != 3 || string.IsNullOrWhiteSpace(parts[2]) || int.TryParse(parts[1], out dexNo) == false || dexNo <= 0)
            return false;

        dataID = DefPrefix + modelName.Substring(ModelPrefix.Length);
        return true;
    }
}
