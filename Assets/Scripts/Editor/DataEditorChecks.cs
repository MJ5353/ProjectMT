using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// 임시 모델·SO·프리팹만 사용한다. 실행은 Unity 명령 또는 테스트 코드에서 호출한다.
public static class DataEditorChecks
{
    public static void Run()
    {
        List<DataDef> listOriginal = DataTools.CollectDefs();
        Dictionary<string, string> dicOriginal = listOriginal.ToDictionary(AssetDatabase.GetAssetPath, def => File.ReadAllText(AssetDatabase.GetAssetPath(def)));
        string testName = "DataEditorCheck_" + Guid.NewGuid().ToString("N");
        string modelFolder = EditorPath.MonsterModelDir + testName;
        string defPath = EditorPath.MonsterDataDir + "Monster_" + testName + ".asset";
        string prefabPath = EditorPath.MonsterPrefabDir + "Monster_" + testName + EditorPath.PrefabExtension;
        string controllerPath = EditorPath.AnimatorDir + testName + ".controller";
        try
        {
            CheckRelationsAndUndo();
            CheckTypeSwitching(listOriginal);
            List<Type> listType = DataEditorUtility.CollectTypes();
            Require(listType.Contains(typeof(MonsterDef)) && listType.Contains(typeof(MapDef)) && listType.Contains(typeof(CurrencyDef)), "Def 타입 탐색 실패");
            Require(listType.All(type => type.IsAbstract == false), "추상 타입이 생성 목록에 포함됨");
            RequireThrows(() => DataEditorUtility.CreateDef(typeof(DataDef), EditorPath.CheckAbstractDef), "추상 Def 생성 허용");
            RequireThrows(() => DataEditorUtility.CreateDef(typeof(MushroomDef), EditorPath.CheckOutsideDef), "Assets 밖 경로 허용");

            GameObject source = BattleSetup.LoadModel("Buggi");
            Require(source != null, "검사용 원본 모델 없음");
            AssetDatabase.CreateFolder(EditorPath.MonsterModelDir.TrimEnd('/'), testName);
            string modelPath = modelFolder + "/" + testName + ".fbx";
            Require(AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), modelPath), "임시 FBX 복사 실패");
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            Require(model != null && model.name == testName, "임시 모델 이름 불일치");
            Require(MonsterPrefabBuilder.FindUnregisteredModels(listOriginal.OfType<MonsterDef>()).Contains(model), "미등록 모델 탐색 실패");

            MonsterDef created = MonsterPrefabBuilder.CreateDef(model);
            Require(AssetDatabase.GetAssetPath(created) == defPath, "생성 경로 불일치");
            Require(MonsterPrefabBuilder.GetPrefabPath(created) == prefabPath, "프리팹 경로가 Def ID 규칙과 다름");
            Require(File.Exists(prefabPath) == false, "Def 등록이 프리팹까지 생성함");
            using (SerializedObject serializedDef = new SerializedObject(created))
            {
                DataEditorUtility.FindProperty(serializedDef, nameof(UnitDef.MaxHp)).intValue = 41;
                serializedDef.ApplyModifiedProperties();
            }
            Require(MonsterPrefabBuilder.CreateDef(model) == created && created.MaxHp == 41, "재등록이 기존 Def 또는 수치를 변경함");
            Require(MonsterPrefabBuilder.FindUnregisteredModels(DataTools.CollectDefs().OfType<MonsterDef>()).Contains(model) == false, "등록된 모델이 미등록으로 남음");
            RequireThrows(() => DataEditorUtility.CreateDef(typeof(MushroomDef), defPath), "기존 에셋 덮어쓰기 허용");
            RequireThrows(() => DataEditorUtility.CreateDef(typeof(MushroomDef), modelFolder + "/" + created.ID + ".asset"), "중복 ID 생성 허용");

