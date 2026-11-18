using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 26.11.18 명명 규칙 일괄 적용(1회용). GUID를 유지한 채 이름만 바꾸므로 Def·프리팹·씬 참조는 끊기지 않는다.
// 규칙: 모델 Mon_NNN_Key / Mush_NNN_Key, 지역 Def와 맵 모델 Map_NNN_Key, Def·ID·프리팹 Monster_NNN_Key / Mushroom_NNN_Key, 애니메이터 AC_NNN_Key, 픽업 Pickup_NNN_Key, 아이콘 Icon_NNN_Key
// !!) 버섯 ID가 바뀌므로 이전 세이브의 버섯은 사라진다. 적용 후 세이브를 초기화할 것
public static class NamingMigration
{
    private const string MonsterModelDir = "Assets/Art/Voxel/Models/Actor/Monster/";
    private const string MushroomModelDir = "Assets/Art/Voxel/Models/Mushroom/";
    private const string MonsterDataDir = "Assets/Resources/Data/Monsters/";
    private const string MushroomDataDir = "Assets/Resources/Data/Mushrooms/";
    private const string PrefabDir = "Assets/Resources/Prefabs/Actor/";
    private const string AnimatorDir = PrefabDir + "Animators/";
    private const string IconDir = "Assets/Art/UI/Sprites/";
    private const string MapModelDir = "Assets/Art/Voxel/Models/Map/";
    private const string RegionDataDir = "Assets/Resources/Data/Regions/";

    // (옛 키, 새 키, 표시 이름). 표시 이름이 null이면 비워서 ID로 보이게 한다
    private static readonly (string Old, string New, string DisplayName)[] s_Monsters =
    {
        ("M_Ch1_Buggi", "001_Buggi", "버끼"),
        ("M_Ch1_Budeoji", "002_Budeoji", "버더지"),
        ("M_Ch1_Bugom", "003_Bugom", "버곰"),
        ("M_Ch1_Burangi", "004_Burangi", "버랑이"),
        ("M_Ch1_Boss_Tree", "005_Boss_Tree", "나무"),
        ("M_Ch2_Bugwin", "006_Bugwin", "버귄"),
        ("M_Ch2_BukBuggi", "007_BukBuggi", "북버끼"),
        ("M_Ch2_Bubeom", "008_Bubeom", "버범"),
        ("M_Ch2_BukBuyeou", "009_BukBuyeou", "북버여우"),
        ("M_Ch2_Boss_BukBugom", "010_Boss_BukBugom", "북버곰"),
        ("M_Ch3_Buraptor", "011_Buraptor", "랩터"),
        ("M_Ch3_Bupachy", "012_Bupachy", "파키케팔로사우르스"),
        ("M_Ch3_Bursus", "013_Bursus", "버우르스"),
        ("M_Ch3_Bukera", "014_Bukera", null),
        ("M_Ch3_Boss_Bursus", "015_Boss_Bursus", "버우르스 보스"),
    };

    // (옛 모델 키, 새 키)
    private static readonly (string Old, string New)[] s_Mushrooms =
    {
        ("Songi", "001_Songi"),
        ("Cloud", "002_Gureum"),
        ("Malbul", "003_Malbul"),
        ("Noru", "004_Noru"),
        ("Pyogo", "005_Pyogo"),
        ("Paengi", "006_Paengi"),
        ("Gwangdae", "007_Gwangdae"),
        ("Seori", "008_Seori"),
    };

    // (옛 지역 Def 이름, 옛 맵 모델 이름, 새 키). 지역 Def와 맵 모델이 같은 이름 Map_NNN_Key를 쓴다
    private static readonly (string OldRegion, string OldMap, string New)[] s_Maps =
    {
        ("Region_01", "Map_Field01", "001_Field"),
        ("Region_02", "Map_Ice01", "002_Ice"),
        ("Region_03", "Map_Dino01", "003_Dino"),
    };

    // (옛 Def 이름, 새 키)
    private static readonly (string Old, string New)[] s_MushroomDefs =
    {
        ("Mushroom_Songi", "001_Songi"),
        ("Mushroom_Cloud", "002_Gureum"),
        ("Mushroom_FlyAgaric", "007_Gwangdae"),
    };

    // (옛 픽업 이름, 새 키)
    private static readonly (string Old, string New)[] s_Pickups =
    {
        ("Pickup_Songi", "001_Songi"),
        ("Pickup_Cloud", "002_Gureum"),
    };

    // (옛 아이콘 이름, 새 키)
    private static readonly (string Old, string New)[] s_Icons =
    {
        ("Icon_Songi", "001_Songi"),
        ("Icon_Cloud", "002_Gureum"),
        ("Icon_Malbul", "003_Malbul"),
    };

    // 이름 규칙 이전에 남은 옛 프리팹·애니메이터(지금 Def가 참조하지 않음). 휴지통으로 보낸다
    private static readonly string[] s_Obsolete =
    {
        PrefabDir + "Monster_Boss_Bursus.prefab",
        PrefabDir + "Monster_Boss_Tree.prefab",
        PrefabDir + "Monster_Buggi.prefab",
        PrefabDir + "Monster_Bugom.prefab",
        PrefabDir + "Monster_Bursus.prefab",
        PrefabDir + "Monster_Penguin.prefab",
        AnimatorDir + "Boss_Bursus.controller",
        AnimatorDir + "Boss_Tree.controller",
        AnimatorDir + "Buggi.controller",
        AnimatorDir + "Bugom.controller",
        AnimatorDir + "Bursus.controller",
        AnimatorDir + "Penguin.controller",
    };

