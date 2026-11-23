using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DataEditorUtility
{
    // 좌측 목록 순서이자 표시 이름. 여기 없는 타입은 뒤에 영문 이름순으로 붙는다
    private static readonly (Type Type, string Label)[] s_TypeOrder =
    {
        (typeof(PlayerDef), "플레이어"),
        (typeof(MonsterDef), "몬스터"),
        (typeof(SkillDef), "스킬"),
        (typeof(AIDef), "AI"),
        (typeof(HitDef), "피격"),
        (typeof(MapDef), "지역"),
        (typeof(MushroomDef), "버섯"),
        (typeof(ItemDef), "아이템"),
        (typeof(RarityDef), "희귀도"),
        (typeof(CurrencyDef), "재화"),
        (typeof(PopupDef), "팝업"),
        (typeof(UIComponentDef), "UI 요소"),
        (typeof(MerchantDef), "상인"),
        (typeof(StringDef), "문구"),
    };

    public static List<Type> CollectTypes()
    {
        return TypeCache.GetTypesDerivedFrom<DataDef>()
            .Where(type => type.IsAbstract == false && type.ContainsGenericParameters == false)
            .Where(type => type != typeof(DefineDef)) // Define은 MushTaker/Define 편집기에서 스펙 단위로 고친다
            .OrderBy(GetTypeOrder)
            .ThenBy(type => type.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static int GetTypeOrder(Type type)
    {
        int index = Array.FindIndex(s_TypeOrder, entry => entry.Type == type);
        return index < 0 ? s_TypeOrder.Length : index;
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
        int index = Array.FindIndex(s_TypeOrder, entry => entry.Type == type);
        return index < 0 ? type.Name : s_TypeOrder[index].Label;
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
            }
        }

        return setRelated.OrderBy(def => def.GetType().Name, StringComparer.Ordinal)
            .ThenBy(def => def.ID, StringComparer.Ordinal).ToList();
    }

    public static DataDef CreateDef(Type type, string path)
    {
        if (type.IsAbstract || typeof(DataDef).IsAssignableFrom(type) == false)
            throw new ArgumentException("생성할 DataDef 타입을 선택하세요.");
        if (path.StartsWith(EditorPath.AssetsRoot, StringComparison.Ordinal) == false || path.Split('/').Contains("..")
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
                || path.StartsWith(EditorPath.AssetsRoot, StringComparison.Ordinal) == false
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
            if (path.StartsWith(EditorPath.AssetsRoot, StringComparison.Ordinal) == false || setPath.Contains(path) || path == EditorPath.GameDatabaseAsset)
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
