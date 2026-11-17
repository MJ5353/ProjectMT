using System.Collections.Generic;
using System.IO;
using System.Linq;
using MushTaker.Data;
using UnityEditor;
using UnityEngine;

namespace MushTaker.EditorTools
{
    /// <summary>
    /// 데이터베이스 갱신·검사. 플레이 버튼을 누를 때도 자동으로 돌아서 새 SO를 만들기만 하면 게임에 잡힌다.
    /// </summary>
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
        
        public static GameDatabase RefreshDatabase(bool verbose)
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (db == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
                
                db = ScriptableObject.CreateInstance<GameDatabase>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }

            var defs = CollectDefs();
            db.EditorSetDefs(defs);
            EditorUtility.SetDirty(db);

            foreach (var def in defs)
            {
                AssetDatabase.SaveAssetIfDirty(def);
            }

            bool ok = Validate(defs, verbose);
            if (verbose && ok) 
                Debug.Log($"[DataTools] 데이터베이스 갱신 완료: {defs.Count}개");
            
            return db;
        }
        
        static List<DataDef> CollectDefs()
        {
            return AssetDatabase.FindAssets("t:DataDef", new[] { "Assets" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<DataDef>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(d => d != null)
                .OrderBy(d => d.GetType().Name).ThenBy(d => d.Id)
                .ToList();
        }

        [MenuItem("MushTaker/Data/데이터베이스 갱신")]
        static void RefreshFromMenu() => RefreshDatabase(true);

        [MenuItem("MushTaker/Data/데이터 검사")]
        static void ValidateFromMenu() => Validate(CollectDefs(), true);

        /// id 비어 있음, id 중복을 잡는다. 문제가 없으면 true.
        static bool Validate(List<DataDef> defs, bool verbose)
        {
            int errors = 0;

            foreach (var def in defs.Where(d => string.IsNullOrWhiteSpace(d.Id)))
            {
                Debug.LogError($"[DataTools] id가 비어 있음: {AssetDatabase.GetAssetPath(def)}", def);
                errors++;
            }

            var groups = defs.Where(d => !string.IsNullOrWhiteSpace(d.Id))
                .GroupBy(d => d.Id)
                .Where(g => g.Count() > 1);

            foreach (var group in groups)
            {
                var paths = string.Join(", ", group.Select(AssetDatabase.GetAssetPath));

                Debug.LogError($"[DataTools] id 중복 '{group.Key}': {paths}", group.First());
                errors++;
            }

            if (verbose && errors == 0)
                Debug.Log($"[DataTools] 검사 통과: {defs.Count}개");
            
            return errors == 0;
        }
    }
}