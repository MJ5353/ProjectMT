using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// UI 공통 기반(폰트·화면 방향)과 "모든 UI 다시 만들기" 메뉴
public static class UISetup
{

    // 갈무리11 원본 격자(11px)의 4배. 22·33·44·66 크기에서 고르게 보인다
    private const int SamplingPointSize = 44;
    private const int AtlasPadding = 5;
    private const int AtlasSize = 2048;

    // ==================================================

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

    // 지금 쓰는 UI 프리팹을 모두 새로 만든다. 같은 경로에 덮어써 GUID가 유지되므로 씬의 프리팹 인스턴스·참조는 따라온다.
    // 씬(Battle·Lobby)은 열거나 저장하지 않는다. 로비 씬 UI는 씬 안에 있으므로 "초기 설정/로비 씬 재생성"으로 다시 만든다
    [MenuItem("MushTaker/UI/모든 UI 다시 만들기", false, 40)]
    public static void RebuildAll()
    {
        if (UIBuilder.Font == null)
        {
            CLogger.Warning("UISetup", "MushTaker/초기 설정/UI 기반 준비를 먼저 실행하세요.");
            return;
        }

        if (EditorUtility.DisplayDialog("모든 UI 다시 만들기", "공용 부품·팝업·HUD 프리팹을 모두 새로 만듭니다. 프리팹에서 손본 부분은 사라집니다.", "다시 만들기", "취소") == false)
            return;

        BuildAllPrefabs(true);
    }

    // 순서가 중요하다: 공용 부품 → 시장(줄·팝업) → 전투(팝업·줄·HUD). 뒤의 것이 앞의 프리팹을 품거나 참조한다
    public static void BuildAllPrefabs(bool isOverwrite)
    {
        if (UIBuilder.Font == null)
        {
            CLogger.Warning("UISetup", "MushTaker/초기 설정/UI 기반 준비를 먼저 실행하세요.");
            return;
        }

        UIBuilder.SaveAsPrefab(UIBuilder.BuildButton(null).gameObject, EditorPath.ButtonPrefab, isOverwrite);
        UIBuilder.SaveAsPrefab(UIBuilder.BuildGauge(null).gameObject, EditorPath.GaugePrefab, isOverwrite);
        UIBuilder.SaveAsPrefab(UIBuilder.BuildCommonSlot(null).gameObject, EditorPath.CommonSlotPrefab, isOverwrite);
        MarketUISetup.BuildPrefabs(isOverwrite);
        BattleUISetup.BuildPrefabs(isOverwrite);

        AssetDatabase.SaveAssets();
        RefreshPrefabDefs();
        CLogger.Log("UISetup", "UI 프리팹 생성 완료. 열린 씬은 저장하지 않았습니다.");
    }

    // 프리팹 표 ---

    // PopupDef·UIComponentDef를 enum 이름과 같은 프리팹으로 채운다. 프리팹을 손으로 만들었거나 옮겼을 때도 이 메뉴만 돌리면 된다
    [MenuItem("MushTaker/UI/UI 프리팹 표 갱신", false, 41)]
    public static void RefreshPrefabDefs()
    {
        FillPrefabDef<PopupDef, EPopupType, PopupBase>(EditorPath.PopupDefAsset, PopupDef.PopupID);
        FillPrefabDef<UIComponentDef, EUIComponentType, PoolableMonoUI>(EditorPath.UIComponentDefAsset, UIComponentDef.UIComponentID);

        AssetDatabase.SaveAssets();
        DataTools.RefreshDatabase(false); // 새로 만든 표도 DB에 잡히게 한다
    }

    // enum 값마다 UI 프리팹 폴더(하위 포함)에서 같은 이름의 프리팹을 찾아 목록을 통째로 바꾼다. None(0)은 건너뛴다
    private static void FillPrefabDef<TDef, TType, TPrefab>(string assetPath, string dataID)
        where TDef : UIPrefabDef<TType, TPrefab>
        where TType : struct, Enum
        where TPrefab : Component
    {
        TDef def = AssetDatabase.LoadAssetAtPath<TDef>(assetPath);
        if (def == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);

            def = ScriptableObject.CreateInstance<TDef>();
            def.EditorSetIdentity(dataID, dataID);
            AssetDatabase.CreateAsset(def, assetPath);
        }

        List<UIPrefabDef<TType, TPrefab>.Entry> listEntry = new();
        foreach (TType type in Enum.GetValues(typeof(TType)))
        {
            if (Convert.ToInt32(type) == 0)
                continue;

            TPrefab prefab = FindUIPrefab<TPrefab>(type.ToString());
            if (prefab == null)
            {
                CLogger.Warning("UISetup", $"{typeof(TDef).Name}: {type} 이름의 {typeof(TPrefab).Name} 프리팹이 {EditorPath.UIPrefabDir}에 없습니다.");
                continue;
            }

            listEntry.Add(new UIPrefabDef<TType, TPrefab>.Entry(type, prefab));
        }

        def.EditorSetList(listEntry);
        EditorUtility.SetDirty(def);
    }

    // 이름이 정확히 같은 프리팹의 루트 컴포넌트. 없으면 null
    private static T FindUIPrefab<T>(string prefabName) where T : Component
    {
        return AssetDatabase.FindAssets($"{prefabName} t:Prefab", new[] { EditorPath.UIPrefabDir.TrimEnd('/') })
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => Path.GetFileNameWithoutExtension(path) == prefabName)
            .Select(path => AssetDatabase.LoadAssetAtPath<T>(path))
            .FirstOrDefault(prefab => prefab != null);
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
        TMP_FontAsset exist = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EditorPath.FontAsset);
        if (exist != null)
            return exist;

        Font font = AssetDatabase.LoadAssetAtPath<Font>(EditorPath.Font);
        if (font == null)
        {
            CLogger.Error("UISetup", $"{EditorPath.Font} 폰트 파일이 없습니다.");
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
        AssetDatabase.CreateAsset(fontAsset, EditorPath.FontAsset);

        Texture2D atlas = fontAsset.atlasTextures[0];
        atlas.name = "Galmuri11 SDF Atlas";
        AssetDatabase.AddObjectToAsset(atlas, fontAsset);

        fontAsset.material.name = "Galmuri11 SDF Material";
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(EditorPath.FontAsset);

        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EditorPath.FontAsset);
    }

    // TMP 기본 폰트를 갈무리로 바꿔서, 손으로 만든 텍스트도 한글이 깨지지 않게 한다
    private static void SetDefaultFont(TMP_FontAsset fontAsset)
    {
        TMP_Settings settings = Resources.Load<TMP_Settings>(CommonPath.TMPSettings);
        if (settings == null)
        {
            CLogger.Warning("UISetup", "TMP Settings가 없습니다. Window > TextMeshPro > Import TMP Essential Resources 후 UI 기반 준비를 다시 실행하세요.");
            return;
        }

        SerializedObject so = new SerializedObject(settings);
        so.FindProperty("m_defaultFontAsset").objectReferenceValue = fontAsset;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
