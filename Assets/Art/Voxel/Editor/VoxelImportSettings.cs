using UnityEditor;
using UnityEngine;

// Assets/Art/Voxel 아래 복셀 에셋 가져오기 설정 (tools/voxel 스크립트가 만든 FBX·PNG 전용)
// - 텍스처: 점 필터, 밉맵·압축 끔 (도트 색이 번지지 않게)
// - 모델: 맵 지형·소품 충돌체, 축 변환 굽기, Generic 애니, 클립 이름 "Buggi|Walk" -> "Walk", Idle/Walk/Run/Underground 반복
// - 머티리얼: FBX 안의 M_Voxel/M_Terrain을 Materials/ 폴더의 실제 .mat으로 바꿔 끼운다 (텍스처 연결이 FBX 위치와 무관하게 유지)
// !!) 다시 가져올 때마다 클립 설정을 덮어쓴다. 클립을 손으로 고칠 거면 이 파일에서 해당 부분을 빼야 한다.
public class VoxelImportSettings : AssetPostprocessor
{
    private const string Root = "Assets/Art/Voxel/";
    private const string MaterialFolder = Root + "Materials";
    private const string TextureFolder = Root + "Models/";
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

        foreach (ModelImporterClipAnimation clip in clips)
        {
            int bar = clip.name.LastIndexOf('|');
            if (bar >= 0)
                clip.name = clip.name.Substring(bar + 1);

            clip.loopTime = System.Array.IndexOf(s_ArrLoopClip, clip.name) >= 0;
        }

        importer.clipAnimations = clips;
    }

    // 지형·소품·경계 벽에 MeshCollider를 추가하고 경계 벽은 숨긴다.
    private void OnPostprocessModel(GameObject model)
    {
        if (assetPath.StartsWith(Root) == false)
            return;

        foreach (MeshFilter meshFilter in model.GetComponentsInChildren<MeshFilter>())
        {
            if (meshFilter.name == "Map_Terrain" || meshFilter.name == "Map_Props" || meshFilter.name == "Map_Bounds")
                meshFilter.gameObject.AddComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;
            if (meshFilter.name == "Map_Bounds" && meshFilter.TryGetComponent<MeshRenderer>(out MeshRenderer renderer))
                renderer.enabled = false;
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

    [MenuItem("MushTaker/Voxel/머티리얼 다시 연결")]
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
