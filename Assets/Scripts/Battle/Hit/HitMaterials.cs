using UnityEngine;

// ==================================================

// 피격 연출용 머티리얼(번쩍, 조각). Resources/Materials 아래 에셋을 쓴다.
// 에셋은 MushTaker/초기 설정/누락 전투 에셋 복구에서 만든다.
// Resources에 있는 머티리얼은 빌드에 셰이더와 함께 들어간다

// [mj] todo) resource manager?

public static class HitMaterials
{
    public const string ResourceDir = "Materials/";
    public const string FlashName = "HitFlash";
    public const string ChunkName = "HitChunk";

    public static Material Flash => Load(ref s_Flash, FlashName);
    private static Material s_Flash;

    public static Material Chunk => Load(ref s_Chunk, ChunkName);
    private static Material s_Chunk;

    // ==================================================

    private static Material Load(ref Material cache, string materialName)
    {
        if (cache != null)
            return cache;

        cache = Resources.Load<Material>(ResourceDir + materialName);
        if (cache == null)
            CLogger.Error("HitMaterials", $"Resources/{ResourceDir}{materialName} 머티리얼이 없습니다. MushTaker/초기 설정/누락 전투 에셋 복구를 실행하세요.");

        return cache;
    }
}
