using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Assets/Art/Voxel 아래 복셀 에셋 가져오기 설정 (tools/voxel 스크립트가 만든 FBX·PNG 전용)
// - 텍스처: 점 필터, 밉맵·압축 끔 (도트 색이 번지지 않게)
// - 모델: 맵 지형·소품 충돌체, 축 변환 굽기, Generic 애니, 클립 이름 "Buggi|Walk" -> "Walk", Idle/Walk/Run/Underground 반복
// - 클립 이름 표준화: AnimNameTable.txt(모델|원래 이름|표준 이름)대로 바꾼다(Attack_NN·Skill_NN). 표에 줄 추가 → 해당 모델 FBX를 Reimport하고 완료 대기 → 애니메이터 다시 만들기 메뉴 실행
// - 머티리얼: FBX 안의 M_Voxel/M_Terrain을 Materials/ 폴더의 실제 .mat으로 바꿔 끼운다 (텍스처 연결이 FBX 위치와 무관하게 유지)
// !!) 다시 가져올 때마다 클립 설정을 덮어쓴다. 클립을 손으로 고칠 거면 이 파일에서 해당 부분을 빼야 한다.
public class VoxelImportSettings : AssetPostprocessor
{
    private const string Root = "Assets/Art/Voxel/";
    private const string MaterialFolder = Root + "Materials";
    private const string TextureFolder = Root + "Models/";
    public const string AnimNameTablePath = Root + "Editor/AnimNameTable.txt";
    private static readonly string[] s_ArrLoopClip = { "Idle", "Walk", "Run", "Underground" };
    // FBX 안 머티리얼 이름 → 붙일 텍스처 이름
    private static readonly (string material, string texture)[] s_ArrVoxelMaterial =
    {
        ("M_Voxel", "VoxelPalette"),
        ("M_Terrain", "TerrainAtlas"),
    };

    // ==================================================

    private void OnPreprocessTexture()
    {
        if (assetPath.StartsWith(Root) == false)
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
    }

    private void OnPreprocessModel()
    {
        if (assetPath.StartsWith(Root) == false)
            return;

        ModelImporter importer = (ModelImporter)assetImporter;
        importer.bakeAxisConversion = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

        foreach ((string materialName, string _) in s_ArrVoxelMaterial)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(GetMaterialPath(materialName));
            if (material != null)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), materialName), material);
        }
    }

    private void OnPreprocessAnimation()
    {
        if (assetPath.StartsWith(Root) == false)
            return;

        ModelImporter importer = (ModelImporter)assetImporter;
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        if (clips == null || clips.Length == 0)
            return;

        Dictionary<string, string> dicRename = LoadAnimNames(Path.GetFileNameWithoutExtension(assetPath));
        foreach (ModelImporterClipAnimation clip in clips)
        {
            int bar = clip.name.LastIndexOf('|');
            if (bar >= 0)
                clip.name = clip.name.Substring(bar + 1);

            if (dicRename.TryGetValue(clip.name, out string standardName))
                clip.name = standardName;

            clip.loopTime = System.Array.IndexOf(s_ArrLoopClip, clip.name) >= 0;
        }

        importer.clipAnimations = clips;
    }

    // 지형·소품·경계 벽에 MeshCollider를 추가하고 경계 벽은 숨긴다. 지형은 Terrain 레이어
    // 이 모델의 원래 클립 이름 → 표준 이름. 표 파일을 직접 읽어서 가져오기 순서와 상관없다
    public static Dictionary<string, string> LoadAnimNames(string modelName)
    {
        Dictionary<string, string> dicRename = new();
        if (File.Exists(AnimNameTablePath) == false)
            return dicRename;

        foreach (string line in File.ReadAllLines(AnimNameTablePath))
        {
            string[] cells = line.Split('|');
            if (line.StartsWith("#") || cells.Length != 3 || cells[0].Trim() != modelName)
                continue;

            dicRename[cells[1].Trim()] = cells[2].Trim();
        }

        return dicRename;
    }

    private void OnPostprocessModel(GameObject model)
    {
        if (assetPath.StartsWith(Root) == false)
            return;

        int terrainLayer = LayerMask.NameToLayer(GameLayers.Terrain);
        foreach (MeshFilter meshFilter in model.GetComponentsInChildren<MeshFilter>())
        {
            if (meshFilter.name == "Map_Terrain" || meshFilter.name == "Map_Props" || meshFilter.name == "Map_Bounds")
                meshFilter.gameObject.AddComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;
            
            if (meshFilter.name == "Map_Terrain" && terrainLayer >= 0)
                meshFilter.gameObject.layer = terrainLayer;

            if (meshFilter.name == "Map_Bounds" && meshFilter.TryGetComponent<MeshRenderer>(out MeshRenderer renderer))
                renderer.enabled = false;
        }

        // !!) 빈 오브젝트(SpawnSlot)는 bakeAxisConversion이 적용되지 않아 X 90도로 누운 채 들어온다. 수평 방향(yaw)만 남긴다
        foreach (Transform child in model.GetComponentsInChildren<Transform>())
        {
            if (child.name.StartsWith(MapDef.SpawnSlotPrefix))
                child.rotation = SpawnService.GetSpawnRotation(child);
        }
    }

    // ==================================================
    // 머티리얼 에셋 생성·연결 (가져오기 밖에서 실행해야 텍스처 연결이 저장된다)

    [InitializeOnLoadMethod]
    private static void SetupOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            foreach ((string materialName, string _) in s_ArrVoxelMaterial)
            {
                if (AssetDatabase.LoadAssetAtPath<Material>(GetMaterialPath(materialName)) == null)
                {
                    SetupMaterials();
                    return;
                }
            }
        };
    }

    [MenuItem("MushTaker/에셋/복셀 머티리얼 다시 연결", false, 6)]
    private static void SetupMaterials()
    {
        if (AssetDatabase.IsValidFolder(MaterialFolder) == false)
            AssetDatabase.CreateFolder(Root.TrimEnd('/'), "Materials");

        foreach ((string materialName, string textureName) in s_ArrVoxelMaterial)
        {
            CreateOrUpdateMaterial(materialName, textureName);
        }
        AssetDatabase.SaveAssets();

        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Root.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
        Debug.Log("[VoxelImportSettings] 복셀 머티리얼 연결 완료");
    }

    private static void CreateOrUpdateMaterial(string materialName, string textureName)
    {
        string materialPath = GetMaterialPath(materialName);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, materialPath);
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(FindTexturePath(textureName) ?? string.Empty);
        if (texture == null)
            Debug.LogWarning($"[VoxelImportSettings] {textureName}.png 를 찾지 못함");

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0f);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", 0f);
        EditorUtility.SetDirty(material);
    }

    private static string GetMaterialPath(string materialName)
    {
        return MaterialFolder + "/" + materialName + ".mat";
    }

    private static string FindTexturePath(string textureName)
    {
        string fixedPath = TextureFolder + textureName + ".png";
        if (System.IO.File.Exists(fixedPath))
            return fixedPath;

        foreach (string guid in AssetDatabase.FindAssets(textureName + " t:Texture2D", new[] { Root.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == textureName)
                return path;
        }
        return null;
    }
}
