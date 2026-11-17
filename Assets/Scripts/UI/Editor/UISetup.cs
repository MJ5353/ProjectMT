using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// UI 공통 기반 메뉴. 순서대로 한 번씩 누르면 된다. 이미 있는 에셋은 덮어쓰지 않는다
public static class UISetup
{
    private const string TMPSettingsPath = "TMP Settings"; // Resources 경로

    // 갈무리11 원본 격자(11px)의 4배. 22·33·44·66 크기에서 고르게 보인다
    private const int SamplingPointSize = 44;
    private const int AtlasPadding = 5;
    private const int AtlasSize = 2048;

    // ==================================================

    [MenuItem("MushTaker/UI/1. 공통 기반 설정 (가로 고정, 폰트)")]
    public static void SetupBase()
    {
        SetLandscape();

        TMP_FontAsset fontAsset = CreateFontAsset();
        if (fontAsset == null)
            return;

        SetDefaultFont(fontAsset);
        AssetDatabase.SaveAssets();

        CLogger.Log("UISetup", "공통 기반 설정 완료. Game 뷰 해상도를 1920x1080으로 두고 확인하세요.");
    }

    [MenuItem("MushTaker/UI/2. 공용 부품 프리팹 만들기")]
    public static void CreateCommonPrefabs()
    {
        if (UIBuilder.Font == null)
        {
            CLogger.Warning("UISetup", "폰트 에셋이 없습니다. 1번 메뉴를 먼저 실행하세요.");
            return;
        }

        UIBuilder.SaveAsPrefab(UIBuilder.BuildButton(null).gameObject, UIBuilder.ButtonPrefabPath);
        UIBuilder.SaveAsPrefab(UIBuilder.BuildGauge(null).gameObject, UIBuilder.GaugePrefabPath);
        UIBuilder.SaveAsPrefab(UIBuilder.BuildItemSlot(null).gameObject, UIBuilder.ItemSlotPrefabPath);

        AssetDatabase.SaveAssets();
        CLogger.Log("UISetup", $"공용 부품 프리팹: {UIBuilder.PrefabDir}");
    }

    // 가로 두 방향(좌우 뒤집힘)만 허용한다
    private static void SetLandscape()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
    }

    private static TMP_FontAsset CreateFontAsset()
    {
        TMP_FontAsset exist = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIBuilder.FontAssetPath);
        if (exist != null)
            return exist;

        Font font = AssetDatabase.LoadAssetAtPath<Font>(UIBuilder.FontPath);
        if (font == null)
        {
            CLogger.Error("UISetup", $"{UIBuilder.FontPath} 폰트 파일이 없습니다.");
            return null;
        }

        // !!) 한글은 글자가 많아 동적(Dynamic)으로 두고, 쓰이는 글자만 아틀라스에 채운다
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA,
            AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
        if (fontAsset == null)
        {
            CLogger.Error("UISetup", "폰트 에셋 생성에 실패했습니다.");
            return null;
        }

        fontAsset.name = "Galmuri11 SDF";
        AssetDatabase.CreateAsset(fontAsset, UIBuilder.FontAssetPath);

        Texture2D atlas = fontAsset.atlasTextures[0];
        atlas.name = "Galmuri11 SDF Atlas";
        AssetDatabase.AddObjectToAsset(atlas, fontAsset);

        fontAsset.material.name = "Galmuri11 SDF Material";
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(UIBuilder.FontAssetPath);

        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIBuilder.FontAssetPath);
    }

    // TMP 기본 폰트를 갈무리로 바꿔서, 손으로 만든 텍스트도 한글이 깨지지 않게 한다
    private static void SetDefaultFont(TMP_FontAsset fontAsset)
    {
        TMP_Settings settings = Resources.Load<TMP_Settings>(TMPSettingsPath);
        if (settings == null)
        {
            CLogger.Warning("UISetup", "TMP Settings가 없습니다. Window > TextMeshPro > Import TMP Essential Resources 후 1번 메뉴를 다시 실행하세요.");
            return;
        }

        SerializedObject so = new SerializedObject(settings);
        so.FindProperty("m_defaultFontAsset").objectReferenceValue = fontAsset;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
