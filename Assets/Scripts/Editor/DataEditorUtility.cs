using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DataEditorUtility
{
    public static List<Type> CollectTypes()
    {
        return TypeCache.GetTypesDerivedFrom<DataDef>()
            .Where(type => type.IsAbstract == false && type.ContainsGenericParameters == false)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToList();
    }

    public static bool MatchesSearch(DataDef def, string search)
    {
        return string.IsNullOrWhiteSpace(search)
            || def.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || (def.DisplayName ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || (def.ID ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string GetTypeLabel(Type type)
    {
        if (type == typeof(MonsterDef))
            return "몬스터";
        if (type == typeof(PlayerDef))
            return "플레이어";
        if (type == typeof(MushroomDef))
            return "버섯";
        if (type == typeof(MapDef))
            return "지역";
        if (type == typeof(CurrencyDef))
            return "재화";

        return type.Name;
    }

    public static SerializedProperty FindProperty(SerializedObject serializedDef, string name)
    {
        return serializedDef.FindProperty(name) ?? serializedDef.FindProperty($"<{name}>k__BackingField");
    }

    public static List<DataDef> FindRelatedDefs(DataDef owner, IReadOnlyList<DataDef> listDef)
    {
        HashSet<DataDef> setRelated = new();
        using (SerializedObject serializedDef = new SerializedObject(owner))
        {
            SerializedProperty property = serializedDef.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                if (property.objectReferenceValue is DataDef related && related != owner)
                    setRelated.Add(related);

                if (property.objectReferenceValue is MushroomPickup pickup)
                {
                    foreach (DataDef candidate in listDef.Where(def => def is MushroomDef && def.ID == pickup.ItemID))
                    {
                        setRelated.Add(candidate);
                    }
                }
            }
        }

        return setRelated.OrderBy(def => def.GetType().Name, StringComparer.Ordinal)
            .ThenBy(def => def.ID, StringComparer.Ordinal).ToList();
    }

    public static DataDef CreateDef(Type type, string path)
    {
        if (type.IsAbstract || typeof(DataDef).IsAssignableFrom(type) == false)
            throw new ArgumentException("생성할 DataDef 타입을 선택하세요.");
        if (path.StartsWith(CommonPath.AssetsRoot, StringComparison.Ordinal) == false || path.Split('/').Contains("..")
            || path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) == false || File.Exists(path))
            throw new ArgumentException("Assets 아래의 새 .asset 경로를 선택하세요. 기존 에셋은 덮어쓰지 않습니다.");

        string dataID = Path.GetFileNameWithoutExtension(path);
        if (DataTools.CollectDefs().Any(def => def.ID == dataID))
            throw new InvalidOperationException($"ID가 이미 있습니다: {dataID}");

        DataDef created = (DataDef)ScriptableObject.CreateInstance(type);
        created.EditorSetIdentity(dataID, dataID);
        try
        {
            AssetDatabase.CreateAsset(created, path);
            return created;
        }
        catch
        {
            UnityEngine.Object.DestroyImmediate(created);
            throw;
        }
    }

    public static string[] GetDeletePaths(IReadOnlyList<DataDef> listDef)
    {
        if (listDef == null || listDef.Count == 0)
            throw new ArgumentException("삭제할 Def를 선택하세요.");

        List<string> listPath = new();
        foreach (DataDef def in listDef)
        {
            string path = AssetDatabase.GetAssetPath(def);
            if (def == null || AssetDatabase.IsMainAsset(def) == false
                || path.StartsWith(CommonPath.AssetsRoot, StringComparison.Ordinal) == false
                || path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) == false)
                throw new ArgumentException("Assets 아래에 저장된 독립 Def 에셋만 삭제할 수 있습니다.");

            listPath.Add(path);
        }
        return listPath.Distinct(StringComparer.Ordinal).ToArray();
    }

    public static List<string> FindDeleteReferences(IReadOnlyList<DataDef> listDef)
    {
        HashSet<string> setPath = new(GetDeletePaths(listDef), StringComparer.Ordinal);
        HashSet<string> setItemID = new(listDef.OfType<MushroomDef>().Select(def => def.ID), StringComparer.Ordinal);
        HashSet<string> setReference = new(StringComparer.Ordinal);
        foreach (string path in AssetDatabase.GetAllAssetPaths())
        {
            if (path.StartsWith(CommonPath.AssetsRoot, StringComparison.Ordinal) == false || setPath.Contains(path) || path == CommonPath.GameDatabaseAsset)
                continue;

            string extension = Path.GetExtension(path);
            if (extension != ".asset" && extension != ".prefab" && extension != ".unity")
                continue;

            if (AssetDatabase.GetDependencies(path, false).Any(setPath.Contains))
                setReference.Add(path);

            if (extension == ".prefab" && setItemID.Count > 0)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponentsInChildren<MushroomPickup>(true).Any(pickup => setItemID.Contains(pickup.ItemID)))
                    setReference.Add(path);
            }
        }

        List<DataDef> listAll = DataTools.CollectDefs();
        foreach (DataDef owner in listAll.Where(def => setPath.Contains(AssetDatabase.GetAssetPath(def)) == false))
        {
            if (FindRelatedDefs(owner, listAll).Any(listDef.Contains))
                setReference.Add(AssetDatabase.GetAssetPath(owner));
        }
        return setReference.OrderBy(path => path, StringComparer.Ordinal).ToList();
    }

    public static List<string> DeleteDefs(IReadOnlyList<DataDef> listDef)
    {
        string[] paths = GetDeletePaths(listDef);
        List<string> listFailed = new();
        if (AssetDatabase.MoveAssetsToTrash(paths, listFailed) == false && listFailed.Count == 0)
            throw new IOException("Def를 휴지통으로 이동하지 못했습니다.");

        return listFailed;
    }
}
