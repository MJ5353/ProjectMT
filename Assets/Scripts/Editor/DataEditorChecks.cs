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
        string modelFolder = BattleSetup.MonsterModelDir + testName;
        string defPath = BattleSetup.MonsterDataDir + "Monster_" + testName + ".asset";
        string prefabPath = BattleSetup.PrefabDir + "Monster_" + testName + ".prefab";
        string renamedPrefabPath = BattleSetup.PrefabDir + testName + "_Renamed.prefab";
        string controllerPath = BattleSetup.PrefabDir + "Animators/" + testName + ".controller";
        try
        {
            CheckRelationsAndUndo();
            CheckTypeSwitching(listOriginal);
            List<Type> listType = DataEditorUtility.CollectTypes();
            Require(listType.Contains(typeof(MonsterDef)) && listType.Contains(typeof(RegionDef)) && listType.Contains(typeof(CurrencyDef)), "Def 타입 탐색 실패");
            Require(listType.All(type => type.IsAbstract == false), "추상 타입이 생성 목록에 포함됨");
            RequireThrows(() => DataEditorUtility.CreateDef(typeof(DataDef), "Assets/Invalid.asset"), "추상 Def 생성 허용");
            RequireThrows(() => DataEditorUtility.CreateDef(typeof(MushroomDef), "Assets/../Invalid.asset"), "Assets 밖 경로 허용");

            GameObject source = BattleSetup.LoadModel("Buggi");
            Require(source != null, "검사용 원본 모델 없음");
            AssetDatabase.CreateFolder(BattleSetup.MonsterModelDir.TrimEnd('/'), testName);
            string modelPath = modelFolder + "/" + testName + ".fbx";
            Require(AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), modelPath), "임시 FBX 복사 실패");
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            Require(model != null && model.name == testName, "임시 모델 이름 불일치");
            Require(MonsterPrefabBuilder.FindUnregisteredModels(listOriginal.OfType<MonsterDef>()).Contains(model), "미등록 모델 탐색 실패");

            MonsterDef created = MonsterPrefabBuilder.CreateDef(model);
            Require(AssetDatabase.GetAssetPath(created) == defPath, "생성 경로 불일치");
            Require(created.Prefab == null, "Def 등록이 프리팹까지 생성함");
            Require(created.ListMushroom.Count == model.GetComponentsInChildren<Transform>(true).Count(bone => bone.name.StartsWith("Socket_")), "모델 소켓 등록 실패");
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
            Require(created.Prefab != null && AssetDatabase.GetAssetPath(created.Prefab) == prefabPath, "Def의 프리팹 연결 실패");
            string prefabGUID = AssetDatabase.AssetPathToGUID(prefabPath);
            Require(string.IsNullOrEmpty(AssetDatabase.MoveAsset(prefabPath, renamedPrefabPath)), "임시 프리팹 이동 실패");
            Require(MonsterPrefabBuilder.GetPrefabPath(created) == renamedPrefabPath, "기존 프리팹 경로 무시");
            Require(MonsterPrefabBuilder.Build(new[] { created }, false) == 0, "누락 복구가 기존 프리팹을 덮어씀");
            Require(MonsterPrefabBuilder.Build(new[] { created }, true) == 1, "이동된 프리팹 재빌드 실패");
            Require(AssetDatabase.AssetPathToGUID(renamedPrefabPath) == prefabGUID && created.MaxHp == 41, "재빌드가 GUID 또는 수치를 변경함");
            AssetDatabase.SaveAssetIfDirty(created);
            AssetDatabase.ImportAsset(defPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            MonsterDef saved = AssetDatabase.LoadAssetAtPath<MonsterDef>(defPath);
            Require(saved.MaxHp == 41 && saved.Prefab != null, "저장 후 값·참조 유지 실패");

            foreach (var pair in dicOriginal)
            {
                Require(File.ReadAllText(pair.Key) == pair.Value, "기존 Def 파일 변경: " + pair.Key);
            }
            Debug.Log("DataEditorChecks 통과: 타입·검색·직접/ID 연관·Undo/Redo·모델 차이·중복 방지·등록·선택 빌드·GUID·저장·기존 데이터 보존");
        }
        finally
        {
            // 이번 실행에서 만든 고유 경로만 정리한다.
            foreach (string path in new[] { defPath, prefabPath, renamedPrefabPath, controllerPath, modelFolder })
            {
                if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)) == false)
                    AssetDatabase.DeleteAsset(path);
            }
        }
    }

    // ------------------------------------------------

    public static void RunDeletion()
    {
        string name = "DataEditorDeleteCheck_" + Guid.NewGuid().ToString("N");
        string folder = "Assets/" + name;
        string targetPath = folder + "/" + name + "_Mushroom.asset";
        string otherPath = folder + "/" + name + "_Currency.asset";
        string regionPath = folder + "/" + name + "_Region.asset";
        string pickupPath = folder + "/Pickup.prefab";
        Scene preview = EditorSceneManager.NewPreviewScene();
        MushroomDef transient = ScriptableObject.CreateInstance<MushroomDef>();
        try
        {
            AssetDatabase.CreateFolder("Assets", name);
            MushroomDef target = (MushroomDef)DataEditorUtility.CreateDef(typeof(MushroomDef), targetPath);
            CurrencyDef other = (CurrencyDef)DataEditorUtility.CreateDef(typeof(CurrencyDef), otherPath);
            RegionDef region = (RegionDef)DataEditorUtility.CreateDef(typeof(RegionDef), regionPath);
            region.EditorSetup(1, null, Vector2.zero, new List<MushroomDef> { target });
            EditorUtility.SetDirty(region);
            AssetDatabase.SaveAssetIfDirty(region);

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
            Require(listReference.Contains(regionPath), "삭제 대상의 SO 참조 누락");
            Require(listReference.Contains(pickupPath), "삭제 대상의 픽업 ID 참조 누락");
            Require(DataEditorUtility.FindDeleteReferences(new DataDef[] { target, region }).Contains(regionPath) == false, "같이 삭제할 Def가 외부 참조로 표시됨");

            MushroomDef child = ScriptableObject.CreateInstance<MushroomDef>();
            child.name = name + "_SubAsset";
            AssetDatabase.AddObjectToAsset(child, other);
            RequireThrows(() => DataEditorUtility.DeleteDefs(new DataDef[] { target, child }), "하위 에셋 삭제 허용");
            Require(File.Exists(targetPath), "전체 검증 전에 일부 Def가 삭제됨");
            RequireThrows(() => DataEditorUtility.DeleteDefs(new DataDef[] { transient }), "미저장 Def 삭제 허용");
            RequireThrows(() => DataEditorUtility.DeleteDefs(Array.Empty<DataDef>()), "빈 선택 삭제 허용");

            string targetID = target.ID;
            string regionContents = File.ReadAllText(regionPath);
            Require(DataEditorUtility.DeleteDefs(new DataDef[] { target, other, target }).Count == 0, "다중 Def 삭제 실패");
            Require(File.Exists(targetPath) == false && File.Exists(targetPath + ".meta") == false
                && File.Exists(otherPath) == false && File.Exists(otherPath + ".meta") == false, "Def 또는 메타 파일이 남음");
            Require(DataTools.CollectDefs().Any(def => def.ID == targetID) == false, "삭제 후 데이터 수집에 남음");
            Require(File.Exists(pickupPath) && File.ReadAllText(regionPath) == regionContents, "삭제가 참조 에셋까지 변경함");
            Require(AssetDatabase.LoadAssetAtPath<GameObject>(pickupPath).GetComponent<MushroomPickup>().ItemID == targetID, "기존 문자열 ID를 변경함");
            Require(DataEditorUtility.DeleteDefs(new DataDef[] { region }).Count == 0 && File.Exists(regionPath) == false, "단일 Def 삭제 실패");
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
        RegionDef region = ScriptableObject.CreateInstance<RegionDef>();
        Scene preview = EditorSceneManager.NewPreviewScene();
        GameObject pickupObject = new GameObject("DataEditorCheckPickup");
        SceneManager.MoveGameObjectToScene(pickupObject, preview);
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

            region.EditorSetup(1, region, Vector2.zero, new List<MushroomDef> { mushroom, mushroom });
            region.EditorSetSpawns(new List<RegionDef.SpawnEntry> { new(2, monster, 1f), new(3, monster, 1f) });
            List<DataDef> listDef = new() { mushroom, monster, region };
            List<DataDef> listRelated = DataEditorUtility.FindRelatedDefs(region, listDef);
            Require(listRelated.Count == 2 && listRelated.Contains(mushroom) && listRelated.Contains(monster), "직접 연관의 중복 제거·자기 참조 제외 실패");

            MushroomPickup pickup = pickupObject.AddComponent<MushroomPickup>();
            using (SerializedObject serializedPickup = new SerializedObject(pickup))
            {
                serializedPickup.FindProperty("m_ItemID").stringValue = mushroom.ID;
                serializedPickup.ApplyModifiedPropertiesWithoutUndo();
            }
            monster.EditorSetup(new AttackSpec(), 0f, new List<MonsterDef.DropEntry> { new(pickup, 1, 1, 1f), new(pickup, 2, 2, 0.5f) });
            listRelated = DataEditorUtility.FindRelatedDefs(monster, listDef);
            Require(listRelated.Count == 1 && listRelated[0] == mushroom, "픽업 ID 연관 해석 실패");
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
            UnityEngine.Object.DestroyImmediate(region);
            UnityEngine.Object.DestroyImmediate(monster);
            UnityEngine.Object.DestroyImmediate(mushroom);
            EditorSceneManager.ClosePreviewScene(preview);
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