    // ------------------------------------------------

    [MenuItem("MushTaker/정리/명명 규칙 일괄 적용 (1회)", false, 100)]
    public static void Run()
    {
        if (EditorUtility.DisplayDialog("명명 규칙 일괄 적용", "모델·Def·프리팹·애니메이터·아이콘·지역·맵 이름과 ID를 새 규칙으로 바꾸고, 옛 프리팹 6개·애니메이터 6개를 휴지통으로 보냅니다.\n버섯 ID가 바뀌어 이전 세이브의 버섯은 사라집니다.", "적용", "취소") == false)
            return;

        List<string> listLog = new();

        foreach (var (oldKey, newKey, _) in s_Monsters)
        {
            Rename(listLog, $"{MonsterModelDir}{oldKey}.fbx", $"Mon_{newKey}");
            Rename(listLog, $"{MonsterDataDir}Monster_{oldKey}.asset", $"Monster_{newKey}");
            Rename(listLog, $"{PrefabDir}Monster_{oldKey}.prefab", $"Monster_{newKey}");
            Rename(listLog, $"{AnimatorDir}{oldKey}.controller", $"AC_{newKey}");
        }

        Rename(listLog, $"{AnimatorDir}Player.controller", "AC_Player");

        foreach (var (oldKey, newKey) in s_Mushrooms)
        {
            Rename(listLog, $"{MushroomModelDir}Mush_{oldKey}.fbx", $"Mush_{newKey}");
            Rename(listLog, $"{MushroomModelDir}Mush_{oldKey}_Broken.fbx", $"Mush_{newKey}_Broken");
        }

        foreach (var (oldName, newKey) in s_MushroomDefs)
        {
            Rename(listLog, $"{MushroomDataDir}{oldName}.asset", $"Mushroom_{newKey}");
        }

        foreach (var (oldRegion, oldMap, newKey) in s_Maps)
        {
            Rename(listLog, $"{RegionDataDir}{oldRegion}.asset", $"Map_{newKey}");
            Rename(listLog, $"{MapModelDir}{oldMap}.fbx", $"Map_{newKey}");
        }

        foreach (var (oldName, newKey) in s_Pickups)
        {
            Rename(listLog, $"{PrefabDir}{oldName}.prefab", $"Pickup_{newKey}");
        }

        foreach (var (oldName, newKey) in s_Icons)
        {
            Rename(listLog, $"{IconDir}{oldName}.png", $"Icon_{newKey}");
        }

        foreach (string path in s_Obsolete)
        {
            if (File.Exists(path) == false)
                continue;

            if (AssetDatabase.MoveAssetToTrash(path))
                listLog.Add($"휴지통: {path}");
            else
                listLog.Add($"!! 휴지통 실패: {path}");
        }

        // ID·모델 이름·표시 이름·픽업 아이템 ID
        foreach (var (_, newKey, displayName) in s_Monsters)
        {
            MonsterDef def = AssetDatabase.LoadAssetAtPath<MonsterDef>($"{MonsterDataDir}Monster_{newKey}.asset");
            if (def == null)
                continue;

            def.EditorSetIdentity($"Monster_{newKey}", displayName, def.Description);
            BattleSetup.SetString(def, nameof(MonsterDef.ModelName), $"Mon_{newKey}");
            EditorUtility.SetDirty(def);
        }

        foreach (var (_, newKey) in s_MushroomDefs)
        {
            MushroomDef def = AssetDatabase.LoadAssetAtPath<MushroomDef>($"{MushroomDataDir}Mushroom_{newKey}.asset");
            if (def == null)
                continue;

            def.EditorSetIdentity($"Mushroom_{newKey}", def.DisplayName, def.Description);
            def.EditorSetDexNo(int.Parse(newKey.Substring(0, 3)));
            EditorUtility.SetDirty(def);
        }

        foreach (var (_, _, newKey) in s_Maps)
        {
            RegionDef def = AssetDatabase.LoadAssetAtPath<RegionDef>($"{RegionDataDir}Map_{newKey}.asset");
            if (def == null)
                continue;

            def.EditorSetIdentity($"Map_{newKey}", def.DisplayName, def.Description);
            EditorUtility.SetDirty(def);
        }

        foreach (var (_, newKey) in s_Pickups)
        {
            MushroomPickup pickup = AssetDatabase.LoadAssetAtPath<MushroomPickup>($"{PrefabDir}Pickup_{newKey}.prefab");
            if (pickup == null)
                continue;

            BattleSetup.SetString(pickup, "m_ItemID", $"Mushroom_{newKey}");
            EditorUtility.SetDirty(pickup);
        }

        // 모든 Def를 다시 저장해 옛 직렬화 이름(m_ID 등)을 새 형식으로 옮긴다
        foreach (DataDef def in DataTools.CollectDefs())
        {
            EditorUtility.SetDirty(def);
        }

        AssetDatabase.SaveAssets();
        DataTools.RefreshDatabase(false);

        CLogger.Log("NamingMigration", $"명명 규칙 적용 완료 ({listLog.Count}건)\n{string.Join("\n", listLog)}");
    }

    // ==================================================

    private static void Rename(List<string> listLog, string path, string newName)
    {
        if (File.Exists(path) == false)
            return;

        string error = AssetDatabase.RenameAsset(path, newName);
        listLog.Add(error.E_IsNullOrEmpty() ? $"{Path.GetFileName(path)} → {newName}" : $"!! {path}: {error}");
    }
}
