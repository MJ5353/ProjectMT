using UnityEditor;
using UnityEngine;

// Assets/Art/Voxel 아래 복셀 에셋 가져오기 설정 (tools/voxel 스크립트가 만든 FBX·PNG 전용)
// - 텍스처: 점 필터, 밉맵·압축 끔 (도트 색이 번지지 않게)
// - 모델: 맵 지형·소품 충돌체, 축 변환 굽기, Generic 애니, 클립 이름 "Buggi|Walk" -> "Walk", Idle/Walk/Run 반복
// 주의: 다시 가져올 때마다 클립 설정을 덮어쓴다. 클립을 손으로 고칠 거면 이 파일에서 해당 부분을 빼야 한다.
public class VoxelImportSettings : AssetPostprocessor
{
    const string Root = "Assets/Art/Voxel/";
    static readonly string[] LoopClips = { "Idle", "Walk", "Run" };

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root)) return;
        var ti = (TextureImporter)assetImporter;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.npotScale = TextureImporterNPOTScale.None;
    }

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Root)) return;
        var mi = (ModelImporter)assetImporter;
        mi.bakeAxisConversion = true;
        mi.importCameras = false;
        mi.importLights = false;
        mi.animationType = ModelImporterAnimationType.Generic;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
    }

    void OnPreprocessAnimation()
    {
        if (!assetPath.StartsWith(Root)) return;
        var mi = (ModelImporter)assetImporter;
        var clips = mi.defaultClipAnimations;
        if (clips == null || clips.Length == 0) return;
        foreach (var c in clips)
        {
            int bar = c.name.LastIndexOf('|');
            if (bar >= 0) c.name = c.name.Substring(bar + 1);
            c.loopTime = System.Array.IndexOf(LoopClips, c.name) >= 0;
        }
        mi.clipAnimations = clips;
    }

    // 맵: 지형·소품·경계 벽에 MeshCollider (풀·꽃 Map_Grass는 제외), 경계 벽은 렌더러 끔
    void OnPostprocessModel(GameObject g)
    {
        if (!assetPath.StartsWith(Root)) return;
        foreach (var mf in g.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.name == "Map_Terrain" || mf.name == "Map_Props" || mf.name == "Map_Bounds")
                mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            if (mf.name == "Map_Bounds" && mf.TryGetComponent<MeshRenderer>(out var mr))
                mr.enabled = false;   // 보이지 않는 경계 벽
        }
    }

    void OnPostprocessMaterial(Material m)
    {
        if (!assetPath.StartsWith(Root)) return;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0f);
    }
}
