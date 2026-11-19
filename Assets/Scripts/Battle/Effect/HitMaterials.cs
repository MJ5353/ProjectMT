using UnityEngine;

// ==================================================

// 피격 연출용 머티리얼(번쩍, 조각). Resources/Materials 아래 에셋을 쓴다(경로는 CommonPath).
// 에셋은 MushTaker/초기 설정/누락 전투 에셋 복구에서 만든다.
// Resources에 있는 머티리얼은 빌드에 셰이더와 함께 들어간다

// [mj] todo) resource manager?

public static class HitMaterials
{
    public static Material Flash => Load(ref s_Flash, CommonPath.HitFlashMaterial);
    private static Material s_Flash;

    public static Material Chunk => Load(ref s_Chunk, CommonPath.HitChunkMaterial);
    private static Material s_Chunk;

    // ==================================================

    private static Material Load(ref Material cache, string resourcePath)
    {
        if (cache != null)
            return cache;

        cache = Resources.Load<Material>(resourcePath);
        if (cache == null)
            CLogger.Error("HitMaterials", $"Resources/{resourcePath} 머티리얼이 없습니다. MushTaker/초기 설정/누락 전투 에셋 복구를 실행하세요.");

        return cache;
    }
}
