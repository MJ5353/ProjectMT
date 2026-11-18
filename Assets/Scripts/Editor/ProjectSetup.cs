using System.IO;
using UnityEditor;
using UnityEngine;

// 샘플 데이터를 만들어 주는 도구. 이미 있는 파일은 덮어쓰지 않는다.
// 씬은 MushTaker/UI(로비)와 MushTaker/Battle(전투) 메뉴가 만든다
public static class ProjectSetup
{
    [MenuItem("MushTaker/Setup/샘플 데이터 만들기")]
    public static void CreateSampleData()
    {
        CreateDef<CurrencyDef>("Assets/Resources/Data/Currencies", "Currency_Gold", "골드");
        CreateDef<MushroomDef>("Assets/Resources/Data/Mushrooms", "Mushroom_Songi", "송이");
        CreateDef<MushroomDef>("Assets/Resources/Data/Mushrooms", "Mushroom_FlyAgaric", "광대버섯");

        AssetDatabase.SaveAssets();
        DataTools.RefreshDatabase(true);
    }

    // ==================================================

    private static void CreateDef<T>(string dir, string dataID, string displayName) where T : DataDef
    {
        string path = $"{dir}/{dataID}.asset";
        if (File.Exists(path))
            return;

        Directory.CreateDirectory(dir);

        T def = ScriptableObject.CreateInstance<T>();
        def.EditorSetIdentity(dataID, displayName);
        AssetDatabase.CreateAsset(def, path);
    }
}
