using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 데이터베이스 갱신·검사. 플레이 버튼을 누를 때도 자동으로 돌아서 새 SO를 만들기만 하면 게임에 잡힌다
[InitializeOnLoad]
public static class DataTools
{
    static DataTools()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                RefreshDatabase(false);
        };
    }

    public const string DatabasePath = "Assets/Resources/GameDatabase.asset";

    public static GameDatabase RefreshDatabase(bool isVerbose)
    {
        GameDatabase db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
        if (db == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);

            db = ScriptableObject.CreateInstance<GameDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }

        List<DataDef> listDef = CollectDefs();
        db.EditorSetList(listDef);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssetIfDirty(db);

        bool isValid = Validate(listDef, isVerbose);
        if (isVerbose && isValid)
            CLogger.Log("DataTools", $"데이터베이스 갱신 완료: {listDef.Count}개");

        return db;
    }

    public static List<DataDef> CollectDefs()
    {
        return AssetDatabase.FindAssets("t:DataDef", new[] { "Assets" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<DataDef>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(d => d != null)
            .OrderBy(d => d.GetType().Name).ThenBy(d => d.ID)
            .ToList();
    }

    public static bool ValidateAll() => Validate(CollectDefs(), true);

    // ID 비어 있음, ID 중복을 잡는다. 문제가 없으면 true
    private static bool Validate(List<DataDef> listDef, bool isVerbose)
    {
        int errorCount = 0;

        foreach (DataDef def in listDef.Where(d => string.IsNullOrWhiteSpace(d.ID)))
        {
            CLogger.Error("DataTools", $"ID가 비어 있음: {AssetDatabase.GetAssetPath(def)}", def);
            errorCount++;
        }

        IEnumerable<IGrouping<string, DataDef>> groups = listDef.Where(d => string.IsNullOrWhiteSpace(d.ID) == false)
            .GroupBy(d => d.ID)
            .Where(g => g.Count() > 1);

        foreach (IGrouping<string, DataDef> group in groups)
        {
            string paths = string.Join(", ", group.Select(AssetDatabase.GetAssetPath));

            CLogger.Error("DataTools", $"ID 중복 '{group.Key}': {paths}", group.First());
            errorCount++;
        }

        if (isVerbose && errorCount == 0)
            CLogger.Log("DataTools", $"검사 통과: {listDef.Count}개");

        return errorCount == 0;
    }
}