            Require(MonsterPrefabBuilder.Build(new[] { created }, true) == 1, "선택 프리팹 빌드 실패");
            Require(Resources.Load<UnitController>(created.PrefabPath) != null, "Def ID 이름의 프리팹이 Resources 경로에 없음");
            string prefabGUID = AssetDatabase.AssetPathToGUID(prefabPath);
            Require(MonsterPrefabBuilder.Build(new[] { created }, false) == 0, "누락 복구가 기존 프리팹을 덮어씀");
            Require(MonsterPrefabBuilder.Build(new[] { created }, true) == 1, "프리팹 재빌드 실패");
            Require(AssetDatabase.AssetPathToGUID(prefabPath) == prefabGUID && created.MaxHp == 41, "재빌드가 GUID 또는 수치를 변경함");
            AssetDatabase.SaveAssetIfDirty(created);
            AssetDatabase.ImportAsset(defPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            MonsterDef saved = AssetDatabase.LoadAssetAtPath<MonsterDef>(defPath);
            Require(saved.MaxHp == 41, "저장 후 값 유지 실패");

            foreach (var pair in dicOriginal)
            {
                Require(File.ReadAllText(pair.Key) == pair.Value, "기존 Def 파일 변경: " + pair.Key);
            }
            Debug.Log("DataEditorChecks 통과: 타입·검색·직접/ID 연관·Undo/Redo·모델 차이·중복 방지·등록·선택 빌드·GUID·저장·기존 데이터 보존");
        }
        finally
        {
            // 이번 실행에서 만든 고유 경로만 정리한다.
            foreach (string path in new[] { defPath, prefabPath, controllerPath, modelFolder })
            {
                if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)) == false)
                    AssetDatabase.DeleteAsset(path);
            }
        }
    }

    // ------------------------------------------------

    public static void RunMushroomRegistration()
    {
        string name = "DataEditorMushroomCheck_" + Guid.NewGuid().ToString("N");
        List<MushroomDef> listOriginal = DataTools.CollectDefs().OfType<MushroomDef>().ToList();
        Dictionary<string, string> dicOriginal = listOriginal.ToDictionary(AssetDatabase.GetAssetPath, def => File.ReadAllText(AssetDatabase.GetAssetPath(def)));
        int dexNo = Enumerable.Range(900000, 1000).First(number => listOriginal.All(def => def.DexNo != number));
        string modelName = $"Mush_{dexNo}_{name}";
        string dataID = $"Mushroom_{dexNo}_{name}";
        string folder = EditorPath.MushroomModelDir + name;
        string defPath = EditorPath.MushroomDataDir + dataID + ".asset";
        string renamedPath = EditorPath.MushroomDataDir + name + ".asset";
        try
        {
            Require(MushroomDefBuilder.TryGetIdentity("Mush_003_Malbul", out string parsedID, out int parsedNo)
                && parsedID == "Mushroom_003_Malbul" && parsedNo == 3, "버섯 이름·번호 해석 실패");
            Require(MushroomDefBuilder.TryGetIdentity("Mush_003_Malbul_Broken", out _, out _) == false, "파손 모델 등록 허용");
            Require(MushroomDefBuilder.TryGetIdentity("Mush_000_Invalid", out _, out _) == false, "잘못된 도감 번호 허용");
            Require(MushroomDefBuilder.TryGetIdentity("Mush_Songi", out _, out _) == false, "번호 없는 모델 등록 허용");

            string sourcePath = AssetDatabase.FindAssets("t:Model", new[] { EditorPath.MushroomModelDir.TrimEnd('/') })
                .Select(AssetDatabase.GUIDToAssetPath)
                .First(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)
                    && MushroomDefBuilder.TryGetIdentity(Path.GetFileNameWithoutExtension(path), out _, out _));
            AssetDatabase.CreateFolder(EditorPath.MushroomModelDir.TrimEnd('/'), name);
            string modelPath = folder + "/" + modelName + ".fbx";
            string brokenPath = folder + "/" + modelName + "_Broken.fbx";
            Require(AssetDatabase.CopyAsset(sourcePath, modelPath) && AssetDatabase.CopyAsset(sourcePath, brokenPath), "임시 버섯 모델 복사 실패");
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(brokenPath, ImportAssetOptions.ForceSynchronousImport);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            GameObject broken = AssetDatabase.LoadAssetAtPath<GameObject>(brokenPath);
            List<GameObject> listSuggested = MushroomDefBuilder.FindUnregisteredModels(listOriginal);
            Require(listSuggested.Contains(model) && listSuggested.Contains(broken) == false, "원형 제안·파손 모델 제외 실패");
            RequireThrows(() => MushroomDefBuilder.CreateDef(broken), "파손 모델 Def 생성 허용");

            MushroomDef created = MushroomDefBuilder.CreateDef(model);
            Require(created.ID == dataID && created.DexNo == dexNo && AssetDatabase.GetAssetPath(created) == defPath, "버섯 Def ID·도감 번호·경로 불일치");
            created.EditorSetIdentity(name + "_CustomID", "직접 수정한 이름");
            using (SerializedObject serializedDef = new SerializedObject(created))
            {
                DataEditorUtility.FindProperty(serializedDef, nameof(MushroomDef.Price)).intValue = 77;
                serializedDef.ApplyModifiedProperties();
            }
            AssetDatabase.SaveAssetIfDirty(created);
            Require(string.IsNullOrEmpty(AssetDatabase.RenameAsset(defPath, name)), "임시 Def 이름 변경 실패");
            Require(MushroomDefBuilder.CreateDef(model) == created && created.DisplayName == "직접 수정한 이름" && created.Price == 77, "재등록이 기존 값을 덮어씀");
            Require(MushroomDefBuilder.FindUnregisteredModels(DataTools.CollectDefs().OfType<MushroomDef>()).Contains(model) == false, "같은 도감 번호가 중복 제안됨");
            AssetDatabase.ImportAsset(renamedPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            Require(AssetDatabase.LoadAssetAtPath<MushroomDef>(renamedPath).DexNo == dexNo, "저장 후 도감 번호 유지 실패");

            Require(DataEditorUtility.DeleteDefs(new DataDef[] { created }).Count == 0, "임시 버섯 Def 삭제 실패");
            Require(MushroomDefBuilder.FindUnregisteredModels(DataTools.CollectDefs().OfType<MushroomDef>()).Contains(model), "삭제 후 모델이 다시 제안되지 않음");
            foreach (var pair in dicOriginal)
            {
                Require(File.ReadAllText(pair.Key) == pair.Value, "기존 버섯 Def 파일 변경: " + pair.Key);
            }
            Debug.Log("DataEditorChecks 버섯 등록 검사 통과: 이름·번호, 파손 제외, 모델 제안, 등록·저장, 중복 방지, 수치 보존, 삭제 후 재제안");
        }
        finally
        {
            foreach (string path in new[] { defPath, renamedPath, folder })
            {
                if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)) == false)
                    AssetDatabase.DeleteAsset(path);
            }
        }
    }

    public static void RunDeletion()
    {
        string name = "DataEditorDeleteCheck_" + Guid.NewGuid().ToString("N");
        string folder = EditorPath.AssetsRoot + name;
        string targetPath = folder + "/" + name + "_Mushroom.asset";
        string otherPath = folder + "/" + name + "_Currency.asset";
        string mapPath = folder + "/" + name + "_Map.asset";
        string pickupPath = folder + "/Pickup.prefab";
        Scene preview = EditorSceneManager.NewPreviewScene();
        MushroomDef transient = ScriptableObject.CreateInstance<MushroomDef>();
        try
        {
            AssetDatabase.CreateFolder("Assets", name);
            MushroomDef target = (MushroomDef)DataEditorUtility.CreateDef(typeof(MushroomDef), targetPath);
            CurrencyDef other = (CurrencyDef)DataEditorUtility.CreateDef(typeof(CurrencyDef), otherPath);
            MapDef mapDef = (MapDef)DataEditorUtility.CreateDef(typeof(MapDef), mapPath);
            mapDef.EditorSetup(1, null, Vector2.zero, new List<MushroomDef> { target });
            EditorUtility.SetDirty(mapDef);
            AssetDatabase.SaveAssetIfDirty(mapDef);

            GameObject pickupObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(pickupObject, preview);
            MushroomPickup pickup = pickupObject.AddComponent<MushroomPickup>();
            using (SerializedObject serializedPickup = new SerializedObject(pickup))
            {
                serializedPickup.FindProperty("m_ItemID").stringValue = target.ID;
                serializedPickup.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(pickupObject, pickupPath);
            List<string> listReference = DataEditorUtility.FindDeleteReferences(new DataDef[] { target });
            Require(listReference.Contains(mapPath), "삭제 대상의 SO 참조 누락");
            Require(listReference.Contains(pickupPath), "삭제 대상의 픽업 ID 참조 누락");
            Require(DataEditorUtility.FindDeleteReferences(new DataDef[] { target, mapDef }).Contains(mapPath) == false, "같이 삭제할 Def가 외부 참조로 표시됨");

            MushroomDef child = ScriptableObject.CreateInstance<MushroomDef>();
            child.name = name + "_SubAsset";
            AssetDatabase.AddObjectToAsset(child, other);
            RequireThrows(() => DataEditorUtility.DeleteDefs(new DataDef[] { target, child }), "하위 에셋 삭제 허용");
            Require(File.Exists(targetPath), "전체 검증 전에 일부 Def가 삭제됨");
            RequireThrows(() => DataEditorUtility.DeleteDefs(new DataDef[] { transient }), "미저장 Def 삭제 허용");
            RequireThrows(() => DataEditorUtility.DeleteDefs(Array.Empty<DataDef>()), "빈 선택 삭제 허용");

            string targetID = target.ID;
            string mapContents = File.ReadAllText(mapPath);
            Require(DataEditorUtility.DeleteDefs(new DataDef[] { target, other, target }).Count == 0, "다중 Def 삭제 실패");
            Require(File.Exists(targetPath) == false && File.Exists(targetPath + ".meta") == false
                && File.Exists(otherPath) == false && File.Exists(otherPath + ".meta") == false, "Def 또는 메타 파일이 남음");
            Require(DataTools.CollectDefs().Any(def => def.ID == targetID) == false, "삭제 후 데이터 수집에 남음");
            Require(File.Exists(pickupPath) && File.ReadAllText(mapPath) == mapContents, "삭제가 참조 에셋까지 변경함");
            Require(AssetDatabase.LoadAssetAtPath<GameObject>(pickupPath).GetComponent<MushroomPickup>().ItemID == targetID, "기존 문자열 ID를 변경함");
            Require(DataEditorUtility.DeleteDefs(new DataDef[] { mapDef }).Count == 0 && File.Exists(mapPath) == false, "단일 Def 삭제 실패");
            Debug.Log("DataEditorChecks 삭제 검사 통과: SO·픽업 ID 참조, 동시 삭제 참조 제외, 전체 대상 사전 검증, 단일·다중 삭제, 메타 정리, 다른 에셋 보존");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(transient);
            EditorSceneManager.ClosePreviewScene(preview);
            if (AssetDatabase.IsValidFolder(folder))
                AssetDatabase.DeleteAsset(folder);
        }
    }

    private static void CheckTypeSwitching(List<DataDef> listDef)
    {
        DataEditorWindow window = EditorWindow.GetWindow<DataEditorWindow>();
        if (window.rootVisualElement.Q("sidebar") == null)
            window.CreateGUI();

        ListView types = window.rootVisualElement.Q("sidebar").Q<ListView>();
        MultiColumnListView table = window.rootVisualElement.Q<MultiColumnListView>();
        int originalIndex = types.selectedIndex;
        List<Type> listType = DataEditorUtility.CollectTypes();
        for (int index = 0; index < listType.Count; index++)
        {
            types.SetSelection(index);
            Type type = listType[index];
            Require(table.itemsSource.Count == listDef.Count(def => def.GetType() == type), "타입 전환 후 행 개수 불일치: " + type.Name);
            foreach (DataDef def in table.itemsSource)
            {
                Require(def.GetType() == type, "다른 타입의 행이 남음: " + type.Name);
            }
        }
        types.SetSelection(originalIndex >= 0 ? originalIndex : 0);
    }

    private static void CheckRelationsAndUndo()
    {
        MushroomDef mushroom = ScriptableObject.CreateInstance<MushroomDef>();
        MonsterDef monster = ScriptableObject.CreateInstance<MonsterDef>();
        MapDef mapDef = ScriptableObject.CreateInstance<MapDef>();
        try
        {
            mushroom.EditorSetIdentity("Check_Mushroom", "검사 버섯");
            monster.EditorSetIdentity("Check_Monster", "검사 몬스터");
            Require(DataEditorUtility.MatchesSearch(monster, "check_monster"), "ID 대소문자 검색 실패");
            Require(DataEditorUtility.MatchesSearch(monster, "검사"), "표시 이름 검색 실패");
            Require(DataEditorUtility.MatchesSearch(monster, "없는 값") == false, "검색 필터 실패");
            monster.EditorSetIdentity(null, null);
            Require(DataEditorUtility.MatchesSearch(monster, "없는 값") == false, "빈 ID 검색 실패");
            monster.EditorSetIdentity("Check_Monster", "검사 몬스터");

            mapDef.EditorSetup(1, mapDef, Vector2.zero, new List<MushroomDef> { mushroom, mushroom });
            mapDef.EditorSetSpawns(new List<MapDef.SpawnEntry> { new(2, monster, 1f), new(3, monster, 1f) });
            List<DataDef> listDef = new() { mushroom, monster, mapDef };
            List<DataDef> listRelated = DataEditorUtility.FindRelatedDefs(mapDef, listDef);
            Require(listRelated.Count == 2 && listRelated.Contains(mushroom) && listRelated.Contains(monster), "직접 연관의 중복 제거·자기 참조 제외 실패");

            monster.EditorSetup(new List<MonsterDef.DropEntry> { new(mushroom, 1, 1, 1f), new(mushroom, 2, 2, 0.5f) });
            listRelated = DataEditorUtility.FindRelatedDefs(monster, listDef);
            Require(listRelated.Count == 1 && listRelated[0] == mushroom, "드랍 버섯 연관 해석 실패");
            using (SerializedObject serializedMonster = new SerializedObject(monster))
            {
                int original = monster.MaxHp;
                Undo.IncrementCurrentGroup();
                DataEditorUtility.FindProperty(serializedMonster, nameof(UnitDef.MaxHp)).intValue = original + 1;
                serializedMonster.ApplyModifiedProperties();
                Undo.FlushUndoRecordObjects();
                Require(monster.MaxHp == original + 1, "직렬화 편집 실패");
                Undo.PerformUndo();
                Require(monster.MaxHp == original, "Undo 실패");
                Undo.PerformRedo();
                Require(monster.MaxHp == original + 1, "Redo 실패");
            }
        }
        finally
        {
            Undo.ClearUndo(monster);
            UnityEngine.Object.DestroyImmediate(mapDef);
            UnityEngine.Object.DestroyImmediate(monster);
            UnityEngine.Object.DestroyImmediate(mushroom);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (condition == false)
            throw new InvalidOperationException("DataEditorChecks: " + message);
    }

    private static void RequireThrows(Action action, string message)
    {
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            return;
        }
        catch (InvalidOperationException)
        {
            return;
        }
        throw new InvalidOperationException("DataEditorChecks: " + message);
    }
}
