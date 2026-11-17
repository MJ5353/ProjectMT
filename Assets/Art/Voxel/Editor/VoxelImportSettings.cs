using UnityEditor;
using UnityEngine;

// Assets/Art/Voxel 아래 복셀 에셋 가져오기 설정 (tools/voxel 스크립트가 만든 FBX·PNG 전용)
// - 텍스처: 점 필터, 밉맵·압축 끔 (도트 색이 번지지 않게)
// - 모델: 맵 지형·소품 충돌체, 축 변환 굽기, Generic 애니, 클립 이름 "Buggi|Walk" -> "Walk", Idle/Walk/Run 반복
// !!) 다시 가져올 때마다 클립 설정을 덮어쓴다. 클립을 손으로 고칠 거면 이 파일에서 해당 부분을 빼야 한다.
public class VoxelImportSettings : AssetPostprocessor
{
    private const string Root = "Assets/Art/Voxel/";
    private static readonly string[] s_ArrLoopClip = { "Idle", "Walk", "Run" };

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

    private void OnPostprocessMaterial(Material material)
    {
        if (assetPath.StartsWith(Root) == false)
            return;

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0f);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", 0f);
    }
}
